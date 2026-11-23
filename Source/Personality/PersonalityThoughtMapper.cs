using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;
using RimMind.Application.Common.Models.UI;
using RimMind.Presentation.Api;
using RimMind.Application.Common.Models.Agent;
using RimMind.Personality.Data;
using RimWorld;
using Verse;

namespace RimMind.Personality
{
    public static class PersonalityThoughtMapper
    {
        public const string SchemaKey = "PersonalityOutput";
        public const string PersonalityOutputSchema =
            "{\"type\":\"object\",\"properties\":{\"thoughts\":{\"type\":\"array\",\"items\":{\"type\":\"object\",\"properties\":{\"type\":{\"type\":\"string\"},\"label\":{\"type\":\"string\"},\"description\":{\"type\":\"string\"},\"intensity\":{\"type\":\"number\"},\"duration_hours\":{\"type\":\"number\"}},\"required\":[\"label\",\"description\",\"intensity\"]}},\"narrative\":{\"type\":\"string\"},\"identity\":{\"type\":\"object\",\"properties\":{\"motivations\":{\"type\":\"array\",\"items\":{\"type\":\"string\"}},\"traits\":{\"type\":\"array\",\"items\":{\"type\":\"string\"}},\"core_values\":{\"type\":\"array\",\"items\":{\"type\":\"string\"}}}}},\"required\":[\"thoughts\",\"narrative\"]}";

        public static string EvaluationSchema
        {
            get
            {
                string? registered = RimMindAPI.Context.FindSchema(SchemaKey);
                return !string.IsNullOrEmpty(registered) ? registered! : PersonalityOutputSchema;
            }
        }

        public const int DefaultMaxTokens = PersonalityRequestDefaults.MaxTokens;
        public const float DefaultTemperature = PersonalityRequestDefaults.Temperature;

        public static void Apply(Result<LlmResponse, RimMindError> result, Pawn pawn)
        {
            if (pawn == null || !RimMindPawnLookup.IsActiveSpawnedColonist(pawn) || pawn.needs?.mood == null) return;

            string pawnName = pawn.Name?.ToStringShort ?? pawn.LabelShort;
            if (result.IsErr)
            {
                if (result.Error.Code != RimMindErrorCode.Cancelled)
                    RimMindErrors.Warn($"[RimMind-Personality] Request failed ({pawnName}): {result.Error}");
                return;
            }

            var dto = PersonalityThoughtPolicy.ParseResponse(result.Value.Content);
            if (dto == null)
            {
                RimMindErrors.Warn($"[RimMind-Personality] Response parse failed ({pawnName}):\n{result.Value.Content}");
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
                var existing = profile.GetAgentIdentitySnapshot();
                var updated = new AgentIdentity
                {
                    Motivations = dto.identity.motivations != null
                        ? new List<string>(dto.identity.motivations)
                        : (existing?.Motivations != null ? new List<string>(existing.Motivations) : new List<string>()),
                    PersonalityTraits = dto.identity.traits != null
                        ? new List<string>(dto.identity.traits)
                        : (existing?.PersonalityTraits != null ? new List<string>(existing.PersonalityTraits) : new List<string>()),
                    CoreValues = dto.identity.core_values != null
                        ? new List<string>(dto.identity.core_values)
                        : (existing?.CoreValues != null ? new List<string>(existing.CoreValues) : new List<string>())
                };
                profile.SetAgentIdentity(updated);
            }
        }

        /// <summary>
        /// Registers a pending shaping-vote request for a single thought entry.
        /// </summary>
        private static void RegisterShapingVote(Pawn pawn, ThoughtEntryDto entry, AIPersonalitySettings? settings)
        {
            if (RimMindPersonalityMod.Settings?.enableShapingVote != true) return;

            string optReinforce = RimMindAPI.Text.TrOr("RimMind.Personality.Shaping.Reinforce", "Reinforce");
            string optSuppress = RimMindAPI.Text.TrOr("RimMind.Personality.Shaping.Suppress", "Suppress");
            string optIgnore = RimMindAPI.Text.TrOr("RimMind.Personality.Shaping.Ignore", "Ignore");
            string optMuteVote = RimMindAPI.Text.TrOr("RimMind.Personality.Shaping.MuteVote", "Mute");
            string optMuteVoteTip = RimMindAPI.Text.TrOr(
                "RimMind.Personality.Shaping.MuteVote.Tip",
                "Disable future personality shaping vote popups (can be re-enabled in Personality settings or the colonist's Personality window).");
            string titleText = RimMindAPI.Text.TrOr("RimMind.Personality.Shaping.NewTrait", "New Personality Trait");

            var capturedEntry = entry;

            RimMindAPI.Bus.RegisterPendingRequest(new RequestEntry
            {
                source = "personality",
                pawn = pawn,
                title = titleText,
                description = $"{capturedEntry.label}: {capturedEntry.description}",
                options = new[] { optReinforce, optSuppress, optIgnore, optMuteVote },
                optionTooltips = new[] { string.Empty, string.Empty, string.Empty, optMuteVoteTip },
                expireTicks = settings?.requestExpireTicks ?? 30000,
                callback = choice =>
                {
                    if (choice == optMuteVote)
                    {
                        if (RimMindPersonalityMod.Settings != null)
                        {
                            RimMindPersonalityMod.Settings.enableShapingVote = false;
                            RimMindPersonalityMod.Settings.Write();
                        }
                        return;
                    }

                    if (!RimMindPawnLookup.IsActiveSpawnedColonist(pawn)) return;

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
            if (pawn == null || !RimMindPawnLookup.IsActiveSpawnedColonist(pawn)) return;
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
                pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(thought);
            }

            var shapingCandidate = PersonalityThoughtPolicy.SelectSignificantShapingCandidate(
                projections.Select(p => p.Source));
            if (shapingCandidate != null)
            {
                RegisterShapingVote(pawn, shapingCandidate, settings);
            }

            if (showNotifications && projections.Count > 0 && pawn.Spawned && pawn.Map != null)
            {
                string pawnName = pawn.Name?.ToStringShort ?? pawn.LabelShort;
                Messages.Message(
                    RimMindAPI.Text.TrOr("RimMind.Personality.UI.PersonalityUpdated", "[RimMind] {0}'s personality state has been updated.", pawnName),
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
