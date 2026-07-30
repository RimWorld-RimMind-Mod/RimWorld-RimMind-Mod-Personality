using System;
using System.Collections.Generic;
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
        public const int DefaultMaxTokens = PersonalityRequestDefaults.MaxTokens;
        public const float DefaultTemperature = PersonalityRequestDefaults.Temperature;

        public static void Apply(Result<LlmResponse, RimMindError> result, Pawn pawn)
        {
            if (result.IsErr)
            {
                RimMindErrors.Warn($"[RimMind-Personality] Request failed ({pawn.Name.ToStringShort}): {result.Error}");
                return;
            }

            var dto = PersonalityThoughtPolicy.ParseResponse(
                result.Value.Content,
                RimMindAPI.Json.TryRepairTruncatedJson);
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

                    if (PersonalityThoughtPolicy.ShouldRecordShapingAction(shapingAction))
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
            var projections = PersonalityThoughtPolicy.Project(
                dto.thoughts,
                settings.durationMode,
                settings.thoughtDurationHours);
            foreach (var projection in projections)
            {
                var thoughtDef = DefDatabase<ThoughtDef>.GetNamedSilentFail(projection.SlotDefName);
                if (thoughtDef == null)
                {
                    RimMindErrors.Warn($"[RimMind-Personality] ThoughtDef '{projection.SlotDefName}' not found.");
                    continue;
                }

                ThoughtEntryDto entry = projection.Source;
                var thought = (Thought_AIPersonality)ThoughtMaker.MakeThought(thoughtDef);
                thought.aiLabel = entry.label;
                thought.aiDescription = entry.description;
                thought.aiIntensity = (int)entry.intensity;
                thought.customDurationTicks = projection.DurationTicks;
                pawn.needs.mood.thoughts.memories.TryGainMemory(thought);

                RegisterShapingVote(pawn, entry, settings);
            }

            if (showNotifications && projections.Count > 0)
            {
                Messages.Message(
                    "RimMind.Personality.UI.PersonalityUpdated".Translate(pawn.Name.ToStringShort),
                    pawn,
                    MessageTypeDefOf.SilentInput,
                    historical: false);
            }
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
            return PersonalityThoughtPolicy.IsOwnedThoughtDef(defName);
        }

    }
}
