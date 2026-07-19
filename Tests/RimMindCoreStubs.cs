// Test-only stubs for RimMind-Core types referenced by PersonalityRequestBuilder.cs.
// The real types live in RimMind-Core (which requires RimWorld) and are not
// included in this pure-logic test project. Only the surface area needed to
// compile PersonalityRequestBuilder.cs is stubbed; BuildEnvelope/BuildForPawn
// are not exercised by tests (only BuildNpcId is pure-testable).

namespace RimMind.Application.Features.Llm
{
    using RimMind.Domain.Llm;

    public sealed class LlmRequestEnvelopeBuilder
    {
        public static LlmRequestEnvelopeBuilder ForScenario(string scenarioId) => new LlmRequestEnvelopeBuilder();

        public LlmRequestEnvelopeBuilder WithModId(string modId) => this;
        public LlmRequestEnvelopeBuilder WithNpcId(string npcId) => this;
        public LlmRequestEnvelopeBuilder WithGameStateInfo(string? gameStateInfo) => this;
        public LlmRequestEnvelopeBuilder WithSchema(string jsonSchema) => this;
        public LlmRequestEnvelopeBuilder WithMaxTokens(int max) => this;
        public LlmRequestEnvelopeBuilder WithTemperature(float t) => this;

        public LlmRequestEnvelope Build() => new LlmRequestEnvelope();
    }
}

namespace RimMind.Domain.Llm
{
    public sealed class LlmRequestEnvelope
    {
        public string? ScenarioId;
        public string? ModId;
        public string? NpcId;
    }
}

namespace RimMind.Presentation.Api
{
    public static class RimMindAPI
    {
        public static class Context
        {
            public static string ScenarioPersonality => "Personality";
            public static string ScenarioDecision => "Decision";
            public static string ScenarioDialogue => "Dialogue";
        }
    }
}

namespace RimMind.Personality
{
    /// <summary>
    /// Test-only stub. Real PersonalityThoughtMapper lives in Source/Personality/
    /// and depends on RimMind-Core + RimWorld, so it is not compiled into tests.
    /// </summary>
    public static class PersonalityThoughtMapper
    {
        public static readonly string EvaluationSchema = "test-schema";
        public const int DefaultMaxTokens = 600;
        public const float DefaultTemperature = 0.8f;
    }
}
