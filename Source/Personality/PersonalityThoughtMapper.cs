using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;
using RimMind.Application.Common.Interfaces.UI;
using RimMind.Application.Common.Models.UI;
using RimMind.Presentation.Api;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Models.Agent;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Personality.Data;
using RimWorld;
using Verse;

namespace RimMind.Personality
{
    public static class PersonalityThoughtMapper
    {
        public static readonly string EvaluationSchema = RimMindAPI.Context.SchemaPersonalityOutput;
        public const int DefaultMaxTokens = 600;
        public const float DefaultTemperature = 0.8f;

        private static readonly string[] SlotDefNames = new[]
        {
            "AIPersonality_Slot_0",
            "AIPersonality_Slot_1",
            "AIPersonality_Slot_2",
        };

        private const int TicksPerHour = 2500;

        public static void Apply(Result<LlmResponse, RimMindError> result, Pawn pawn)
        {
            if (result.IsErr)
            {
                RimMindErrors.Warn($"[RimMind-Personality] Request failed ({pawn.Name.ToStringShort}): {result.Error}");
                return;
            }

            var dto = ParseResponse(result.Value.Content);
            if (dto == null)
            {
                RimMindErrors.Warn($"[RimMind-Personality] Response parse failed ({pawn.Name.ToStringShort}):\n{result.Value.Content}");
                return;
            }

            var profile = AIPersonalityWorldComponent.Instance?.GetOrCreate(pawn);
            if (profile != null)
                UpdateProfile(profile, dto);

            ApplyThoughts(pawn, dto, RimMindPersonalityMod.Settings);
        }

        /// <summary>
        /// Parses the LLM response content into a PersonalityResultDto.
        /// Attempts direct deserialization, then truncated-JSON repair as fallback.
        /// Returns null if both attempts fail.
        /// </summary>
        private static PersonalityResultDto? ParseResponse(string? content)
        {
            if (string.IsNullOrEmpty(content)) return null;

            try
            {
                var dto = JsonConvert.DeserializeObject<PersonalityResultDto>(content!);
                if (dto != null) return dto;
            }
            catch { }

            string? trimmed = RimMindAPI.Json.TryRepairTruncatedJson(content ?? "");
            if (trimmed != null)
            {
                try { return JsonConvert.DeserializeObject<PersonalityResultDto>(trimmed); }
                catch { }
            }

            return null;
        }

        /// <summary>
        /// Updates the PersonalityProfile's narrative and AgentIdentity from the DTO.
        /// </summary>
        private static void UpdateProfile(PersonalityProfile profile, PersonalityResultDto dto)
        {
            if (!dto.narrative.NullOrEmpty())
            {
                profile.aiNarrative = dto.narrative;
                profile.lastNarrativeUpdateTick = Find.TickManager.TicksGame;
            }

            if (dto.identity != null)
            {
                if (profile.agentIdentity == null)
                    profile.agentIdentity = new AgentIdentity();
                if (dto.identity.motivations != null)
                    profile.agentIdentity.Motivations = new List<string>(dto.identity.motivations);
                if (dto.identity.traits != null)
                    profile.agentIdentity.PersonalityTraits = new List<string>(dto.identity.traits);
                if (dto.identity.core_values != null)
                    profile.agentIdentity.CoreValues = new List<string>(dto.identity.core_values);
            }
        }

        /// <summary>
        /// Registers a pending shaping-vote request for a single thought entry.
        /// </summary>
        private static void RegisterShapingVote(Pawn pawn, ThoughtEntryDto entry, AIPersonalitySettings? settings)
        {
            if (RimMindPersonalityMod.Settings?.enableShapingVote != true) return;

            string optReinforce = "RimMind.Personality.Shaping.Reinforce".Translate();
            string optSuppress = "RimMind.Personality.Shaping.Suppress".Translate();
            string optIgnore = "RimMind.Personality.Shaping.Ignore".Translate();

            var capturedEntry = entry;

            RimMindAPI.RegisterPendingRequest(new RequestEntry
            {
                source = "personality",
                pawn = pawn,
                title = "RimMind.Personality.Shaping.NewTrait".Translate(),
                description = $"{capturedEntry.label}: {capturedEntry.description}",
                options = new[] { optReinforce, optSuppress, optIgnore },
                expireTicks = settings?.requestExpireTicks ?? 30000,
                callback = choice =>
                {
                    var shapingAction = choice == optReinforce ? ShapingAction.Reinforce
                        : choice == optSuppress ? ShapingAction.Suppress
                        : ShapingAction.Ignore;

                    if (shapingAction != ShapingAction.Ignore)
                    {
                        var profile = AIPersonalityWorldComponent.Instance?.GetOrCreate(pawn);
                        if (profile != null)
                        {
                            profile.AddShapingRecord(new ShapingRecord
                            {
                                label = capturedEntry.label,
                                action = shapingAction.ToActionString(),
                                tick = Find.TickManager.TicksGame,
                            }, settings?.shapingHistoryMaxCount ?? 20);
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Removes old thoughts and creates new ones from the DTO, registering shaping votes.
        /// </summary>
        private static void ApplyThoughts(Pawn pawn, PersonalityResultDto dto, AIPersonalitySettings? settings)
        {
            RemoveAllAIPersonalityThoughts(pawn);

            if (settings == null) return;

            bool showNotifications = settings.showNotifications;
            int slotIndex = 0;
            dto.thoughts ??= Array.Empty<ThoughtEntryDto>();
            foreach (var entry in dto.thoughts)
            {
                if (slotIndex >= SlotDefNames.Length) break;

                var thoughtDef = DefDatabase<ThoughtDef>.GetNamedSilentFail(SlotDefNames[slotIndex]);
                if (thoughtDef == null)
                {
                    RimMindErrors.Warn($"[RimMind-Personality] ThoughtDef '{SlotDefNames[slotIndex]}' not found.");
                    slotIndex++;
                    continue;
                }

                var thought = (Thought_AIPersonality)ThoughtMaker.MakeThought(thoughtDef);
                thought.aiLabel = entry.label;
                thought.aiDescription = entry.description;
                thought.aiIntensity = (int)entry.intensity;
                thought.customDurationTicks = CalcDurationTicks(entry, settings);
                pawn.needs.mood.thoughts.memories.TryGainMemory(thought);

                RegisterShapingVote(pawn, entry, settings);
                slotIndex++;
            }

            if (showNotifications && dto.thoughts.Length > 0)
            {
                Messages.Message(
                    "RimMind.Personality.UI.PersonalityUpdated".Translate(pawn.Name.ToStringShort),
                    pawn,
                    MessageTypeDefOf.SilentInput,
                    historical: false);
            }
        }

        private static int CalcDurationTicks(ThoughtEntryDto entry, AIPersonalitySettings? settings)
        {
            if (settings == null) return TicksPerHour;
            if (settings.durationMode == ThoughtDurationMode.AIDecides
                && entry.duration_hours.HasValue
                && entry.duration_hours.Value > 0)
            {
                return (int)(Math.Clamp(entry.duration_hours.Value, 1f, 24f) * TicksPerHour);
            }
            return System.Math.Max(1, (int)(settings.thoughtDurationHours * TicksPerHour));
        }

        public static void RemoveAllAIPersonalityThoughts(Pawn pawn)
        {
            var memories = pawn.needs?.mood?.thoughts?.memories;
            if (memories == null) return;

            var toRemove = new System.Collections.Generic.List<Thought_Memory>();
            foreach (var t in memories.Memories)
            {
                if (IsAIPersonalityDef(t.def.defName))
                    toRemove.Add(t);
            }
            foreach (var t in toRemove)
                memories.RemoveMemory(t);
        }

        public static bool IsAIPersonalityDef(string defName)
        {
            foreach (var s in SlotDefNames)
                if (s == defName) return true;
            return false;
        }

    }
}
