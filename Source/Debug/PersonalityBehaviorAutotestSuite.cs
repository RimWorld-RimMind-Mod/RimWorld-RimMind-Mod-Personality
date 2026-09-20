using System;
using System.Linq;
using LudeonTK;
using RimMind.Personality.Comps;
using RimMind.Personality.Data;
using RimMind.Presentation.Api;
using RimWorld;
using Verse;

namespace RimMind.Personality.Debug
{
    /// <summary>
    /// In-game behavioral autotest suite for RimMind-Personality.
    /// Discovered automatically by Core's BehaviorAutotestRunner and exposed to Dev menu.
    /// </summary>
    public sealed class PersonalityBehaviorAutotestSuite : BehaviorAutotestSuiteBase
    {
        public override string ModId => "Personality";
        public override string SuiteId => "Behavior.PersonalityCore";

        [DebugAction("Autotests", "Run Personality In-Game Behavior Test", actionType = DebugActionType.Action)]
        public static void RunFromDevMenu() => RunSuiteFromDevMenu<PersonalityBehaviorAutotestSuite>();

        public override void RunSuite(IInGameBehaviorSuiteContext context)
        {
            Pawn? pawn = context.ActiveColonist;

            // 1. CompAIPersonality Attachment
            if (context.CurrentMap?.mapPawns?.FreeColonists != null && context.CurrentMap.mapPawns.FreeColonists.Count > 0)
            {
                bool anyAttached = context.CurrentMap.mapPawns.FreeColonists.Any(p => p.GetComp<CompAIPersonality>() != null);
                context.Assert(anyAttached, "Free colonists on map have CompAIPersonality attached");
            }
            else if (pawn != null)
            {
                context.Assert(pawn.GetComp<CompAIPersonality>() != null, "Active colonist has CompAIPersonality attached");
            }

            // 2. AIPersonalityWorldComponent & PersonalityProfile
            var worldComp = AIPersonalityWorldComponent.Instance;
            context.Assert(worldComp != null, "AIPersonalityWorldComponent is available in loaded world");

            if (worldComp != null && pawn != null)
            {
                try
                {
                    var profile = worldComp.GetOrCreate(pawn);
                    context.Assert(profile != null, $"PersonalityProfile retrieved/created for {pawn.Name.ToStringShort}");

                    if (profile != null)
                    {
                        var record = new ShapingRecord
                        {
                            action = "autotest_shaping",
                            tick = Find.TickManager.TicksGame,
                            label = "Verified shaping history addition"
                        };

                        int countBefore = profile.playerShapingHistory.Count;
                        profile.AddShapingRecord(record, 20);

                        context.Assert(profile.playerShapingHistory.Count == countBefore + 1, "ShapingRecord appended successfully to playerShapingHistory");
                        context.Assert(profile.playerShapingHistory.Last().action == "autotest_shaping", "Latest shaping record matches autotest action");

                        // Self-cleanup
                        profile.playerShapingHistory.Remove(record);
                        context.Assert(profile.playerShapingHistory.Count == countBefore, "Test shaping record cleaned up cleanly");
                    }
                }
                catch (Exception ex)
                {
                    context.Assert(false, $"PersonalityProfile operations threw exception: {ex.Message}");
                }
            }

            // 3. MoodOffsetCalculator Table Assertions
            float maxMood = MoodOffsetCalculator.CalcMoodOffset(3);
            float minMood = MoodOffsetCalculator.CalcMoodOffset(-3);
            float zeroMood = MoodOffsetCalculator.CalcMoodOffset(0);
            float posMood = MoodOffsetCalculator.CalcMoodOffset(1);

            context.Assert(maxMood == 10f, $"Intensity +3 maps to +10 mood offset (got: {maxMood})");
            context.Assert(minMood == -10f, $"Intensity -3 maps to -10 mood offset (got: {minMood})");
            context.Assert(zeroMood == 0f, $"Intensity 0 maps to 0 mood offset (got: {zeroMood})");
            context.Assert(posMood == 1f, $"Intensity +1 maps to +1 mood offset (got: {posMood})");
        }
    }
}
