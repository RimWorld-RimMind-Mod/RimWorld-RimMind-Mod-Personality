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
                ("personality context is limited to its three consumer scenarios", () =>
                {
                    Assert.True(PersonalityContextScenarioPolicy.IncludesPersonalityContext("Personality"));
                    Assert.True(PersonalityContextScenarioPolicy.IncludesPersonalityContext("Decision"));
                    Assert.True(PersonalityContextScenarioPolicy.IncludesPersonalityContext("Dialogue"));
                    Assert.False(PersonalityContextScenarioPolicy.IncludesPersonalityContext("Storyteller"));
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
                        "                PublicProviderOwner,\n" +
                        "                pawn =>\n" +
                        "                {\n" +
                        "                    if (pawn == null) return null;\n" +
                        "                    var profile = AIPersonalityWorldComponent.Instance?.GetOrCreate(pawn);\n" +
                        "                    var history = profile?.playerShapingHistory;\n" +
                        "                    if (history == null || history.Count == 0) return string.Empty;\n" +
                        "\n" +
                        "                    var sb = new System.Text.StringBuilder(\"[RimMind Shaping]\");\n" +
                        "                    int start = Math.Max(0, history.Count - 5);\n" +
                        "                    for (int i = start; i < history.Count; i++)\n" +
                        "                    {\n" +
                        "                        var record = history[i];\n" +
                        "                        sb.AppendLine($\"- [{record.action}] {record.label}\");\n" +
                        "                    }\n" +
                        "                    return sb.ToString().TrimEnd();\n" +
                        "                },\n" +
                        "                PublicProviderPriority,\n" +
                        "                overrideExisting: true);",
                        source,
                        StringComparison.Ordinal);

                    int profileFieldStart = source.IndexOf(
                        "        private static void RegisterProfileField(",
                        StringComparison.Ordinal);
                    Assert.True(profileFieldStart >= 0, "RegisterProfileField method was not found.");
                    int profileFieldEnd = source.IndexOf(
                        "\n        private static void RegisterAgentIdentityProvider()",
                        profileFieldStart,
                        StringComparison.Ordinal);
                    Assert.True(profileFieldEnd > profileFieldStart, "RegisterProfileField method boundary was not found.");
                    string profileFieldSource = source.Substring(
                        profileFieldStart,
                        profileFieldEnd - profileFieldStart);

                    Assert.Contains(
                        "RimMindAPI.Providers.RegisterPawnProvider(\n" +
                        "                category,\n" +
                        "                PublicProviderOwner,\n" +
                        "                pawn =>\n" +
                        "                {\n" +
                        "                    if (pawn == null) return null;\n" +
                        "                    var profile = AIPersonalityWorldComponent.Instance?.GetOrCreate(pawn);\n" +
                        "                    return profile == null ? null : selector(profile);\n" +
                        "                },\n" +
                        "                PublicProviderPriority,\n" +
                        "                overrideExisting: true);",
                        profileFieldSource,
                        StringComparison.Ordinal);
                    Assert.DoesNotContain("RimTalk", source, StringComparison.Ordinal);
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
                ("request envelope uses the personality scenario schema and limits", () =>
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
