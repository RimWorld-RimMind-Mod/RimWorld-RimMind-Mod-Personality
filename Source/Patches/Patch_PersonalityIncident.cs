using HarmonyLib;
using RimMind.Personality.Comps;
using RimWorld;
using Verse;

namespace RimMind.Personality.Patches
{
    [HarmonyPatch(typeof(IncidentWorker), "TryExecuteWorker")]
    static class Patch_PersonalityIncident
    {
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

            PersonalityTriggerHelper.TriggerForColonists((Map)parms.target,
                $"{"RimMind.Storyteller.Context.IncidentOccurred".Translate(def!.LabelCap)}",
                TriggerEventType.Incident);
        }
    }
}
