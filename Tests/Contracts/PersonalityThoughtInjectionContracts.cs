using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Personality.Comps;
using RimMind.Personality.Data;
using RimMind.Testing;
using Xunit;

namespace RimMind.Personality.Tests.Contracts
{
    public sealed class PersonalityThoughtInjectionContracts
    {
        [Fact]
        public void Thought_projection_uses_three_owned_slots_and_preserves_AI_values()
        {
            ContractCaseRunner.Run(
                ("exactly three owned Thought slots are registered", () =>
                {
                    var entries = Enumerable.Range(0, 4)
                        .Select(index => new ThoughtEntryDto { label = $"L{index}" })
                        .ToArray();
                    var projected = PersonalityThoughtPolicy.Project(
                        entries,
                        ThoughtDurationMode.Fixed,
                        1f);

                    Assert.Equal(3, PersonalityThoughtPolicy.OwnedSlotCount);
                    Assert.Equal(3, projected.Count);
                    Assert.Equal("AIPersonality_Slot_0", projected[0].SlotDefName);
                    Assert.Equal("AIPersonality_Slot_2", projected[2].SlotDefName);
                }),
                ("AI label description intensity and duration map into the Thought", () =>
                {
                    var entry = new ThoughtEntryDto
                    {
                        label = "Focused",
                        description = "Working carefully",
                        intensity = 2,
                        duration_hours = 4f,
                    };
                    var projection = Assert.Single(PersonalityThoughtPolicy.Project(
                        new[] { entry },
                        ThoughtDurationMode.AIDecides,
                        24f));

                    Assert.Same(entry, projection.Source);
                    Assert.Equal(10000, projection.DurationTicks);
                }),
                ("old owned Thoughts are removed without touching foreign Thoughts", () =>
                {
                    Assert.True(PersonalityThoughtPolicy.IsOwnedThoughtDef("AIPersonality_Slot_0"));
                    Assert.True(PersonalityThoughtPolicy.IsOwnedThoughtDef("AIPersonality_Slot_2"));
                    Assert.False(PersonalityThoughtPolicy.IsOwnedThoughtDef("ForeignThought"));
                    Assert.False(PersonalityThoughtPolicy.IsOwnedThoughtDef(null));
                }),
                ("mood offset delegates to the shared clamped calculator", () =>
                {
                    Assert.Equal(-10f, MoodOffsetCalculator.CalcMoodOffset(-99));
                    Assert.Equal(3f, MoodOffsetCalculator.CalcMoodOffset(2));
                    Assert.Equal(10f, MoodOffsetCalculator.CalcMoodOffset(99));
                }));
        }

        [Fact]
        public void Trigger_filters_and_shaping_actions_preserve_eligibility_boundaries()
        {
            ContractCaseRunner.Run(
                ("all four trigger types have a settings gate", () =>
                ("all trigger types have a settings gate", () =>
                {
                    var settings = new AIPersonalitySettings
                    {
                        enableInjuryTrigger = false,
                        enableSkillTrigger = false,
                        enableIncidentTrigger = false,
                        enableDeathTrigger = false,
                        enableSunriseTrigger = false,
                        enableRecreationTrigger = false,
                        enableSkygazeTrigger = false,
                        enableMoodSwingTrigger = false,
                    };
                    foreach (TriggerEventType eventType in System.Enum.GetValues(typeof(TriggerEventType)))
                        Assert.False(PersonalityTriggerPolicy.IsTriggerEnabled(eventType, settings));

                    settings.enableDeathTrigger = true;
                    Assert.True(PersonalityTriggerPolicy.IsTriggerEnabled(TriggerEventType.Death, settings));
                    settings.enableDeathTrigger = false;

                    settings.enableSunriseTrigger = true;
                    Assert.True(PersonalityTriggerPolicy.IsTriggerEnabled(TriggerEventType.Sunrise, settings));

                    // Verify state-transition probability policies
                    Assert.True(PersonalityTriggerPolicy.ShouldTriggerSunrise(true, 0.30f, 0.20f, true));
                    Assert.False(PersonalityTriggerPolicy.ShouldTriggerSunrise(true, 0.30f, 0.50f, true));
                    Assert.False(PersonalityTriggerPolicy.ShouldTriggerSunrise(true, 0.30f, 0.10f, false));

                    Assert.True(PersonalityTriggerPolicy.ShouldTriggerRecreation(true, 0.25f, 0.15f));
                    Assert.False(PersonalityTriggerPolicy.ShouldTriggerRecreation(true, 0.25f, 0.35f));

                    Assert.True(PersonalityTriggerPolicy.ShouldTriggerSkygaze(true, 0.40f, 0.20f));
                    Assert.False(PersonalityTriggerPolicy.ShouldTriggerSkygaze(false, 0.40f, 0.20f));

                    Assert.True(PersonalityTriggerPolicy.ShouldTriggerMoodSwing(true, 0.50f, 0.25f, -0.20f));
                    Assert.False(PersonalityTriggerPolicy.ShouldTriggerMoodSwing(true, 0.50f, 0.25f, 0.05f));
                }),
                ("eligible pawns are living free colonists with map and mood", () =>
                {
                    Assert.True(PersonalityTriggerPolicy.IsPawnEligible(true, false, true, true));
                    Assert.False(PersonalityTriggerPolicy.IsPawnEligible(false, false, true, true));
                    Assert.False(PersonalityTriggerPolicy.IsPawnEligible(true, true, true, true));
                    Assert.False(PersonalityTriggerPolicy.IsPawnEligible(true, false, false, true));
                    Assert.False(PersonalityTriggerPolicy.IsPawnEligible(true, false, true, false));
                }),
                ("injury incident and skill patches retain their event filters", () =>
                {
                    Assert.False(PersonalityTriggerPolicy.ShouldTriggerInjury(
                        true, true, true, 0.19f, true));
                    Assert.True(PersonalityTriggerPolicy.ShouldTriggerInjury(
                        true, true, true, 0.2f, true));
                    Assert.True(PersonalityTriggerPolicy.ShouldTriggerIncident(
                        true, true, true, "ThreatBig"));
                    Assert.False(PersonalityTriggerPolicy.ShouldTriggerIncident(
                        true, true, true, "Misc"));
                    Assert.True(PersonalityTriggerPolicy.ShouldTriggerSkill(
                        true, true, 4, 5));
                    Assert.False(PersonalityTriggerPolicy.ShouldTriggerSkill(
                        true, true, 5, 5));
                }),
                ("shaping action strings round-trip and unknown input is ignored", () =>
                {
                    Assert.Equal("reinforce", ShapingAction.Reinforce.ToActionString());
                    Assert.Equal("suppress", ShapingAction.Suppress.ToActionString());
                    Assert.Equal("ignored", ShapingAction.Ignore.ToActionString());
                    Assert.Equal(ShapingAction.Reinforce, ShapingActionExtensions.FromString("reinforce"));
                    Assert.Equal(ShapingAction.Suppress, ShapingActionExtensions.FromString("suppress"));
                    Assert.Equal(ShapingAction.Ignore, ShapingActionExtensions.FromString("unknown"));
                    Assert.Equal(ShapingAction.Ignore, ShapingActionExtensions.FromString(null));
                    Assert.True(PersonalityThoughtPolicy.ShouldRecordShapingAction(ShapingAction.Reinforce));
                    Assert.False(PersonalityThoughtPolicy.ShouldRecordShapingAction(ShapingAction.Ignore));
                }));
        }

        [Fact]
        public void Shaping_history_is_bounded_and_retains_the_newest_records()
        {
            ContractCaseRunner.Run(
                ("history always keeps at least one newest record", () =>
                {
                    var profile = new PersonalityProfile();
                    profile.AddShapingRecord(new ShapingRecord { label = "old" }, 0);
                    profile.AddShapingRecord(new ShapingRecord { label = "new" }, 0);
                    Assert.Equal("new", Assert.Single(profile.playerShapingHistory).label);
                }),
                ("overflow removes the oldest range only", () =>
                {
                    var profile = new PersonalityProfile();
                    for (int index = 0; index < 5; index++)
                        profile.AddShapingRecord(new ShapingRecord { label = index.ToString() }, 3);

                    Assert.Equal(new[] { "2", "3", "4" },
                        profile.playerShapingHistory.Select(record => record.label));
                }),
                ("ignored votes do not enter shaping history", () =>
                {
                    Assert.False(PersonalityThoughtPolicy.ShouldRecordShapingAction(ShapingAction.Ignore));
                    Assert.True(PersonalityThoughtPolicy.ShouldRecordShapingAction(ShapingAction.Suppress));
                }));
        }

        [Fact]
        public void Shaping_candidate_selection_filters_minor_thoughts_and_picks_peak_intensity()
        {
            ContractCaseRunner.Run(
                ("low intensity thoughts are ignored to avoid player fatigue", () =>
                {
                    var entries = new[]
                    {
                        new ThoughtEntryDto { label = "minor mood", intensity = 1 },
                        new ThoughtEntryDto { label = "casual thought", intensity = -1 },
                    };
                    var candidate = PersonalityThoughtPolicy.SelectSignificantShapingCandidate(entries, minIntensity: 2);
                    Assert.Null(candidate);
                }),
                ("single pivotal thought is selected", () =>
                {
                    var entries = new[]
                    {
                        new ThoughtEntryDto { label = "casual thought", intensity = 1 },
                        new ThoughtEntryDto { label = "profound realization", intensity = 2 },
                    };
                    var candidate = PersonalityThoughtPolicy.SelectSignificantShapingCandidate(entries, minIntensity: 2);
                    Assert.NotNull(candidate);
                    Assert.Equal("profound realization", candidate!.label);
                }),
                ("peaks with greatest absolute intensity among multiple pivotal thoughts", () =>
                {
                    var entries = new[]
                    {
                        new ThoughtEntryDto { label = "strong trauma", intensity = -3 },
                        new ThoughtEntryDto { label = "notable joy", intensity = 2 },
                    };
                    var candidate = PersonalityThoughtPolicy.SelectSignificantShapingCandidate(entries, minIntensity: 2);
                    Assert.NotNull(candidate);
                    Assert.Equal("strong trauma", candidate!.label);
                }),
                ("null or empty inputs safely evaluate to null", () =>
                {
                    Assert.Null(PersonalityThoughtPolicy.SelectSignificantShapingCandidate((IEnumerable<ThoughtEntryDto>?)null));
                    Assert.Null(PersonalityThoughtPolicy.SelectSignificantShapingCandidate(Array.Empty<ThoughtEntryDto>()));
                }));
        }
    }
}
