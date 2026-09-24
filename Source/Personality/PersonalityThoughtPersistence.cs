using Verse;

namespace RimMind.Personality
{
    /// <summary>
    /// Stable save contract for dynamic Thought fields, isolated from the RimWorld Thought adapter.
    /// </summary>
    public static class PersonalityThoughtPersistence
    {
        public static void Look(
            ref string aiLabel,
            ref string aiDescription,
            ref int aiIntensity,
            ref int customDurationTicks)
        {
#pragma warning disable CS8601
            Scribe_Values.Look(ref aiLabel, "aiLabel", string.Empty);
            Scribe_Values.Look(ref aiDescription, "aiDesc", string.Empty);
#pragma warning restore CS8601
            Scribe_Values.Look(ref aiIntensity, "aiIntensity", 0);
            Scribe_Values.Look(ref customDurationTicks, "customDurationTicks", -1);
        }
    }
}
