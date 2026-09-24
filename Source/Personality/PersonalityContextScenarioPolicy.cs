using RimMind.Presentation.Api;

namespace RimMind.Personality
{
    internal static class PersonalityContextScenarioPolicy
    {
        public static bool IncludesPersonalityContext(string? scenario)
            => scenario == RimMindAPI.Context.ScenarioPersonality
                || scenario == RimMindAPI.Context.ScenarioDecision
                || scenario == RimMindAPI.Context.ScenarioDialogue;
    }
}
