using RimMind.Application.Common.Helpers;
using RimMind.Domain.ValueObjects;
using RimMind.Presentation.Api;
using RimWorld;
using Verse;

namespace RimMind.Personality.Comps
{
    public class CompProperties_AIPersonality : CompProperties
    {
        public CompProperties_AIPersonality()
        {
            compClass = typeof(CompAIPersonality);
        }
    }

    public class CompAIPersonality : ThingComp
    {
        private bool _hasPendingRequest;
        private int _requestGeneration;
        private int _lastEventTick = -1200;
        private int _lastDailyDecayTick = -1;
        private int _pendingRequestTick;
        private string? _pendingEventContext;
        private string? _cachedThingId;
        private int _cachedThingIdNumber = -1;

        private int _lastHour = -1;
        private string? _lastJobDefName;
        private float _lastMood = -1f;
        private bool _wasAwake = true;

        private Pawn? PawnOrNull => parent as Pawn;
        private AIPersonalitySettings Settings => RimMindPersonalityMod.Settings;

        private int GetDailyJitter()
        {
            if (PawnOrNull is not Pawn pawn) return 0;
            int range = Settings.jitterRangeTicks;
            if (range <= 0) return 0;
            int span = range * 2 + 1;
            return TickStagger.PhaseOffset(pawn.thingIDNumber, span) - range;
        }

        private string GetCachedThingId(Pawn pawn)
        {
            if (_cachedThingId == null || _cachedThingIdNumber != pawn.thingIDNumber)
            {
                _cachedThingIdNumber = pawn.thingIDNumber;
                _cachedThingId = pawn.ThingID;
            }
            return _cachedThingId;
        }

        public override void CompTick()
        {
            if (parent == null || !parent.IsHashIntervalTick(60)) return;
            if (!Settings.enablePersonality) return;
            if (!(parent is Pawn pawn) || !RimMindPawnLookup.IsActiveSpawnedColonist(pawn)) return;
            if (!RimMindAPI.Settings.IsConfigured()) return;
            if (RimMindAPI.IsAgentActive(GetCachedThingId(pawn))) return;
            if (_hasPendingRequest)
            {
                if (Find.TickManager.TicksGame - _pendingRequestTick > Settings.requestTimeoutTicks)
                {
                    string pawnName = pawn.Name?.ToStringShort ?? pawn.LabelShort;
                    RimMindErrors.Warn($"[RimMind-Personality] Pending request timeout for {pawnName}, resetting.");
                    _hasPendingRequest = false;
                    _requestGeneration++;
                }
                else
                {
                    return;
                }
            }
            if (!IsEligible()) return;

            // 1. Observe State Transitions (Sunrise, Joy/Recreation, Skygaze/Meditate, Mood Swing)
            ObserveStateTransitions();

            // 2. Evaluation admission: Event/Transition + Cooldown Guardrail
            int ticksGame = Find.TickManager.TicksGame;
            if (_lastDailyDecayTick < 0)
            {
                _lastDailyDecayTick = ticksGame;
            }

            int effectiveCooldown = RimMindAPI.Settings.ScaleCooldown(Settings.eventCooldownTicks);
            bool cooldownExpired = (ticksGame - _lastEventTick) >= effectiveCooldown;
            bool dailyFire = PersonalityTriggerPolicy.ShouldTriggerDailyEvaluation(
                Settings.enableDailyEval,
                pawn.Awake(),
                ticksGame,
                _lastDailyDecayTick,
                Settings.dailyIntervalTicks,
                GetDailyJitter());
            bool eventFire = _pendingEventContext != null && cooldownExpired;

            if (!dailyFire && !eventFire) return;

            string? eventCtx = _pendingEventContext;
            _pendingEventContext = null;
            _lastEventTick = ticksGame;
            if (dailyFire)
            {
                _lastDailyDecayTick = ticksGame;
            }
            _hasPendingRequest = true;
            _pendingRequestTick = ticksGame;
            int requestGen = ++_requestGeneration;

            if (!string.IsNullOrEmpty(eventCtx))
            {
                eventCtx = TranslateOrFallback(
                    "RimMind.Personality.Prompt.TaskInstruction.TriggerReason",
                    "[Trigger Reason] {0}",
                    eventCtx!);
            }

            var envelope = PersonalityRequestBuilder.BuildForPawn(pawn.thingIDNumber, eventCtx);

            RimMindAPI.Request.Send(envelope, result =>
            {
                if (requestGen != _requestGeneration) return;
                _hasPendingRequest = false;
                PersonalityThoughtMapper.Apply(result, pawn);
            });
        }

        private void ObserveStateTransitions()
        {
            if (PawnOrNull is not Pawn pawn) return;
            Map? map = pawn.Map;
            if (map == null) return;

            int currentTick = Find.TickManager.TicksGame;
            float activityScale = RimMindAPI.Settings.ActivityFrequencyScale;
            int effectiveCooldown = RimMindAPI.Settings.ScaleCooldown(Settings.eventCooldownTicks);
            bool canQueue = !_hasPendingRequest && (currentTick - _lastEventTick) >= effectiveCooldown && _pendingEventContext == null;

            bool isAwake = pawn.Awake();
            bool justWokeUp = !_wasAwake && isAwake;
            _wasAwake = isAwake;

            float currentMood = pawn.needs?.mood?.CurLevel ?? 0.5f;

            if (justWokeUp && canQueue && Settings.enableSunriseTrigger)
            {
                float jitter = Rand.Range(0.85f, 1.15f);
                float dynamicChance = PersonalityTriggerPolicy.CalculateDynamicPersonalityChance(Settings.sunriseTriggerChance, currentMood, jitter, activityScale);
                if (PersonalityTriggerPolicy.ShouldTriggerAwakening(true, dynamicChance, Rand.Value, justWokeUp))
                {
                    int hour = GenLocalDate.HourInteger(map);
                    bool isNight = hour >= 19 || hour < 6;
                    string shiftDesc = isNight
                        ? TranslateOrFallback("RimMind.Personality.Trigger.ShiftNight", "Night shift / nocturnal schedule")
                        : TranslateOrFallback("RimMind.Personality.Trigger.ShiftDay", "Day shift / morning schedule");
                    string hourStr = hour.ToString("D2");
                    _pendingEventContext = isNight
                        ? TranslateOrFallback(
                            "RimMind.Personality.Trigger.AwakeningNight",
                            "Awakening (Hour {0}:00, {1}): Just woke up into dusk/night, starting nocturnal work shift.",
                            hourStr,
                            shiftDesc)
                        : TranslateOrFallback(
                            "RimMind.Personality.Trigger.AwakeningDay",
                            "Awakening (Hour {0}:00, {1}): Just woke up, clearing morning thoughts and preparing for daylight hours ahead.",
                            hourStr,
                            shiftDesc);
                }
            }

            if (pawn.IsHashIntervalTick(250, 60))
            {
                int currentHour = GenLocalDate.HourInteger(map);
                if (_lastHour != -1 && currentHour != _lastHour)
                {
                    if ((currentHour == 18 || currentHour == 19) && canQueue && Settings.enableSunriseTrigger)
                    {
                        float jitter = Rand.Range(0.85f, 1.15f);
                        float dynamicChance = PersonalityTriggerPolicy.CalculateDynamicPersonalityChance(Settings.sunriseTriggerChance * 0.8f, currentMood, jitter, activityScale);
                        if (PersonalityTriggerPolicy.ShouldTriggerDusk(true, dynamicChance, Rand.Value, isAwake))
                        {
                            _pendingEventContext = TranslateOrFallback(
                                "RimMind.Personality.Trigger.Dusk",
                                "Dusk: Dusk falls across the settlement, bringing an evening pause for reflection.");
                        }
                    }
                }
                _lastHour = currentHour;
            }

            if (pawn.IsHashIntervalTick(2500, 60) && canQueue && isAwake)
            {
                JobDef? activeJob = pawn.CurJobDef;
                bool isIdle = activeJob == JobDefOf.Wait_Wander || activeJob == JobDefOf.GotoWander || activeJob == JobDefOf.Wait;
                if (isIdle)
                {
                    float jitter = Rand.Range(0.85f, 1.15f);
                    float dynamicChance = PersonalityTriggerPolicy.CalculateDynamicPersonalityChance(0.20f, currentMood, jitter, activityScale);
                    if (PersonalityTriggerPolicy.ShouldTriggerContemplationPulse(true, dynamicChance, Rand.Value, isAwake, isIdle))
                    {
                        string jobLabel = activeJob?.label ?? TranslateOrFallback("RimMind.Personality.Trigger.Wandering", "wandering");
                        _pendingEventContext = TranslateOrFallback(
                            "RimMind.Personality.Trigger.ContemplationIdle",
                            "Contemplation: A quiet pause while {0}, pondering the colony's fortunes.",
                            jobLabel);
                    }
                }
            }

            JobDef? curJob = pawn.CurJobDef;
            string? curJobName = curJob?.defName;
            if (curJobName != _lastJobDefName)
            {
                if (curJob != null && curJobName != null)
                {
                    float jitter = Rand.Range(0.85f, 1.15f);
                    if (curJobName == "Skygaze" || curJobName == "Meditate" || curJobName == "Pray")
                    {
                        float dynamicChance = PersonalityTriggerPolicy.CalculateDynamicPersonalityChance(Settings.skygazeTriggerChance, currentMood, jitter, activityScale);
                        if (canQueue && PersonalityTriggerPolicy.ShouldTriggerSkygaze(
                            Settings.enableSkygazeTrigger,
                            dynamicChance,
                            Rand.Value))
                        {
                            _pendingEventContext = TranslateOrFallback(
                                "RimMind.Personality.Trigger.ContemplationSkygaze",
                                "Contemplation: Solitary moments of quiet reflection during {0}.",
                                curJob.label ?? curJobName);
                        }
                    }
                    else if (curJob.joyKind != null)
                    {
                        float dynamicChance = PersonalityTriggerPolicy.CalculateDynamicPersonalityChance(Settings.recreationTriggerChance, currentMood, jitter, activityScale);
                        if (canQueue && PersonalityTriggerPolicy.ShouldTriggerRecreation(
                            Settings.enableRecreationTrigger,
                            dynamicChance,
                            Rand.Value))
                        {
                            _pendingEventContext = TranslateOrFallback(
                                "RimMind.Personality.Trigger.Recreation",
                                "Recreation: Relaxing and enjoying leisure time ({0}).",
                                curJob.label ?? curJobName);
                        }
                    }
                }
                _lastJobDefName = curJobName;
            }

            if (pawn.IsHashIntervalTick(500, 60) && pawn.needs?.mood != null)
            {
                float curMood = pawn.needs.mood.CurLevel;
                if (_lastMood < 0f)
                {
                    _lastMood = curMood;
                }
                else
                {
                    float moodDelta = curMood - _lastMood;
                    if (System.Math.Abs(moodDelta) >= 0.15f)
                    {
                        if (canQueue && PersonalityTriggerPolicy.ShouldTriggerMoodSwing(
                            Settings.enableMoodSwingTrigger,
                            Settings.moodSwingTriggerChance * activityScale,
                            Rand.Value,
                            moodDelta))
                        {
                            _pendingEventContext = TranslateOrFallback(
                                "RimMind.Personality.Trigger.MoodShift",
                                "MoodShift: Experiencing a noticeable emotional shift ({0}).",
                                moodDelta.ToString("+0.00;-0.00"));
                        }
                        _lastMood = curMood;
                    }
                }
            }
        }

        public void TriggerEvent(string context, TriggerEventType eventType = TriggerEventType.Incident)
        {
            if (PawnOrNull == null || !Settings.enablePersonality) return;

            if (!PersonalityTriggerPolicy.IsTriggerEnabled(eventType, Settings))
                return;

            _pendingEventContext = context;
        }

        private bool IsEligible() => PawnOrNull is Pawn pawn && PersonalityTriggerPolicy.IsEligible(pawn);

        private static string TranslateOrFallback(string key, string fallback, params object[] args)
            => RimMindText.TrOr(key, fallback, args);

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref _lastEventTick, "lastEventTick", -1200);
            Scribe_Values.Look(ref _lastDailyDecayTick, "lastDailyDecayTick", -1);
            int legacyDailyJitter = -1;
            Scribe_Values.Look(ref legacyDailyJitter, "dailyJitter", -1);
            Scribe_Values.Look(ref _lastHour, "lastHour", -1);
            Scribe_Values.Look(ref _lastJobDefName, "lastJobDefName", null);
            Scribe_Values.Look(ref _lastMood, "lastMood", -1f);
            Scribe_Values.Look(ref _wasAwake, "wasAwake", true);
        }
    }
}
