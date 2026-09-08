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
        public const int DefaultMaxTokens = PersonalityRequestDefaults.MaxTokens;
        public const float DefaultTemperature = PersonalityRequestDefaults.Temperature;
    }
}
