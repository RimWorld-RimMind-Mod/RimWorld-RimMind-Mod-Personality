using System.Collections.Generic;
using HarmonyLib;
using RimMind.Personality.Comps;
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
            // pawn's Map may be null after death, and related colonists may be on
            // other maps (caravans, multiple bases) and must still receive the trigger.
            var killedPawn = __instance;
            foreach (var map in Find.Maps)
            {
                foreach (var pawn in map.mapPawns.FreeColonists)
                {
                    if (pawn == killedPawn) continue;
                    var rel = pawn.relations?.DirectRelations;
                    if (rel == null) continue;
                    foreach (var dr in rel)
                    {
                        if (dr.otherPawn == killedPawn)
                        {
                            PersonalityTriggerHelper.TriggerForPawn(pawn,
                                $"{"RimMind.Memory.Trigger.RelationDeath".Translate(dr.def.LabelCap, killedPawn.Name.ToStringShort)}",
                                TriggerEventType.Death);
                            break;
                        }
                    }
                }
            }
        }
    }
}
