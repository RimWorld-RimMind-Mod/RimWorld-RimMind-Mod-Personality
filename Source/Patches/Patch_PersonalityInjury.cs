using HarmonyLib;
using RimMind.Personality.Comps;
using RimWorld;
using Verse;

namespace RimMind.Personality.Patches
{
    [HarmonyPatch(typeof(HediffSet), "AddDirect")]
    static class Patch_PersonalityInjury
    {
        static void Postfix(HediffSet __instance, Hediff hediff)
        {
            var pawn = __instance.pawn;
            if (!PersonalityTriggerPolicy.ShouldTriggerInjury(
                    RimMindPersonalityMod.Settings.enableInjuryTrigger,
                    hediff != null,
                    hediff?.def.isBad == true,
                    hediff?.Severity ?? 0f,
                    pawn?.IsFreeNonSlaveColonist == true))
            {
                return;
            }

            PersonalityTriggerHelper.TriggerForPawn(pawn!,
                $"{"RimMind.Memory.Trigger.Contracted".Translate(hediff!.LabelCap, "RimMind.Memory.Trigger.FullBody".Translate())}",
                TriggerEventType.Injury);
        }
    }
}
