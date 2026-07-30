using System.Collections.Generic;
using System.Linq;
using RimMind.Personality.Data;
using RimMind.Testing;
using Verse;
using Xunit;

namespace RimMind.Personality.Tests.Contracts
{
    public sealed class PersonalityPersistenceContracts
    {
        [Fact]
        public void Settings_defaults_and_save_keys_cover_every_stable_option()
        {
            ContractCaseRunner.Run(
                ("feature and trigger defaults are enabled", () =>
                {
                    var settings = new AIPersonalitySettings();
                    Assert.True(settings.enablePersonality);
                    Assert.True(settings.showNotifications);
                    Assert.True(settings.enableDailyEval);
                    Assert.True(settings.enableInjuryTrigger);
                    Assert.True(settings.enableSkillTrigger);
                    Assert.True(settings.enableIncidentTrigger);
                    Assert.True(settings.enableDeathTrigger);
                    Assert.True(settings.enableShapingVote);
                }),
                ("duration request and history defaults remain stable", () =>
                {
                    var settings = new AIPersonalitySettings();
                    Assert.Equal(24f, settings.thoughtDurationHours);
                    Assert.Equal(ThoughtDurationMode.AIDecides, settings.durationMode);
                    Assert.Equal(30000, settings.requestExpireTicks);
                    Assert.Equal(20, settings.shapingHistoryMaxCount);
                    Assert.Equal(60000, settings.dailyIntervalTicks);
                    Assert.Equal(3000, settings.jitterRangeTicks);
                    Assert.Equal(1200, settings.eventCooldownTicks);
                    Assert.Equal(60000, settings.requestTimeoutTicks);
                }),
                ("every settings field participates in Scribe persistence with stable defaults", () =>
                {
                    ScribeRecorder.Reset();
                    new AIPersonalitySettings().ExposeData();

                    string[] persistedFields =
                    {
                        "enablePersonality",
                        "showNotifications",
                        "enableDailyEval",
                        "enableInjuryTrigger",
                        "enableSkillTrigger",
                        "enableIncidentTrigger",
                        "enableDeathTrigger",
                        "thoughtDurationHours",
                        "durationMode",
                        "showLabelPrefix",
                        "requestExpireTicks",
                        "enableShapingVote",
                        "shapingHistoryMaxCount",
                        "dailyIntervalTicks",
                        "jitterRangeTicks",
                        "eventCooldownTicks",
                        "requestTimeoutTicks"
                    };
                    Assert.Equal(persistedFields, ScribeRecorder.Labels);
                    Assert.Equal(
                        new (string Label, object? DefaultValue)[]
                        {
                            ("enablePersonality", true),
                            ("showNotifications", true),
                            ("enableDailyEval", true),
                            ("enableInjuryTrigger", true),
                            ("enableSkillTrigger", true),
                            ("enableIncidentTrigger", true),
                            ("enableDeathTrigger", true),
                            ("thoughtDurationHours", 24f),
                            ("durationMode", ThoughtDurationMode.AIDecides),
                            ("showLabelPrefix", true),
                            ("requestExpireTicks", 30000),
                            ("enableShapingVote", true),
                            ("shapingHistoryMaxCount", 20),
                            ("dailyIntervalTicks", 60000),
                            ("jitterRangeTicks", 3000),
                            ("eventCooldownTicks", 1200),
                            ("requestTimeoutTicks", 60000),
                        },
                        ScribeRecorder.ValueCalls);
                }));
        }

        [Fact]
        public void Profile_and_Thought_save_contracts_preserve_compatibility()
        {
            ContractCaseRunner.Run(
                ("profile values and shaping history use stable save labels", () =>
                {
                    ScribeRecorder.Reset();
                    new PersonalityProfile().ExposeData();
                    string[] labels =
                    {
                        "description",
                        "workTendencies",
                        "socialTendencies",
                        "aiNarrative",
                        "lastNarrativeUpdateTick",
                        "rimTalkSynced",
                        "playerShapingHistory",
                        "agentIdentity",
                    };
                    Assert.Equal(labels, ScribeRecorder.Labels);
                    Assert.Equal(
                        new (string Label, object? DefaultValue)[]
                        {
                            ("description", string.Empty),
                            ("workTendencies", string.Empty),
                            ("socialTendencies", string.Empty),
                            ("aiNarrative", string.Empty),
                            ("lastNarrativeUpdateTick", 0),
                            ("rimTalkSynced", false),
                        },
                        ScribeRecorder.ValueCalls);
                }),
                ("legacy RimTalk synchronization state remains consumable", () =>
                {
                    ScribeRecorder.Reset();
                    new PersonalityProfile().ExposeData();
                    Assert.Contains("rimTalkSynced", ScribeRecorder.Labels);
                }),
                ("AI Thought description keeps the historical aiDesc key", () =>
                {
                    ScribeRecorder.Reset();
                    string label = "L";
                    string description = "D";
                    int intensity = 2;
                    int duration = 2500;
                    PersonalityThoughtPersistence.Look(
                        ref label,
                        ref description,
                        ref intensity,
                        ref duration);

                    Assert.Equal(
                        new[] { "aiLabel", "aiDesc", "aiIntensity", "customDurationTicks" },
                        ScribeRecorder.Labels);
                    Assert.DoesNotContain("aiDescription", ScribeRecorder.Labels);
                    Assert.Equal(
                        new (string Label, object? DefaultValue)[]
                        {
                            ("aiLabel", string.Empty),
                            ("aiDesc", string.Empty),
                            ("aiIntensity", 0),
                            ("customDurationTicks", -1),
                        },
                        ScribeRecorder.ValueCalls);
                }),
                ("world component persists profiles by pawn id", () =>
                {
                    ScribeRecorder.Reset();
                    var profiles = new Dictionary<int, PersonalityProfile>();
                    PersonalityProfilePersistence.Look(ref profiles);
                    Assert.Equal("profiles", Assert.Single(ScribeRecorder.Labels));
                    Assert.Equal(
                        ("profiles", LookMode.Value, LookMode.Deep),
                        Assert.Single(ScribeRecorder.CollectionCalls));
                }));
        }

        [Fact]
        public void Malformed_or_partial_saved_state_recovers_safe_collections()
        {
            ContractCaseRunner.Run(
                ("missing shaping history becomes an empty list", () =>
                {
                    ScribeRecorder.Reset();
                    Scribe_Collections.AssignNullOnLook = true;
                    var profile = new PersonalityProfile();
                    profile.ExposeData();
                    Assert.NotNull(profile.playerShapingHistory);
                    Assert.Empty(profile.playerShapingHistory);
                }),
                ("missing identity becomes an empty identity object", () =>
                {
                    ScribeRecorder.Reset();
                    Scribe_Deep.AssignNullOnLook = true;
                    var profile = new PersonalityProfile();
                    profile.ExposeData();
                    Assert.NotNull(profile.agentIdentity);
                }),
                ("missing world profile dictionary becomes empty", () =>
                {
                    ScribeRecorder.Reset();
                    Scribe_Collections.AssignNullOnLook = true;
                    var profiles = new Dictionary<int, PersonalityProfile>();
                    PersonalityProfilePersistence.Look(ref profiles);
                    Assert.NotNull(profiles);
                    Assert.Empty(profiles);
                }));
        }
    }
}
