using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Features.Context;
using RimMind.Domain.ValueObjects;
using RimMind.Personality.Data;
using RimMind.Presentation.Api;
using Verse;

namespace RimMind.Personality
{
    internal static class PersonalityProviderRegistrar
    {
        private const string PublicProviderOwner = "RimMind.Personality";
        private const int PublicProviderPriority = 100;

        internal static void RegisterAll()
        {
            RegisterContextProviders();
            RegisterPublicProviders();
            RegisterAgentIdentityProvider();
        }

        private static void RegisterPublicProviders()
        {
            RegisterProfileField("personality.description", profile => profile.description);
            RegisterProfileField("personality.work_tendencies", profile => profile.workTendencies);
            RegisterProfileField("personality.social_tendencies", profile => profile.socialTendencies);
            RegisterProfileField("personality.ai_narrative", profile => profile.aiNarrative);

            RimMindAPI.Providers.RegisterPawnProvider(
                "personality.shaping_history",
                PublicProviderOwner,
                pawn =>
                {
                    if (pawn == null) return null;
                    // Read-only lookup: synchronous public providers may be called from any thread.
                    var profile = AIPersonalityWorldComponent.Instance?.TryGet(pawn);
                    var history = profile?.playerShapingHistory?.ToArray();
                    if (history == null || history.Length == 0) return string.Empty;

                    return PersonalityContextText.FormatHeaderedLines(
                        "[RimMind Shaping]",
                        PersonalityContextText.TakeLast(history, 5)
                            .Where(record => record != null)
                            .Select(record => $"- [{record.action}] {record.label}")) ?? string.Empty;
                },
                PublicProviderPriority,
                overrideExisting: true);
        }

        private static void RegisterProfileField(
            string category,
            Func<PersonalityProfile, string?> selector)
        {
            RimMindAPI.Providers.RegisterPawnProvider(
                category,
                PublicProviderOwner,
                pawn =>
                {
                    if (pawn == null) return null;
                    var profile = AIPersonalityWorldComponent.Instance?.TryGet(pawn);
                    return profile == null ? null : selector(profile);
                },
                PublicProviderPriority,
                overrideExisting: true);
        }

        private static void RegisterAgentIdentityProvider()
        {
            RimMindAPI.RegisterAgentIdentityProvider(pawn =>
            {
                var profile = AIPersonalityWorldComponent.Instance?.TryGet(pawn);
                return profile?.agentIdentity;
            });
        }

        private static string Tr(string key, params object[] args)
            => RimMindText.TrOr(key, key ?? string.Empty, args);

        /// <summary>
        /// Context providers run on pipeline thread-pool threads; every read of pawns, profiles,
        /// memories and translations happens inside this main-thread snapshot.
        /// </summary>
        private static Task<string?> OnMainThread(Func<string?> build, CancellationToken ct)
            => RimMindAPI.MainThreadSnapshot.GetAsync(build, null, ct: ct);

        // No invalidation triggers: Personality publishes no bus event, and an unknown trigger name
        // is inert in Core. Cached keys refresh through their 750-tick staleness window.
        private static void RegisterContextProviders()
        {
            ScenarioRegistry.Register(new ScenarioDescriptor
            {
                Id = ScenarioIds.Personality,
                DefaultBaseRounds = 3,
                Description = "RimMind.Application.Scenario.Personality",
                DefaultBudget = 0.4f,
                L4Mode = L4Mode.MemoryOnly,
                DefaultExcludeKeys = new[] { "combat_status" },
                OwnerModId = PublicProviderOwner,
                RelevanceWeights = new Dictionary<string, float>(StringComparer.Ordinal)
                {
                    ["health"] = 0.5f,
                    ["mood"] = 0.85f,
                    ["current_job"] = 0.6f,
                    ["combat_status"] = 0.3f,
                    ["target_info"] = 0.2f,
                    ["task_progress"] = 0.4f,
                    ["nearby_pawns"] = 0.6f,
                    ["colony_status"] = 0.4f,
                    ["current_area"] = 0.3f,
                    ["weather"] = 0.1f,
                    ["time_of_day"] = 0.2f,
                    ["season"] = 0.1f,
                    ["map_structure"] = 0.2f,
                    ["pawn_base_info"] = 0.9f,
                    ["fixed_relations"] = 0.8f,
                    ["ideology"] = 0.85f,
                    ["skills_summary"] = 0.7f,
                    ["memory_pawn"] = 0.8f,
                    ["working_memory"] = 0.5f,
                    ["memory_narrator"] = 0.3f,
                    ["personality_profile"] = 1.0f,
                    ["personality_state"] = 0.95f,
                    ["personality_shaping"] = 0.9f,
                    ["personality_task"] = 0.95f
                }
            });

            RimMindAPI.Context.ContextKeys.Register(new ContextProviderDef(
                "personality_profile", ContextLayer.L3_State, 0.25f,
                (ctx, ct) =>
                {
                    if (!PersonalityContextScenarioPolicy.IncludesPersonalityContext(ctx.Scenario))
                        return Task.FromResult<string?>(null);
                    int pawnId = ctx.PawnId;
                    return OnMainThread(() => BuildProfileContext(pawnId), ct);
                }, PublicProviderOwner, stalenessTicks: 750));

            RimMindAPI.Context.ContextKeys.Register(new ContextProviderDef(
                "personality_state", ContextLayer.L3_State, 0.2f,
                (ctx, ct) =>
                {
                    if (!PersonalityContextScenarioPolicy.IncludesPersonalityContext(ctx.Scenario))
                        return Task.FromResult<string?>(null);
                    int pawnId = ctx.PawnId;
                    return OnMainThread(() => BuildStateContext(pawnId), ct);
                }, PublicProviderOwner, stalenessTicks: 750));

            RimMindAPI.Context.ContextKeys.Register(new ContextProviderDef(
                "personality_shaping", ContextLayer.L3_State, 0.15f,
                (ctx, ct) =>
                {
                    if (!PersonalityContextScenarioPolicy.IncludesPersonalityContext(ctx.Scenario))
                        return Task.FromResult<string?>(null);
                    int pawnId = ctx.PawnId;
                    return OnMainThread(() => BuildShapingContext(pawnId), ct);
                }, PublicProviderOwner, stalenessTicks: 750));

            RimMindAPI.Context.ContextKeys.Register(new ContextProviderDef(
                "personality_task", ContextLayer.L0_Static, 0.95f,
                (ctx, ct) =>
                {
                    if (ctx.Scenario != RimMindAPI.Context.ScenarioPersonality)
                        return Task.FromResult<string?>(null);
                    return OnMainThread(BuildTaskInstruction, ct);
                }, PublicProviderOwner, stalenessTicks: 0));
        }

        /// <summary>Main thread only.</summary>
        private static string? BuildProfileContext(int pawnId)
        {
            var pawn = PawnResolver.TryFindPawn(pawnId);
            if (pawn == null) return null;
            var profile = AIPersonalityWorldComponent.Instance?.TryGet(pawn);
            if (profile == null || profile.IsEmpty) return null;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(Tr("RimMind.Personality.Context.ProfileHeader", pawn.Name?.ToStringShort ?? ""));
            if (!profile.description.NullOrEmpty())
                sb.AppendLine(profile.description);
            if (!profile.workTendencies.NullOrEmpty())
                sb.AppendLine(Tr("RimMind.Personality.Context.WorkTendencies", profile.workTendencies));
            if (!profile.socialTendencies.NullOrEmpty())
                sb.AppendLine(Tr("RimMind.Personality.Context.SocialTendencies", profile.socialTendencies));
            if (!profile.aiNarrative.NullOrEmpty())
                sb.AppendLine(Tr("RimMind.Personality.Context.RecentState", profile.aiNarrative));
            return sb.ToString().TrimEnd();
        }

        /// <summary>Main thread only.</summary>
        private static string? BuildStateContext(int pawnId)
        {
            var pawn = PawnResolver.TryFindPawn(pawnId);
            var memories = pawn?.needs?.mood?.thoughts?.memories?.Memories;
            if (memories == null || memories.Count == 0) return null;

            var lines = new System.Collections.Generic.List<string>();
            for (int i = 0; i < memories.Count; i++)
            {
                var t = memories[i];
                if (t?.def == null || !Personality.PersonalityThoughtMapper.IsAIPersonalityDef(t.def.defName)) continue;

                string desc = (t as Thought_AIPersonality)?.aiDescription ?? t.def.label;
                float hours = t.DurationTicks / (float)RimMind.Domain.Common.RimMindTime.TicksPerHour;
                lines.Add(Tr("RimMind.Personality.Context.StateEntry", desc, $"{hours:F1}"));
            }
            return PersonalityContextText.FormatHeaderedLines(Tr("RimMind.Personality.Context.StateHeader"), lines);
        }

        /// <summary>Main thread only.</summary>
        private static string? BuildShapingContext(int pawnId)
        {
            var pawn = PawnResolver.TryFindPawn(pawnId);
            if (pawn == null) return null;
            var history = AIPersonalityWorldComponent.Instance?.TryGet(pawn)?.playerShapingHistory;
            if (history == null || history.Count == 0) return null;
            int maxCount = RimMindPersonalityMod.Settings?.shapingHistoryMaxCount ?? 20;

            var lines = PersonalityContextText.TakeLast(history, maxCount)
                .Where(r => r != null)
                .Select(r =>
                {
                    string actionLabel = ShapingActionExtensions.FromString(r.action) switch
                    {
                        ShapingAction.Reinforce => Tr("RimMind.Personality.ShapingAction.Reinforce"),
                        ShapingAction.Suppress => Tr("RimMind.Personality.ShapingAction.Suppress"),
                        _ => Tr("RimMind.Personality.ShapingAction.Ignore")
                    };
                    return $"- {r.label}: {actionLabel}";
                })
                .ToList();
            return PersonalityContextText.FormatHeaderedLines(Tr("RimMind.Personality.Context.ShapingHistoryHeader"), lines);
        }

        /// <summary>Main thread only.</summary>
        private static string? BuildTaskInstruction()
        {
            string baseInstruction = RimMindAPI.Prompt.BuildTaskInstruction("RimMind.Personality.Prompt.TaskInstruction", null,
                "Role", "Goal", "Process", "Constraint", "Example", "Output", "Fallback",
                "JsonFormatDirect", "LabelHint", "DescHint",
                "NarrativeHint", "DurationHint", "DiversityHint");
            int maxSlots = PersonalityThoughtPolicy.OwnedSlotCount;
            string evalInst = Tr("RimMind.Personality.Prompt.TaskInstruction.EvalInstruction", maxSlots);
            return string.IsNullOrEmpty(evalInst) ? baseInstruction : $"{baseInstruction}\n\n{evalInst}";
        }
    }
}
