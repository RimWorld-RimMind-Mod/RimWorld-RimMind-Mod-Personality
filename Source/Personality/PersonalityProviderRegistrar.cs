using System;
using System.Linq;
using RimMind.Application.Common.Interfaces.Context;
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
                    var profile = AIPersonalityWorldComponent.Instance?.GetOrCreate(pawn);
                    var history = profile?.playerShapingHistory;
                    if (history == null || history.Count == 0) return string.Empty;

                    var sb = new System.Text.StringBuilder("[RimMind Shaping]");
                    int start = Math.Max(0, history.Count - 5);
                    for (int i = start; i < history.Count; i++)
                    {
                        var record = history[i];
                        sb.AppendLine($"- [{record.action}] {record.label}");
                    }
                    return sb.ToString().TrimEnd();
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
                    var profile = AIPersonalityWorldComponent.Instance?.GetOrCreate(pawn);
                    return profile == null ? null : selector(profile);
                },
                PublicProviderPriority,
                overrideExisting: true);
        }

        private static void RegisterAgentIdentityProvider()
        {
            RimMindAPI.RegisterAgentIdentityProvider(pawn =>
            {
                var profile = AIPersonalityWorldComponent.Instance?.GetOrCreate(pawn);
                return profile?.agentIdentity;
            });
        }

        private static string Tr(string key, params object[] args)
        {
            try
            {
                if (string.IsNullOrEmpty(key)) return string.Empty;
                string template = key.CanTranslate() ? key.TranslateSimple() : key;
                return (args == null || args.Length == 0) ? template : string.Format(template, args);
            }
            catch { return key ?? string.Empty; }
        }

        private static void RegisterContextProviders()
        {
            RimMindAPI.Context.ContextKeys.Register(new ContextProviderDef(
                "personality_profile", ContextLayer.L3_State, 0.25f,
                async (ctx, ct) =>
                {
                    if (!PersonalityContextScenarioPolicy.IncludesPersonalityContext(ctx.Scenario)) return null;
                    var pawn = PawnResolver.TryFindPawn(ctx.PawnId);
                    if (pawn == null) return null;
                    var profile = AIPersonalityWorldComponent.Instance?.GetOrCreate(pawn);
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
                }, PublicProviderOwner, stalenessTicks: 750, invalidationTriggers: new[] { "PersonalityEvent" }));

            RimMindAPI.Context.ContextKeys.Register(new ContextProviderDef(
                "personality_state", ContextLayer.L3_State, 0.2f,
                async (ctx, ct) =>
                {
                    if (!PersonalityContextScenarioPolicy.IncludesPersonalityContext(ctx.Scenario)) return null;
                    var pawn = PawnResolver.TryFindPawn(ctx.PawnId);
                    if (pawn == null) return null;
                    RimWorld.Thought_Memory[] memories;
                    try
                    {
                        var raw = pawn.needs?.mood?.thoughts?.memories?.Memories;
                        if (raw == null || raw.Count == 0) return null;
                        memories = raw.ToArray();
                    }
                    catch
                    {
                        return null;
                    }

                    var sb = new System.Text.StringBuilder(Tr("RimMind.Personality.Context.StateHeader") + "\n");
                    bool any = false;
                    foreach (var t in memories)
                    {
                        if (t?.def == null || !Personality.PersonalityThoughtMapper.IsAIPersonalityDef(t.def.defName)) continue;

                        string desc = (t as Thought_AIPersonality)?.aiDescription ?? t.def.label;
                        float hours = t.DurationTicks / (float)RimMind.Domain.Common.RimMindTime.TicksPerHour;
                        sb.AppendLine(Tr("RimMind.Personality.Context.StateEntry", desc, $"{hours:F1}"));
                        any = true;
                    }
                    return any ? sb.ToString().TrimEnd() : null;
                }, PublicProviderOwner, stalenessTicks: 750, invalidationTriggers: new[] { "PersonalityEvent" }));

            RimMindAPI.Context.ContextKeys.Register(new ContextProviderDef(
                "personality_shaping", ContextLayer.L3_State, 0.15f,
                async (ctx, ct) =>
                {
                    if (!PersonalityContextScenarioPolicy.IncludesPersonalityContext(ctx.Scenario)) return null;
                    var pawn = PawnResolver.TryFindPawn(ctx.PawnId);
                    if (pawn == null) return null;
                    var profile = AIPersonalityWorldComponent.Instance?.GetOrCreate(pawn);
                    int maxCount = RimMindPersonalityMod.Settings?.shapingHistoryMaxCount ?? 20;
                    System.Collections.Generic.List<(string action, string label)> recent;
                    try
                    {
                        if (profile?.playerShapingHistory == null || profile.playerShapingHistory.Count == 0)
                            return null;
                        var snapshot = profile.playerShapingHistory.ToArray();
                        recent = snapshot
                            .Skip(System.Math.Max(0, snapshot.Length - maxCount))
                            .Where(r => r != null)
                            .Select(r => (r.action, r.label))
                            .ToList();
                    }
                    catch
                    {
                        return null;
                    }
                    if (recent.Count == 0) return null;

                    var sb = new System.Text.StringBuilder(Tr("RimMind.Personality.Context.ShapingHistoryHeader"));
                    foreach (var r in recent)
                    {
                        var shapingAction = ShapingActionExtensions.FromString(r.action);
                        string actionLabel = shapingAction switch
                        {
                            ShapingAction.Reinforce => Tr("RimMind.Personality.ShapingAction.Reinforce"),
                            ShapingAction.Suppress => Tr("RimMind.Personality.ShapingAction.Suppress"),
                            _ => Tr("RimMind.Personality.ShapingAction.Ignore")
                        };
                        sb.AppendLine($"- {r.label}: {actionLabel}");
                    }
                    return sb.ToString().TrimEnd();
                }, PublicProviderOwner, stalenessTicks: 750, invalidationTriggers: new[] { "PersonalityEvent" }));

            RimMindAPI.Context.ContextKeys.Register(new ContextProviderDef(
                "personality_task", ContextLayer.L0_Static, 0.95f,
                async (ctx, ct) =>
                {
                    if (ctx.Scenario != RimMindAPI.Context.ScenarioPersonality) return null;
                    string baseInstruction = RimMindAPI.Prompt.BuildTaskInstruction("RimMind.Personality.Prompt.TaskInstruction", null,
                        "Role", "Goal", "Process", "Constraint", "Example", "Output", "Fallback",
                        "JsonFormatDirect", "LabelHint", "DescHint",
                        "NarrativeHint", "DurationHint", "DiversityHint");
                    int maxSlots = PersonalityThoughtPolicy.OwnedSlotCount;
                    string evalInst = Tr("RimMind.Personality.Prompt.TaskInstruction.EvalInstruction", maxSlots);
                    return string.IsNullOrEmpty(evalInst) ? baseInstruction : $"{baseInstruction}\n\n{evalInst}";
                }, PublicProviderOwner, stalenessTicks: 0, invalidationTriggers: new[] { "PersonalityEvent" }));
        }
    }
}
