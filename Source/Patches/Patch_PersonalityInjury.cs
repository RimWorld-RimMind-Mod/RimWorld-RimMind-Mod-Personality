using HarmonyLib;
using RimMind.Personality.Comps;
using RimWorld;
using Verse;

namespace RimMind.Personality.Patches
{
    [HarmonyPatch(typeof(HediffSet), "AddHediff")]
    static class Patch_PersonalityInjury
    {
        static void Postfix(HediffSet __instance, Hediff hediff)
        {
            if (!RimMindPersonalityMod.Settings.enableInjuryTrigger) return;
            if (hediff == null) return;
            var pawn = __instance.pawn;
            if (pawn == null || !pawn.IsColonist) return;

            var comp = pawn.GetComp<CompAIPersonality>();
            if (comp == null) return;
            comp.TriggerEvent($"{"RimMind.Memory.Trigger.Contracted".Translate(hediff.LabelCap, "RimMind.Memory.Trigger.FullBody".Translate())}", TriggerEventType.Injury);
        }
    }
}
