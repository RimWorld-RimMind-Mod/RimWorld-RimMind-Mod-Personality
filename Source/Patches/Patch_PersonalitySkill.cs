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

            var settings = RimMindPersonalityMod.Settings;
            if (!settings.enablePersonality) return;

            int currentLevel = __instance.Level;
            if (!PersonalityTriggerPolicy.ShouldTriggerSkill(
                    settings.enableSkillTrigger,
                    preLevelWasCaptured: true,
                    __state,
                    currentLevel))
            {
                return;
            }

            Pawn pawn = __instance.Pawn;
            if (!RimMindPawnLookup.IsActiveSpawnedColonist(pawn)) return;

            string defName = __instance.def?.defName ?? string.Empty;
            RimMindAPI.GameEvents.Publish(new SkillLevelUpGameEvent(
                pawn.thingIDNumber,
                defName,
                __state,
                currentLevel));

            string skillLabel = (__instance.def?.LabelCap.RawText.NullOrEmpty() ?? true)
                ? defName
                : __instance.def!.LabelCap.RawText;
            PersonalityTriggerHelper.TriggerForPawn(
                pawn,
                RimMindAPI.Text.TrOr(
                    "RimMind.Personality.Trigger.SkillUp",
                    "{0} skill increased to level {1} ({2}→{3})",
                    skillLabel,
                    currentLevel,
                    __state,
                    currentLevel),
                TriggerEventType.Skill);
        }
    }
}
