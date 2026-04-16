using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimMind.Personality.Comps;
using RimMind.Personality.Data;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimMind.Personality.UI
{
    [HarmonyPatch(typeof(CharacterCardUtility), "DoTopStack")]
    public static class BioTabPersonalityPatch
    {
        private static void AddPersonalityButton(Pawn pawn)
        {
            if (pawn.GetComp<CompAIPersonality>() == null) return;

            var tmpStackElements = (List<GenUI.AnonymousStackElement>?)
                AccessTools.Field(typeof(CharacterCardUtility), "tmpStackElements")?.GetValue(null);
            if (tmpStackElements == null) return;

            string label = "RimMind.Personality.UI.BioTab.Label".Translate();
            float textW = Text.CalcSize(label).x;
            float totalW = textW + 16f;

            tmpStackElements.Add(new GenUI.AnonymousStackElement
            {
                width = totalW,
                drawer = rect =>
                {
                    Widgets.DrawOptionBackground(rect, false);
                    Widgets.DrawHighlightIfMouseover(rect);

                    var profile = AIPersonalityWorldComponent.Instance?.GetOrCreate(pawn);
                    string tip = (profile != null && !profile.aiNarrative.NullOrEmpty())
                        ? "RimMind.Personality.UI.BioTab.NarrativeTip".Translate(profile.aiNarrative)
                        : "RimMind.Personality.UI.BioTab.NotEvaluatedTip".Translate();
                    TooltipHandler.TipRegion(rect, tip);

                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(rect, label);
                    Text.Anchor = TextAnchor.UpperLeft;

                    if (Widgets.ButtonInvisible(rect))
                        Find.WindowStack.Add(new Dialog_PersonalityProfile(pawn));
                }
            });
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo anchor = AccessTools.Method(
                typeof(QuestUtility),
                nameof(QuestUtility.AppendInspectStringsFromQuestParts),
                new Type[] { typeof(Action<string, Quest>), typeof(ISelectable), typeof(int).MakeByRefType() });

            foreach (var instr in instructions)
            {
                yield return instr;
                if (anchor != null && instr.Calls(anchor))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call,
                        AccessTools.Method(typeof(BioTabPersonalityPatch), nameof(AddPersonalityButton)));
                }
            }
        }
    }
}
