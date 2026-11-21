using RimMind.Application.Common.Constants;
using RimMind.Personality.Comps;
using RimMind.Personality.Data;
using RimMind.Presentation.Api;
using RimMind.Presentation.UI;
using Verse;

namespace RimMind.Personality.UI
{
    /// <summary>
    /// Bio-tab badge provider for RimMind-Personality registered via Core's unified
    /// <c>CharacterCardUtility.DoTopStack</c> host (<see cref="RimMindAPI.UI.RegisterBioTabBadge"/>).
    /// Uses strictly read-only <see cref="AIPersonalityWorldComponent.TryGet(Pawn?)"/> during hover/draw.
    /// </summary>
    public sealed class PersonalityBioTabBadgeProvider : IBioTabBadgeProvider
    {
        public const string BadgeId = "rimmind.personality.bio_tab";

        public string Id => BadgeId;
        public string OwnerModId => RimMindOwnerConsts.PersonalityModId;
        public int Order => 10;

        public BioTabBadgeDescriptor? TryGetBadge(Pawn pawn)
        {
            if (pawn == null || pawn.GetComp<CompAIPersonality>() == null)
                return null;

            string label = RimMindAPI.Text.TrOr("RimMind.Personality.UI.BioTab.Label", "Personality");
            return new BioTabBadgeDescriptor(
                Label: label,
                TooltipProvider: () => BuildTooltip(pawn),
                OnClick: () => Find.WindowStack.Add(new Dialog_PersonalityProfile(pawn)),
                ExtraWidth: 16f);
        }

        private static string BuildTooltip(Pawn pawn)
        {
            var profile = AIPersonalityWorldComponent.Instance?.TryGet(pawn);
            if (profile != null && !profile.aiNarrative.NullOrEmpty())
            {
                return RimMindAPI.Text.TrOr(
                    "RimMind.Personality.UI.BioTab.NarrativeTip",
                    "AI recent narrative: {0}",
                    profile.aiNarrative);
            }

            return RimMindAPI.Text.TrOr(
                "RimMind.Personality.UI.BioTab.NotEvaluatedTip",
                "Not yet evaluated - AI will fill in personality narrative automatically after running for a while.");
        }
    }
}
