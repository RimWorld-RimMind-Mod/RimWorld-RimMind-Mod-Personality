using HarmonyLib;
using RimMind.Personality.Comps;
using RimWorld;
using Verse;

namespace RimMind.Personality.Patches
{
    // Patch the public, non-virtual entry point. TryExecuteWorker is protected virtual and is
    // overridden by concrete workers (raids, etc.), so a base-method patch almost never fires.
    [HarmonyPatch(typeof(IncidentWorker), nameof(IncidentWorker.TryExecute))]
    static class Patch_PersonalityIncident
    {
        private const string IncidentOccurredKey = "RimMind.Personality.Trigger.IncidentOccurred";

        static void Postfix(IncidentWorker __instance, IncidentParms parms, bool __result)
        {
            var def = __instance.def;
            bool targetIsMap = parms.target is Map;
            if (!PersonalityTriggerPolicy.ShouldTriggerIncident(
                    __result,
                    RimMindPersonalityMod.Settings.enableIncidentTrigger,
                    targetIsMap,
                    def?.category?.defName))
            {
                return;
            }

            string context = IncidentOccurredKey.CanTranslate()
                ? IncidentOccurredKey.Translate(def!.LabelCap).ToString()
                : $"Incident occurred: {def!.LabelCap}";
            PersonalityTriggerHelper.TriggerForColonists((Map)parms.target, context, TriggerEventType.Incident);
        }
    }
}
