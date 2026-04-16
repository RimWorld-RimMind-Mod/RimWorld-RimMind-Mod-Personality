using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimMind.Personality.Comps;
using RimWorld;
using Verse;

namespace RimMind.Personality.Patches
{
    /// <summary>
    /// 在所有 ThingDef 解析完继承关系（ResolveReferences）之后，
    /// 动态为所有人形智能小人（含 AlienRaces 外星种族）注入 CompProperties_AIPersonality。
    ///
    /// 为什么不用 XML PatchOperation？
    ///   XML Patch 在继承解析前运行，raw XML 里没有 race/intelligence 字段（它们从父类继承而来），
    ///   所以 XPath 过滤 race/intelligence="Humanlike" 匹配 0 个节点。
    ///   Harmony Postfix 在解析后运行，可直接读取已解析的 race 字段，
    ///   一次性覆盖原版 Human + 所有 AlienRaces 种族 + 未来任何 mod 添加的人形种族。
    /// </summary>
    [HarmonyPatch(typeof(ThingDef), nameof(ThingDef.ResolveReferences))]
    public static class AddCompToHumanlikePatch
    {
        [HarmonyPostfix]
        public static void Postfix(ThingDef __instance)
        {
            // 只处理人形智能种族
            if (__instance.race?.intelligence != Intelligence.Humanlike) return;

            __instance.comps ??= new List<CompProperties>();

            // 避免重复注入（热重载或多次调用场景）
            if (__instance.comps.Any(c => c is CompProperties_AIPersonality)) return;

            __instance.comps.Add(new CompProperties_AIPersonality());
        }
    }
}
