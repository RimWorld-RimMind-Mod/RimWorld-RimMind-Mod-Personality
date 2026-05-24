using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;
using RimMind.Presentation;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Personality.Data;
using RimWorld;
using Verse;

namespace RimMind.Personality.Comps
{
    public enum TriggerEventType { Injury, Skill, Incident, Death }

    public class CompProperties_AIPersonality : CompProperties
    {
        public CompProperties_AIPersonality()
        {
            compClass = typeof(CompAIPersonality);
        }
    }

    /// <summary>
    /// 挂载�?Pawn �?ThingComp，负责触�?AI 人格评估�?
    /// 支持每日定时触发和事件驱动触发（外部 Patch 通过 TriggerEvent 注入）�?
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

            var schema = PersonalityThoughtMapper.EvaluationSchema;

            var npcId = $"NPC-{Pawn.thingIDNumber}";
            var envelope = LlmRequestEnvelopeBuilder
                .ForScenario(RimMindAPI.Context.ScenarioPersonality)
                .WithModId("RimMind.Personality")
                .WithNpcId(npcId)
                .WithGameStateInfo(eventCtx)
                .WithSchema(schema)
                .WithMaxTokens(PersonalityThoughtMapper.DefaultMaxTokens)
                .WithTemperature(PersonalityThoughtMapper.DefaultTemperature)
                .Build();

            RimMindAPI.Request.Send(envelope, result =>
            {
                _hasPendingRequest = false;
                PersonalityThoughtMapper.Apply(result, Pawn);
            });
        }

        /// <summary>
        /// 从外�?Patch（受伤、技能升级、事件等）触发一次人格评估�?
        /// </summary>
        public void TriggerEvent(string context, TriggerEventType eventType = TriggerEventType.Incident)
        {
            if (!Settings.enablePersonality) return;

            bool enabled = eventType switch
            {
                TriggerEventType.Injury => Settings.enableInjuryTrigger,
                TriggerEventType.Skill => Settings.enableSkillTrigger,
                TriggerEventType.Incident => Settings.enableIncidentTrigger,
                TriggerEventType.Death => Settings.enableDeathTrigger,
                _ => true,
            };
            if (!enabled) return;

            _pendingEventContext = context;
        }

        private bool IsEligible() =>
            Pawn.IsFreeNonSlaveColonist &&
            !Pawn.Dead &&
            Pawn.Map != null &&
            Pawn.needs?.mood != null;

        // ContextEngine 接管，不再手动构建 SystemPrompt

        // �?ContextSettings 读取人格场景预算
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref _lastEventTick, "lastEventTick", -1200);
            Scribe_Values.Look(ref _dailyJitter, "dailyJitter", -1);
        }
    }
}
