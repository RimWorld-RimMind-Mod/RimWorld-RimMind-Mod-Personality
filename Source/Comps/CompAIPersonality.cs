using RimMind.Domain.ValueObjects;
using RimMind.Presentation.Api;
using RimWorld;
using UnityEngine;
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

    /// <summary>
    /// 挂载于 Pawn 的 ThingComp，负责触发 AI 人格评估。
    /// 支持每日定时触发和事件驱动触发（外部 Patch 通过 TriggerEvent 注入）。
    /// </summary>
    public class CompAIPersonality : ThingComp
    {
        private bool _hasPendingRequest;
        private int _lastEventTick = -1200;
        private int _pendingRequestTick;
        private string? _pendingEventContext;
        private int _dailyJitter = -1;

        private int _lastHour = -1;
        private string? _lastJobDefName;
        private float _lastMood = -1f;
        private bool _wasAwake = true;

        private Pawn Pawn => (Pawn)parent;
        private AIPersonalitySettings Settings => RimMindPersonalityMod.Settings;

        private int GetDailyJitter()
        {
            if (_dailyJitter < 0)
                _dailyJitter = new System.Random(Pawn.thingIDNumber ^ 0x3C3C3C3C).Next(-Settings.jitterRangeTicks, Settings.jitterRangeTicks + 1);
            return _dailyJitter;
        }

        public override void CompTick()
        {
            if (!Settings.enablePersonality) return;
            if (RimMindAPI.IsConfigured() == false) return;
            if (RimMindAPI.IsAgentActive(Pawn.ThingID)) return;
            if (_hasPendingRequest)
            {
                if (Find.TickManager.TicksGame - _pendingRequestTick > Settings.requestTimeoutTicks)
                {
                    RimMindErrors.Warn($"[RimMind-Personality] Pending request timeout for {Pawn.Name.ToStringShort}, resetting.");
                    _hasPendingRequest = false;
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
            int currentTick = Find.TickManager.TicksGame;
            bool cooldownExpired = (currentTick - _lastEventTick) >= Settings.eventCooldownTicks;

            bool dailyFire = Settings.enableDailyEval && Pawn.IsHashIntervalTick(Settings.dailyIntervalTicks + GetDailyJitter());
            bool eventFire = _pendingEventContext != null && cooldownExpired;

            if (!dailyFire && !eventFire) return;

            string? eventCtx = _pendingEventContext;
            _pendingEventContext = null;
            _lastEventTick = currentTick;
            _hasPendingRequest = true;
            _pendingRequestTick = currentTick;

            var envelope = PersonalityRequestBuilder.BuildForPawn(Pawn.thingIDNumber, eventCtx);

            RimMindAPI.Request.Send(envelope, result =>
            {
                _hasPendingRequest = false;
                PersonalityThoughtMapper.Apply(result, Pawn);
            });
        }

        /// <summary>
        /// Detects natural behavioral transitions and queues probabilistic personality evaluations.
        /// Colonist thoughts are not locked to 6:00 AM; they emerge dynamically at natural awakening (any hour),
        /// dusk/sunset, idle wandering pulses, job shifts, and mood swings.
        /// </summary>
        private void ObserveStateTransitions()
        {
            Map? map = Pawn.Map;
            if (map == null) return;

            int currentTick = Find.TickManager.TicksGame;
            bool canQueue = !_hasPendingRequest && (currentTick - _lastEventTick) >= Settings.eventCooldownTicks && _pendingEventContext == null;
            float activityScale = RimMindAPI.Settings.ActivityFrequencyScale;
            float cooldownMultiplier = activityScale > 0.01f ? (1.0f / activityScale) : 1.0f;
            int effectiveCooldown = Mathf.RoundToInt(Settings.eventCooldownTicks * Mathf.Clamp(cooldownMultiplier, 0.35f, 3.5f));
            bool canQueue = !_hasPendingRequest && (currentTick - _lastEventTick) >= effectiveCooldown && _pendingEventContext == null;

            // Check A: Sunrise / Dawn transition (sampled every 250 ticks ≈ 4 seconds)
            bool isAwake = Pawn.Awake();
            bool justWokeUp = !_wasAwake && isAwake;
            _wasAwake = isAwake;

            float currentMood = Pawn.needs?.mood?.CurLevel ?? 0.5f;

            // Check A1: Natural Awakening transition at ANY hour of day or night
            if (justWokeUp && canQueue && Settings.enableSunriseTrigger)
            {
                float jitter = Rand.Range(0.85f, 1.15f);
                float dynamicChance = PersonalityTriggerPolicy.CalculateDynamicPersonalityChance(Settings.sunriseTriggerChance, currentMood, jitter);
                float dynamicChance = PersonalityTriggerPolicy.CalculateDynamicPersonalityChance(Settings.sunriseTriggerChance, currentMood, jitter, activityScale);
                if (PersonalityTriggerPolicy.ShouldTriggerAwakening(true, dynamicChance, Rand.Value, justWokeUp))
                {
                    int hour = GenLocalDate.HourInteger(map);
                    _pendingEventContext = $"Awakening (Hour {hour:D2}:00): Just woke up, clearing morning thoughts and preparing for the hours ahead.";
                }
            }

            // Check A2: Dusk / Sunset transition (sampled every 250 ticks ≈ 4 seconds)
            if (Pawn.IsHashIntervalTick(250))
            {
                int currentHour = GenLocalDate.HourInteger(map);
                if (_lastHour != -1 && currentHour == 6 && _lastHour != 6)
                if (_lastHour != -1 && currentHour != _lastHour)
                {
                    if (canQueue && PersonalityTriggerPolicy.ShouldTriggerSunrise(
                        Settings.enableSunriseTrigger,
                        Settings.sunriseTriggerChance,
                        Rand.Value,
                        Pawn.Awake()))
                    if ((currentHour == 18 || currentHour == 19) && canQueue && Settings.enableSunriseTrigger)
                    {
                        _pendingEventContext = "Sunrise: The dawn breaks over the colony. A new day begins with fresh morning thoughts.";
                        float jitter = Rand.Range(0.85f, 1.15f);
                        float dynamicChance = PersonalityTriggerPolicy.CalculateDynamicPersonalityChance(Settings.sunriseTriggerChance * 0.8f, currentMood, jitter);
                        float dynamicChance = PersonalityTriggerPolicy.CalculateDynamicPersonalityChance(Settings.sunriseTriggerChance * 0.8f, currentMood, jitter, activityScale);
                        if (PersonalityTriggerPolicy.ShouldTriggerDusk(true, dynamicChance, Rand.Value, isAwake))
                        {
                            _pendingEventContext = "Dusk: Dusk falls across the settlement, bringing an evening pause for reflection.";
                        }
                    }
                }
                _lastHour = currentHour;
            }

            // Check A3: Daytime Idle Wandering Contemplation Pulse (sampled every 2500 ticks ≈ 40 seconds)
            if (Pawn.IsHashIntervalTick(2500) && canQueue && isAwake)
            {
                JobDef? activeJob = Pawn.CurJobDef;
                bool isIdle = activeJob == JobDefOf.Wait_Wander || activeJob == JobDefOf.GotoWander || activeJob == JobDefOf.Wait;
                if (isIdle)
                {
                    float jitter = Rand.Range(0.85f, 1.15f);
                    float dynamicChance = PersonalityTriggerPolicy.CalculateDynamicPersonalityChance(0.20f, currentMood, jitter);
                    float dynamicChance = PersonalityTriggerPolicy.CalculateDynamicPersonalityChance(0.20f, currentMood, jitter, activityScale);
                    if (PersonalityTriggerPolicy.ShouldTriggerContemplationPulse(true, dynamicChance, Rand.Value, isAwake, isIdle))
                    {
                        _pendingEventContext = $"Contemplation: A quiet pause while {activeJob?.label ?? "wandering"}, pondering the colony's fortunes.";
                    }
                }
            }

            // Check B: Job Transition (Recreation or Skygaze/Meditate)
            JobDef? curJob = Pawn.CurJobDef;
            string? curJobName = curJob?.defName;
            if (curJobName != _lastJobDefName)
            {
                if (curJob != null && curJobName != null)
                {
                    float jitter = Rand.Range(0.85f, 1.15f);
                    // Skygazing / Meditating / Praying
                    if (curJobName == "Skygaze" || curJobName == "Meditate" || curJobName == "Pray")
                    {
                        float dynamicChance = PersonalityTriggerPolicy.CalculateDynamicPersonalityChance(Settings.skygazeTriggerChance, currentMood, jitter);
                        float dynamicChance = PersonalityTriggerPolicy.CalculateDynamicPersonalityChance(Settings.skygazeTriggerChance, currentMood, jitter, activityScale);
                        if (canQueue && PersonalityTriggerPolicy.ShouldTriggerSkygaze(
                            Settings.enableSkygazeTrigger,
                            dynamicChance,
                            Rand.Value))
                        {
                            _pendingEventContext = $"Contemplation: Solitary moments of quiet reflection during {curJob.label ?? curJobName}.";
                        }
                    }
                    // Joy / Recreation
                    else if (curJob.joyKind != null)
                    {
                        float dynamicChance = PersonalityTriggerPolicy.CalculateDynamicPersonalityChance(Settings.recreationTriggerChance, currentMood, jitter);
                        float dynamicChance = PersonalityTriggerPolicy.CalculateDynamicPersonalityChance(Settings.recreationTriggerChance, currentMood, jitter, activityScale);
                        if (canQueue && PersonalityTriggerPolicy.ShouldTriggerRecreation(
                            Settings.enableRecreationTrigger,
                            dynamicChance,
                            Rand.Value))
                        {
                            _pendingEventContext = $"Recreation: Relaxing and enjoying leisure time ({curJob.label ?? curJobName}).";
                        }
                    }
                }
                _lastJobDefName = curJobName;
            }

            // Check C: Mood Swing (sampled every 500 ticks ≈ 8 seconds)
            if (Pawn.IsHashIntervalTick(500) && Pawn.needs?.mood != null)
            {
                float curMood = Pawn.needs.mood.CurLevel;
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
                            Settings.moodSwingTriggerChance,
                            Settings.moodSwingTriggerChance * activityScale,
                            Rand.Value,
                            moodDelta))
                        {
                            _pendingEventContext = $"MoodShift: Experiencing a noticeable emotional shift ({moodDelta:+0.00;-0.00}).";
                        }
                        _lastMood = curMood;
                    }
                }
            }
        }

        /// <summary>
        /// 从外部 Patch（受伤、技能升级、事件等）触发一次人格评估。
        /// </summary>
        public void TriggerEvent(string context, TriggerEventType eventType = TriggerEventType.Incident)
        {
            if (!Settings.enablePersonality) return;

            if (!PersonalityTriggerPolicy.IsTriggerEnabled(eventType, Settings))
                return;

            _pendingEventContext = context;
        }

        private bool IsEligible() => PersonalityTriggerPolicy.IsPawnEligible(
            Pawn.IsFreeNonSlaveColonist,
            Pawn.Dead,
            Pawn.Map != null,
            Pawn.needs?.mood != null);

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref _lastEventTick, "lastEventTick", -1200);
            Scribe_Values.Look(ref _dailyJitter, "dailyJitter", -1);
            Scribe_Values.Look(ref _lastHour, "lastHour", -1);
            Scribe_Values.Look(ref _lastJobDefName, "lastJobDefName", null);
            Scribe_Values.Look(ref _lastMood, "lastMood", -1f);
            Scribe_Values.Look(ref _wasAwake, "wasAwake", true);
        }
    }
}
