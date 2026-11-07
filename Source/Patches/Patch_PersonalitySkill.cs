using HarmonyLib;
using RimMind.Application.Features.GameEvents;
using RimMind.Personality.Comps;
using RimMind.Presentation.Api;
using RimWorld;
using Verse;

namespace RimMind.Personality.Patches
{
    [HarmonyPatch(typeof(SkillRecord), "Learn")]
    static class Patch_PersonalitySkill
    {
        static void Prefix(SkillRecord __instance, float xp, out int __state)
        {
            __state = xp > 0f && !__instance.TotallyDisabled ? __instance.Level : -1;
        }

        static void Postfix(SkillRecord __instance, float xp, int __state)
        {
            if (__state < 0) return;

            int currentLevel = __instance.Level;
            if (!PersonalityTriggerPolicy.ShouldTriggerSkill(
                    RimMindPersonalityMod.Settings.enableSkillTrigger,
                    preLevelWasCaptured: true,
                    __state,
                    currentLevel))
            {
                return;
            }

            Pawn pawn = __instance.Pawn;
            if (!RimMindPawnLookup.IsActiveSpawnedColonist(pawn)) return;

            RimMindAPI.GameEvents.Publish(new SkillLevelUpGameEvent(
                pawn.thingIDNumber,
                __instance.def.defName ?? string.Empty,
                __state,
                currentLevel));

            PersonalityTriggerHelper.TriggerForPawn(
                pawn,
                $"{"RimMind.Personality.Trigger.SkillUp".Translate(__instance.def.LabelCap, currentLevel, __state, currentLevel)}",
                TriggerEventType.Skill);
        }
    }
}
