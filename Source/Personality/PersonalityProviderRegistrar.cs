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
        internal static void RegisterAll()
        {
            RegisterContextProviders();
            RegisterAgentIdentityProvider();
        }

        private static void RegisterAgentIdentityProvider()
        {
            RimMindAPI.RegisterAgentIdentityProvider(pawn =>
            {
                var profile = AIPersonalityWorldComponent.Instance?.GetOrCreate(pawn);
                return profile?.agentIdentity;
            });
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
                    sb.AppendLine("RimMind.Personality.Context.ProfileHeader".Translate(pawn.Name?.ToStringShort ?? ""));
                    if (!profile.description.NullOrEmpty())
                        sb.AppendLine(profile.description);
                    if (!profile.workTendencies.NullOrEmpty())
                        sb.AppendLine("RimMind.Personality.Context.WorkTendencies".Translate(profile.workTendencies));
                    if (!profile.socialTendencies.NullOrEmpty())
                        sb.AppendLine("RimMind.Personality.Context.SocialTendencies".Translate(profile.socialTendencies));
                    if (!profile.aiNarrative.NullOrEmpty())
                        sb.AppendLine("RimMind.Personality.Context.RecentState".Translate(profile.aiNarrative));
                    return sb.ToString().TrimEnd();
                }, "RimMind.Personality", stalenessTicks: 750, invalidationTriggers: new[] { "PersonalityEvent" }));

            RimMindAPI.Context.ContextKeys.Register(new ContextProviderDef(
                "personality_state", ContextLayer.L3_State, 0.2f,
                async (ctx, ct) =>
                {
                    if (!PersonalityContextScenarioPolicy.IncludesPersonalityContext(ctx.Scenario)) return null;
                    var pawn = PawnResolver.TryFindPawn(ctx.PawnId);
                    if (pawn == null) return null;
                    var memories = pawn.needs?.mood?.thoughts?.memories?.Memories;
                    if (memories == null) return null;

                    var sb = new System.Text.StringBuilder("RimMind.Personality.Context.StateHeader".Translate() + "\n");
                    bool any = false;
                    foreach (var t in memories)
                    {
                        if (!Personality.PersonalityThoughtMapper.IsAIPersonalityDef(t.def.defName)) continue;

                        string desc = (t as Thought_AIPersonality)?.aiDescription ?? t.def.label;
                        float hours = t.DurationTicks / 2500f;
                        sb.AppendLine("RimMind.Personality.Context.StateEntry".Translate(desc, $"{hours:F1}"));
                        any = true;
                    }
                    return any ? sb.ToString().TrimEnd() : null;
                }, "RimMind.Personality", stalenessTicks: 750, invalidationTriggers: new[] { "PersonalityEvent" }));

            RimMindAPI.Context.ContextKeys.Register(new ContextProviderDef(
                "personality_shaping", ContextLayer.L3_State, 0.15f,
                async (ctx, ct) =>
                {
                    if (!PersonalityContextScenarioPolicy.IncludesPersonalityContext(ctx.Scenario)) return null;
                    var pawn = PawnResolver.TryFindPawn(ctx.PawnId);
                    if (pawn == null) return null;
                    var profile = AIPersonalityWorldComponent.Instance?.GetOrCreate(pawn);
                    if (profile?.playerShapingHistory == null || profile.playerShapingHistory.Count == 0)
                        return null;

                    int maxCount = RimMindPersonalityMod.Settings?.shapingHistoryMaxCount ?? 20;
                    var recent = profile.playerShapingHistory.Skip(System.Math.Max(0, profile.playerShapingHistory.Count - maxCount)).ToList();
                    var sb = new System.Text.StringBuilder("RimMind.Personality.Context.ShapingHistoryHeader".Translate());
                    foreach (var r in recent)
                    {
                        var shapingAction = ShapingActionExtensions.FromString(r.action);
                        string actionLabel = shapingAction switch
                        {
                            ShapingAction.Reinforce => "RimMind.Personality.ShapingAction.Reinforce".Translate(),
                            ShapingAction.Suppress => "RimMind.Personality.ShapingAction.Suppress".Translate(),
                            _ => "RimMind.Personality.ShapingAction.Ignore".Translate()
                        };
                        sb.AppendLine($"- {r.label}: {actionLabel}");
                    }
                    return sb.ToString().TrimEnd();
                }, "RimMind.Personality", stalenessTicks: 750, invalidationTriggers: new[] { "PersonalityEvent" }));

            var personalityTaskInstruction = RimMindAPI.Prompt.BuildTaskInstruction("RimMind.Personality.Prompt.TaskInstruction", null,
                "Role", "Goal", "Process", "Constraint", "Example", "Output", "Fallback",
                "EvalInstruction", "JsonFormatDirect", "LabelHint", "DescHint",
                "NarrativeHint", "DurationHint", "DiversityHint", "TriggerReason");

            RimMindAPI.Context.ContextKeys.Register(new ContextProviderDef(
                "personality_task", ContextLayer.L0_Static, 0.95f,
                async (ctx, ct) =>
                {
                    if (ctx.Scenario != RimMindAPI.Context.ScenarioPersonality) return null;
                    return personalityTaskInstruction;
                }, "RimMind.Personality", stalenessTicks: 0, invalidationTriggers: new[] { "PersonalityEvent" }));
        }
    }
}
