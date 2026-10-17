using System.Collections.Generic;
using HarmonyLib;
using RimMind.Personality.Comps;
using RimMind.Presentation.Api;
using RimWorld;
using Verse;

namespace RimMind.Personality.Patches
{
    [HarmonyPatch(typeof(Pawn), "Kill")]
    static class Patch_PersonalityDeath
    {
        static void Postfix(Pawn __instance, DamageInfo? dinfo, Hediff exactCulprit)
        {
            if (!RimMindPersonalityMod.Settings.enableDeathTrigger) return;

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
                            PersonalityTriggerHelper.TriggerForPawn(pawn,
                                $"{"RimMind.Personality.Trigger.RelationDeath".Translate(dr.def.LabelCap, killedName)}",
                                TriggerEventType.Death);
                            break;
                        }
                    }
                }
            }
        }
    }
}
