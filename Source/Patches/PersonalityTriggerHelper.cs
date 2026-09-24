using RimMind.Personality.Comps;
using RimWorld;
using Verse;

namespace RimMind.Personality.Patches
{
    /// <summary>
    /// Shared helper for event-trigger Patches.
    /// Encapsulates the "find CompAIPersonality → TriggerEvent" pattern
    /// used by Injury, Death, Incident, and Skill patches.
    /// </summary>
    public static class PersonalityTriggerHelper
    {
        /// <summary>
        /// Triggers a personality evaluation for a single pawn if it has the comp.
        /// </summary>
        public static void TriggerForPawn(Pawn pawn, string context, TriggerEventType eventType)
        {
            var comp = pawn.GetComp<CompAIPersonality>();
            if (comp != null)
                comp.TriggerEvent(context, eventType);
        }

        /// <summary>
        /// Triggers a personality evaluation for all free colonists on a map.
        /// </summary>
        public static void TriggerForColonists(Map map, string context, TriggerEventType eventType)
        {
            foreach (var pawn in map.mapPawns.FreeColonists)
            {
                TriggerForPawn(pawn, context, eventType);
            }
        }
    }
}
