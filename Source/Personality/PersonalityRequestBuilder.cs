using RimMind.Application.Features.Llm;
using RimMind.Domain.Llm;
using RimMind.Presentation.Api;

namespace RimMind.Personality
{
    /// <summary>
    /// Shared helper for building personality evaluation LLM request envelopes.
    /// Eliminates duplication between CompAIPersonality.CompTick and PersonalityDebugActions.
    /// </summary>
    public static class PersonalityRequestBuilder
    {
        /// <summary>
        /// Builds the NPC ID string from a Pawn's thingIDNumber.
        /// Pure function — testable without RimWorld.
        /// </summary>
        public static string BuildNpcId(int thingId) => $"NPC-{thingId}";

        public const int MaxQueueWaitTicks = 60000;

        /// <summary>
        /// Builds the queue coalesce key for a pawn's personality evaluation request.
        /// </summary>
        public static string BuildCoalesceKey(string npcId) => $"{PersonalityRequestDefaults.ModId}:{npcId}";

        /// <summary>
        /// Builds a complete LLM request envelope for personality evaluation.
        /// </summary>
        public static LlmRequestEnvelope BuildEnvelope(string npcId, string? gameStateInfo)
        {
            return LlmRequestEnvelopeBuilder
                .ForScenario(RimMindAPI.Context.ScenarioPersonality)
                .WithModId(PersonalityRequestDefaults.ModId)
                .WithNpcId(npcId)
                .WithGameStateInfo(gameStateInfo)
                .WithSchema(PersonalityThoughtMapper.EvaluationSchema)
                .WithMaxTokens(PersonalityRequestDefaults.MaxTokens)
                .WithTemperature(PersonalityRequestDefaults.Temperature)
                .WithCoalesceKey(BuildCoalesceKey(npcId))
                .WithMaxQueueWaitTicks(MaxQueueWaitTicks)
                .Build();
        }

        /// <summary>
        /// Convenience: builds envelope directly from a Pawn thingIDNumber.
        /// </summary>
        public static LlmRequestEnvelope BuildForPawn(int thingId, string? gameStateInfo)
        {
            return BuildEnvelope(BuildNpcId(thingId), gameStateInfo);
        }
    }
}
