using System.Collections.Generic;
using HarmonyLib;
using RimMind.Personality.Comps;
using RimWorld;
using Verse;

namespace RimMind.Personality.Patches
{
    [HarmonyPatch(typeof(SkillRecord), "Learn")]
    static class Patch_PersonalitySkill
    {
        internal static readonly Dictionary<int, int> PreLevels = new Dictionary<int, int>();

        static void Prefix(SkillRecord __instance)
        {
            if (!RimMindPersonalityMod.Settings.enableSkillTrigger) return;
            PreLevels[__instance.GetHashCode()] = __instance.levelInt;
        }

        static void Postfix(SkillRecord __instance)
        {
            if (!RimMindPersonalityMod.Settings.enableSkillTrigger) return;
            var hash = __instance.GetHashCode();
            if (!PreLevels.TryGetValue(hash, out int preLevel)) return;
            PreLevels.Remove(hash);

            if (__instance.levelInt <= preLevel) return;

            foreach (var map in Find.Maps)
            {
                foreach (var pawn in map.mapPawns.FreeColonists)
                {
                    if (pawn.skills == null) continue;
                    var field = AccessTools.Field(typeof(Pawn_SkillTracker), "skills");
                    var skills = field?.GetValue(pawn.skills) as List<SkillRecord>;
                    if (skills != null && skills.Contains(__instance))
                    {
                        var comp = pawn.GetComp<CompAIPersonality>();
                        if (comp != null)
                            comp.TriggerEvent($"{"RimMind.Memory.Trigger.SkillUp".Translate(__instance.def.LabelCap, __instance.levelInt, preLevel, __instance.levelInt)}", TriggerEventType.Skill);
                        return;
                    }
                }
            }
        }
    }
}
