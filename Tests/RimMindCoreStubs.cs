using RimMind.Application.Features.Json;

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

        public static class Json
        {
            public static string StripThinkingAndCodeFences(string? raw)
                => LlmPayloadParser.StripThinkingAndCodeFences(raw);

            public static string? ExtractJsonPayload(string? raw)
                => LlmPayloadParser.ExtractJsonPayload(raw);

            public static bool TryDeserialize<T>(string json, out T? result) where T : class
                => JsonExtractorUtils.TryDeserialize<T>(json, out result);

            public static bool TryDeserializeWithRepair<T>(string? raw, out T? value) where T : class
                => LlmPayloadParser.TryDeserializeWithRepair<T>(raw, out value);

            public static string? TryRepairTruncatedJson(string input)
                => JsonRepairer.TryRepairTruncatedJson(input);
        }
    }

    public static class RimMindPawnLookup
    {
        public static bool IsEligibleColonist(Verse.Pawn? pawn)
        {
            if (pawn == null) return false;
            if (pawn.Dead || pawn.Destroyed) return false;
            if (!pawn.Spawned || pawn.Map == null) return false;
            if (!pawn.IsColonist || pawn.IsPrisoner || pawn.IsSlave) return false;
            if (pawn.Downed || pawn.MentalState != null) return false;
            return true;
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
