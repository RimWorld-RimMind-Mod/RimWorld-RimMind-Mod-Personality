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

            bool dailyFire = Settings.enableDailyEval && Pawn.IsHashIntervalTick(Settings.dailyIntervalTicks + GetDailyJitter());
            bool eventFire = _pendingEventContext != null &&
                             Find.TickManager.TicksGame - _lastEventTick >= Settings.eventCooldownTicks;

            if (!dailyFire && !eventFire) return;

            string? eventCtx = _pendingEventContext;
            _pendingEventContext = null;
            _lastEventTick = Find.TickManager.TicksGame;
            _hasPendingRequest = true;
            _pendingRequestTick = Find.TickManager.TicksGame;

            var envelope = PersonalityRequestBuilder.BuildForPawn(Pawn.thingIDNumber, eventCtx);

            RimMindAPI.Request.Send(envelope, result =>
            {
                _hasPendingRequest = false;
                PersonalityThoughtMapper.Apply(result, Pawn);
            });
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
        }
    }
}
