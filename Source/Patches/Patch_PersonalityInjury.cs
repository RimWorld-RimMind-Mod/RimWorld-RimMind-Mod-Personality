using HarmonyLib;
using RimMind.Personality.Comps;
using RimMind.Presentation.Api;
using Verse;

namespace RimMind.Personality.Patches
{
    [HarmonyPatch(typeof(HediffSet), "AddDirect")]
    static class Patch_PersonalityInjury
    {
        static void Postfix(HediffSet __instance, Hediff hediff)
        {
            var settings = RimMindPersonalityMod.Settings;
            if (!settings.enablePersonality) return;

            var pawn = __instance.pawn;
            if (!PersonalityTriggerPolicy.ShouldTriggerInjury(
                    settings.enableInjuryTrigger,
                    hediff != null,
                    hediff?.def?.isBad == true,
                    hediff?.Severity ?? 0f,
                    PersonalityTriggerPolicy.IsEligible(pawn)))
            {
                return;
            }

            string partLabel = hediff!.Part?.Label
                ?? RimMindAPI.Text.TrOr("RimMind.Personality.Trigger.FullBody", "full body");
            string hediffLabel = hediff.LabelCap.NullOrEmpty()
                ? (hediff.def?.defName ?? string.Empty)
                : hediff.LabelCap;
            PersonalityTriggerHelper.TriggerForPawn(pawn!,
                RimMindAPI.Text.TrOr(
                    "RimMind.Personality.Trigger.Contracted",
                    "Suffered {0} ({1})",
                    hediffLabel,
                    partLabel),
                TriggerEventType.Injury);
        }
    }
}
