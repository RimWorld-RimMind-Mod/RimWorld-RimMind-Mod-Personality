using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using RimMind.Testing;
using Xunit;

namespace RimMind.Personality.Tests.Contracts
{
    public sealed class PersonalityEvaluationContracts
    {
        [Fact]
        public void Evaluation_math_dto_and_context_policy_are_stable()
        {
            ContractCaseRunner.Run(
                ("mood intensity clamps to the documented seven-value table", () =>
                {
                    var expected = new Dictionary<float, float>
                    {
                        [-4f] = -10f,
                        [-3f] = -10f,
                        [-2f] = -3f,
                        [-1f] = -1f,
                        [0f] = 0f,
                        [1f] = 1f,
                        [2f] = 3f,
                        [3f] = 10f,
                        [4f] = 10f
                    };

                    foreach (var pair in expected)
                        Assert.Equal(pair.Value, MoodOffsetCalculator.CalcMoodOffset(pair.Key));
                }),
                ("AI duration is clamped to one through twenty-four hours with fixed fallback", () =>
                {
                    Assert.Equal(2500, PersonalityThoughtPolicy.CalculateDurationTicks(
                        0.5f, ThoughtDurationMode.AIDecides, 24f));
                    Assert.Equal(60000, PersonalityThoughtPolicy.CalculateDurationTicks(
                        48f, ThoughtDurationMode.AIDecides, 24f));
                    Assert.Equal(5000, PersonalityThoughtPolicy.CalculateDurationTicks(
                        12f, ThoughtDurationMode.Fixed, 2f));
                    Assert.Equal(1, PersonalityThoughtPolicy.CalculateDurationTicks(
                        null, ThoughtDurationMode.Fixed, 0f));
                }),
                ("identity and thought DTOs round-trip without losing optional fields", () =>
                {
                    var dto = new PersonalityResultDto
                    {
                        narrative = "steady",
                        thoughts = new[]
                        {
                            new ThoughtEntryDto
                            {
                                type = "state",
                                label = "Focused",
                                description = "Working carefully",
                                intensity = 2,
                                duration_hours = 12
                            }
                        },
                        identity = new PersonalityIdentityDto
                        {
                            motivations = new[] { "mastery" },
                            traits = new[] { "careful" },
                            core_values = new[] { "community" }
                        }
                    };

                    var restored = JsonConvert.DeserializeObject<PersonalityResultDto>(
                        JsonConvert.SerializeObject(dto));

                    Assert.NotNull(restored);
                    Assert.Equal("steady", restored!.narrative);
                    Assert.Equal("Focused", Assert.Single(restored.thoughts).label);
                    Assert.Equal("mastery", Assert.Single(restored.identity!.motivations!));
                    Assert.Equal("careful", Assert.Single(restored.identity.traits!));
                    Assert.Equal("community", Assert.Single(restored.identity.core_values!));
                }),
                ("profile defaults are empty and collection-safe", () =>
                {
                    var profile = new Data.PersonalityProfile();
                    Assert.True(profile.IsEmpty);
                    Assert.NotNull(profile.playerShapingHistory);
                    Assert.Empty(profile.playerShapingHistory);
                }),
                ("personality context covers personality, dialogue, and split decision scenarios only", () =>
                {
                    Assert.True(PersonalityContextScenarioPolicy.IncludesPersonalityContext("Personality"));
                    Assert.True(PersonalityContextScenarioPolicy.IncludesPersonalityContext("Decision"));
                    Assert.True(PersonalityContextScenarioPolicy.IncludesPersonalityContext("agent_decision"));
                    Assert.True(PersonalityContextScenarioPolicy.IncludesPersonalityContext("advisor_decision"));
                    Assert.True(PersonalityContextScenarioPolicy.IncludesPersonalityContext("Dialogue"));
                    Assert.True(PersonalityContextScenarioPolicy.IncludesPersonalityContext("CrisisDialogue"));
                    Assert.False(PersonalityContextScenarioPolicy.IncludesPersonalityContext("Storyteller"));
                    Assert.False(PersonalityContextScenarioPolicy.IncludesPersonalityContext("Memory"));
                    Assert.False(PersonalityContextScenarioPolicy.IncludesPersonalityContext(null));
                }),
                ("public personality providers expose stable bridge data without RimTalk coupling", () =>
                {
                    string source = ReadSource("Personality", "PersonalityProviderRegistrar.cs")
                        .Replace("\r\n", "\n");

                    Assert.Contains(
                        "internal static void RegisterAll()\n" +
                        "        {\n" +
                        "            RegisterContextProviders();\n" +
                        "            RegisterPublicProviders();\n" +
                        "            RegisterAgentIdentityProvider();\n" +
                        "        }",
                        source,
                        StringComparison.Ordinal);
                    Assert.Contains(
                        "private const string PublicProviderOwner = \"RimMind.Personality\";\n" +
                        "        private const int PublicProviderPriority = 100;",
                        source,
                        StringComparison.Ordinal);
                    Assert.Contains(
                        "RegisterProfileField(\"personality.description\", profile => profile.description);\n" +
                        "            RegisterProfileField(\"personality.work_tendencies\", profile => profile.workTendencies);\n" +
                        "            RegisterProfileField(\"personality.social_tendencies\", profile => profile.socialTendencies);\n" +
                        "            RegisterProfileField(\"personality.ai_narrative\", profile => profile.aiNarrative);",
                        source,
                        StringComparison.Ordinal);
                    Assert.Contains(
                        "RimMindAPI.Providers.RegisterPawnProvider(\n" +
                        "                \"personality.shaping_history\",\n" +
                        "                PublicProviderOwner,",
                        source,
                        StringComparison.Ordinal);

                    // Providers must never create profiles (GetOrCreate is main-thread only) and every
                    // context provider body must read Verse state through the main-thread snapshot.
                    Assert.DoesNotContain("GetOrCreate(", source, StringComparison.Ordinal);
                    Assert.Contains("AIPersonalityWorldComponent.Instance?.TryGet(pawn)", source, StringComparison.Ordinal);
                    Assert.Contains("RimMindAPI.MainThreadSnapshot.GetAsync(", source, StringComparison.Ordinal);
                    Assert.DoesNotContain("async (ctx, ct)", source, StringComparison.Ordinal);
                    Assert.DoesNotContain("RimTalk", source, StringComparison.Ordinal);
                }),
                ("thought mapper checks IsActiveSpawnedColonist and profile cleanup delegates to Core retention & scoped cache", () =>
                {
                    string mapperSource = ReadSource("Personality", "PersonalityThoughtMapper.cs");
                    string profileSource = ReadSource("Data", "PersonalityProfile.cs");
                    string thoughtSource = ReadSource("Personality", "Thought_AIPersonality.cs");
                    string compSource = ReadSource("Comps", "CompAIPersonality.cs");
                    string bioTabSource = ReadSource("UI", "BioTabPersonalityPatch.cs");
                    string drawerSource = ReadSource("Settings", "PersonalitySettingsDrawer.cs");

                    Assert.Contains("!RimMindPawnLookup.IsActiveSpawnedColonist(pawn)", mapperSource, StringComparison.Ordinal);
                    Assert.DoesNotContain("AllPawnsAlive", profileSource, StringComparison.Ordinal);
                    Assert.Contains("RimMindPawnLookup.CollectRetainedColonyPawnIds()", profileSource, StringComparison.Ordinal);
                    Assert.Contains("WorldScopedComponentCache<AIPersonalityWorldComponent>", profileSource, StringComparison.Ordinal);
                    Assert.Contains("RimMindAPI.Thoughts.FormatCleanLabel(aiLabel)", thoughtSource, StringComparison.Ordinal);
                    Assert.Contains("TickStagger.PhaseOffset(", compSource, StringComparison.Ordinal);
                    Assert.DoesNotContain("new System.Random(", compSource, StringComparison.Ordinal);
                    Assert.Contains("IBioTabBadgeProvider", bioTabSource, StringComparison.Ordinal);
                    Assert.Contains("TryGet(pawn)", bioTabSource, StringComparison.Ordinal);
                    Assert.DoesNotContain("GetOrCreate(", bioTabSource, StringComparison.Ordinal);
                    Assert.DoesNotContain("[HarmonyPatch", bioTabSource, StringComparison.Ordinal);
                    Assert.DoesNotContain("AccessTools.Field", bioTabSource, StringComparison.Ordinal);
                    Assert.Contains("SettingsUIDrawer.DrawScrollableSettingsPage", drawerSource, StringComparison.Ordinal);
                    Assert.DoesNotContain("EstimateHeight", drawerSource, StringComparison.Ordinal);
                }),
                ("comp state transition contexts use guarded translation keys with English fallbacks", () =>
                {
                    string compSource = ReadSource("Comps", "CompAIPersonality.cs");
                    string[] expectedKeys = new[]
                    {
                        "RimMind.Personality.Prompt.TaskInstruction.TriggerReason",
                        "RimMind.Personality.Trigger.ShiftNight",
                        "RimMind.Personality.Trigger.ShiftDay",
                        "RimMind.Personality.Trigger.AwakeningNight",
                        "RimMind.Personality.Trigger.AwakeningDay",
                        "RimMind.Personality.Trigger.Dusk",
                        "RimMind.Personality.Trigger.Wandering",
                        "RimMind.Personality.Trigger.ContemplationIdle",
                        "RimMind.Personality.Trigger.ContemplationSkygaze",
                        "RimMind.Personality.Trigger.Recreation",
                        "RimMind.Personality.Trigger.MoodShift"
                    };
                    foreach (string key in expectedKeys)
                    {
                        Assert.Contains(key, compSource, StringComparison.Ordinal);
                    }
                    Assert.Contains("RimMindText.TrOr(key, fallback, args)", compSource, StringComparison.Ordinal);
                }));
        }

        [Fact]
        public void Evaluation_request_and_response_failures_are_isolated()
        {
            ContractCaseRunner.Run(
                ("NPC identity formatting is deterministic", () =>
                {
                    Assert.Equal("NPC-0", PersonalityRequestBuilder.BuildNpcId(0));
                    Assert.Equal("NPC-42", PersonalityRequestBuilder.BuildNpcId(42));
                    Assert.Equal("NPC--7", PersonalityRequestBuilder.BuildNpcId(-7));
                }),
                ("request envelope uses the personality scenario schema, limits, coalescing, and TTL", () =>
                {
                    var envelope = PersonalityRequestBuilder.BuildEnvelope("NPC-42", "injured");
                    Assert.Equal("Personality", envelope.ScenarioId);
                    Assert.Equal("RimMind.Personality", envelope.ModId);
                    Assert.Equal("NPC-42", envelope.NpcId);
                    Assert.True(envelope.GameStateInfo!.ContainsSection("perceptions"));
                    Assert.Contains("injured", envelope.GameStateInfo.ToXml(), StringComparison.Ordinal);
                    Assert.Equal("test-schema", envelope.JsonSchema);
                    Assert.Equal(600, envelope.MaxTokens);
                    Assert.Equal(0.8f, envelope.Temperature);
                    Assert.Equal("RimMind.Personality:NPC-42", envelope.CoalesceKey);
                    Assert.Equal(60000, envelope.MaxQueueWaitTicks);
                }),
                ("malformed responses use repair then return a safe null outcome", () =>
                {
                    int repairCalls = 0;
                    var repaired = PersonalityThoughtPolicy.ParseResponse(
                        "{\"narrative\":",
                        _ =>
                        {
                            repairCalls++;
                            return "{\"narrative\":\"repaired\",\"thoughts\":[]}";
                        });
                    var invalid = PersonalityThoughtPolicy.ParseResponse(
                        "{broken",
                        _ => "{still broken");

                    Assert.Equal(1, repairCalls);
                    Assert.Equal("repaired", repaired!.narrative);
                    Assert.Null(invalid);
                }),
                ("valid responses bypass repair and preserve the payload", () =>
                {
                    bool repairCalled = false;
                    var parsed = PersonalityThoughtPolicy.ParseResponse(
                        "{\"narrative\":\"direct\",\"thoughts\":[]}",
                        _ =>
                        {
                            repairCalled = true;
                            return null;
                        });

                    Assert.False(repairCalled);
                    Assert.Equal("direct", parsed!.narrative);
                }),
                ("markdown code fences, thinking blocks, and truncated JSON parse via Core RimMindAPI.Json", () =>
                {
                    var fenced = PersonalityThoughtPolicy.ParseResponse(
                        "<think>reflecting on raid</think>\n```json\n{\"narrative\":\"resilient\",\"thoughts\":[{\"label\":\"Steady\",\"description\":\"Kept calm\",\"intensity\":2}]}\n```");
                    Assert.NotNull(fenced);
                    Assert.Equal("resilient", fenced!.narrative);
                    Assert.Equal("Steady", Assert.Single(fenced.thoughts).label);

                    var repaired = PersonalityThoughtPolicy.ParseResponse(
                        "{\"narrative\":\"cut off\",\"thoughts\":[{\"label\":\"Alert\",\"description\":\"On guard\",\"intensity\":1");
                    Assert.NotNull(repaired);
                    Assert.Equal("cut off", repaired!.narrative);
                    Assert.Equal("Alert", Assert.Single(repaired.thoughts).label);

                    Assert.Null(PersonalityThoughtPolicy.ParseResponse("not json at all"));
                }));
        }

        private static string ReadSource(params string[] relativeParts)
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                string sourceRoot = Path.Combine(directory.FullName, "Source");
                string path = Path.Combine(sourceRoot, Path.Combine(relativeParts));
                if (File.Exists(path))
                    return File.ReadAllText(path);

                directory = directory.Parent;
            }

            throw new FileNotFoundException(
                $"Unable to locate Source/{string.Join("/", relativeParts)} from {AppContext.BaseDirectory}.");
        }
    }
}
