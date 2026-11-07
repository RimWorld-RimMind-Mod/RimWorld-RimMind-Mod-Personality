using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RimMind.Personality;
using RimMind.Personality.Comps;
using RimMind.Testing;
using Xunit;

namespace RimMind.Personality.Tests.Contracts
{
    /// <summary>
    /// Regression contracts for Harmony entry points and queue identity.
    /// </summary>
    public sealed class PersonalityPatchTargetContracts
    {
        [Fact]
        public void Incident_trigger_hooks_the_public_entry_point_and_queue_identity_stays_aligned()
        {
            ContractCaseRunner.Run(
                ("incident patch targets IncidentWorker.TryExecute, not the overridden TryExecuteWorker", () =>
                {
                    string source = ReadSource("Patches", "Patch_PersonalityIncident.cs");
                    var attribute = Regex.Match(source, @"\[HarmonyPatch\(typeof\(IncidentWorker\),\s*(?<target>[^\)]*\)?)\)\]");
                    Assert.True(attribute.Success, "Patch_PersonalityIncident must declare a HarmonyPatch on IncidentWorker.");
                    Assert.Equal("nameof(IncidentWorker.TryExecute)", attribute.Groups["target"].Value.Trim());
                    Assert.Contains("bool __result", source, StringComparison.Ordinal);
                    Assert.Contains("PersonalityTriggerPolicy.ShouldTriggerIncident", source, StringComparison.Ordinal);
                }),
                ("only successful threat incidents on maps trigger", () =>
                {
                    Assert.True(PersonalityTriggerPolicy.ShouldTriggerIncident(true, true, true, "ThreatBig"));
                    Assert.False(PersonalityTriggerPolicy.ShouldTriggerIncident(false, true, true, "ThreatBig"));
                    Assert.False(PersonalityTriggerPolicy.ShouldTriggerIncident(true, true, false, "ThreatSmall"));
                    Assert.False(PersonalityTriggerPolicy.ShouldTriggerIncident(true, true, true, "Misc"));
                }),
                ("queue cooldown owner and envelope ModId use the same module constant", () =>
                {
                    var literalCooldownKey = new Regex(@"(ClearModCooldown|GetModCooldownTicksLeft)\(\s*""");
                    var literalModId = new Regex(@"WithModId\(\s*""(?<id>[^""]*)""");
                    foreach (var file in Directory.EnumerateFiles(SourceRoot(), "*.cs", SearchOption.AllDirectories)
                                 .Where(path => !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)))
                    {
                        string text = File.ReadAllText(file);
                        Assert.False(literalCooldownKey.IsMatch(text), $"{Path.GetFileName(file)} passes a literal cooldown key.");
                        foreach (Match match in literalModId.Matches(text))
                            Assert.Equal(PersonalityRequestDefaults.ModId, match.Groups["id"].Value);
                    }

                    string cooldown = ReadSource("Extensions", "PersonalityModCooldown.cs");
                    Assert.Contains("OwnerModId => PersonalityRequestDefaults.ModId", cooldown, StringComparison.Ordinal);
                }));
        }

        private static string ReadSource(params string[] relativeParts)
            => File.ReadAllText(Path.Combine(SourceRoot(), Path.Combine(relativeParts)));

        private static string SourceRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null &&
                   !Directory.Exists(Path.Combine(directory.FullName, "RimMind-Personality", "Source")))
                directory = directory.Parent;

            return Path.Combine(
                directory?.FullName ?? throw new InvalidOperationException("Repository root not found."),
                "RimMind-Personality",
                "Source");
        }
    }
}
