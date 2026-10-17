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
                    PersonalityTriggerPolicy.IsEligible(pawn)))
            {
                return;
            }

            PersonalityTriggerHelper.TriggerForPawn(pawn!,
                $"{"RimMind.Personality.Trigger.Contracted".Translate(hediff!.LabelCap, "RimMind.Personality.Trigger.FullBody".Translate())}",
                TriggerEventType.Injury);
        }
    }
}
