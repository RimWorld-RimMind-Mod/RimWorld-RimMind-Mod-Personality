using HarmonyLib;
using RimMind.Personality.Comps;
using RimMind.Presentation.Api;
using Verse;

namespace RimMind.Personality.Patches
{
    [HarmonyPatch(typeof(Pawn), "Kill")]
    static class Patch_PersonalityDeath
    {
        static void Postfix(Pawn __instance, DamageInfo? dinfo, Hediff exactCulprit)
        {
            var settings = RimMindPersonalityMod.Settings;
            if (!settings.enablePersonality || !settings.enableDeathTrigger) return;
            if (Find.Maps == null) return;

            // Iterate ALL maps, not just the killed pawn's current map: the killed
            // pawn's Map may be null after death, and related colonists on other
            // maps (multiple bases) must still receive the trigger.
            var killedPawn = __instance;
            string killedName = killedPawn?.Name != null ? killedPawn.Name.ToStringShort : (killedPawn?.LabelShort ?? "Colonist");
            foreach (var map in Find.Maps)
            {
                foreach (var pawn in RimMindPawnLookup.GetEligibleColonists(map))
                {
                    if (pawn == killedPawn) continue;
                    var rel = pawn.relations?.DirectRelations;
                    if (rel == null) continue;
                    foreach (var dr in rel)
                    {
                        if (dr.otherPawn == killedPawn)
                        {
                            string relLabel = dr.def?.LabelCap.RawText ?? dr.def?.defName ?? string.Empty;
                            PersonalityTriggerHelper.TriggerForPawn(pawn,
                                RimMindAPI.Text.TrOr(
                                    "RimMind.Personality.Trigger.RelationDeath",
                                    "{0} {1} died",
                                    relLabel,
                                    killedName),
                                TriggerEventType.Death);
                            break;
                        }
                    }
                }
            }
        }
    }
}
