using RimMind.Personality.Data;
using RimMind.Presentation.Api;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimMind.Personality.UI
{
    public class Dialog_PersonalityProfile : Window
    {
        private readonly Pawn _pawn;
        private PersonalityProfile? _profile;

        private string _editDesc = "";
        private string _editWork = "";
        private string _editSocial = "";
        private Vector2 _scrollNarrative;

        private static string Tr(string key, string fallback, params object[] args)
            => RimMindAPI.Text.TrOr(key, fallback, args);

        public Dialog_PersonalityProfile(Pawn pawn)
        {
            _pawn = pawn;
            doCloseX = true;
            draggable = true;
            closeOnAccept = false;
            closeOnCancel = true;
            absorbInputAroundWindow = false;
            preventCameraMotion = false;
        }

        public override Vector2 InitialSize => new Vector2(520f, 510f);

        public override void PreOpen()
        {
            base.PreOpen();
            _profile = AIPersonalityWorldComponent.Instance?.TryGet(_pawn);
            _editDesc = _profile?.description ?? "";
            _editWork = _profile?.workTendencies ?? "";
            _editSocial = _profile?.socialTendencies ?? "";
        }

        public override void DoWindowContents(Rect inRect)
        {
            float y = inRect.y;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, 30f), Tr("RimMind.Personality.UI.ProfileTitle", "{0}'s AI Personality", _pawn.LabelShort));
            y += 34f;
            Text.Font = GameFont.Small;

            Widgets.Label(new Rect(inRect.x, y, inRect.width, 20f), Tr("RimMind.Personality.UI.DescriptionLabel", "Personality description (background, referenced by AI evaluation)"));
            y += 22f;
            _editDesc = Widgets.TextArea(new Rect(inRect.x, y, inRect.width, 64f), _editDesc);
            y += 68f;

            Widgets.Label(new Rect(inRect.x, y, inRect.width, 20f), Tr("RimMind.Personality.UI.WorkTendencies", "Work tendencies"));
            y += 22f;
            _editWork = Widgets.TextArea(new Rect(inRect.x, y, inRect.width, 42f), _editWork);
            y += 46f;

            Widgets.Label(new Rect(inRect.x, y, inRect.width, 20f), Tr("RimMind.Personality.UI.SocialTendencies", "Social tendencies"));
            y += 22f;
            _editSocial = Widgets.TextArea(new Rect(inRect.x, y, inRect.width, 42f), _editSocial);
            y += 46f;

            Widgets.Label(new Rect(inRect.x, y, inRect.width, 20f), Tr("RimMind.Personality.UI.AINarrative", "AI recent narrative (read-only, updated daily by AI)"));
            y += 22f;
            string narrative = _profile == null || _profile.aiNarrative.NullOrEmpty()
                ? Tr("RimMind.Personality.UI.NarrativeEmpty", "(Not yet evaluated - AI will fill in automatically after running for a while)")
                : _profile.aiNarrative;
            float narrativeH = Mathf.Min(80f, Text.CalcHeight(narrative, inRect.width - 16f) + 4f);
            Rect narrativeOuterRect = new Rect(inRect.x, y, inRect.width, narrativeH);
            Widgets.BeginScrollView(narrativeOuterRect, ref _scrollNarrative,
                new Rect(0f, 0f, inRect.width - 16f, Text.CalcHeight(narrative, inRect.width - 16f)));
            GUI.color = new Color(0.8f, 0.8f, 0.8f);
            Widgets.Label(new Rect(0f, 0f, inRect.width - 16f, 9999f), narrative);
            GUI.color = Color.white;
            Widgets.EndScrollView();
            y += narrativeH + 8f;

            if (RimMindPersonalityMod.Settings != null)
            {
                string voteToggleLabel = Tr(
                    "RimMind.Personality.UI.EnableShapingVoteToggle",
                    "Enable personality shaping vote popups");
                string voteToggleTip = Tr(
                    "RimMind.Personality.UI.EnableShapingVoteToggle.Tip",
                    "When unchecked, new personality traits will not trigger shaping vote popups in the request overlay.");
                Rect toggleRect = new Rect(inRect.x, y, inRect.width, 24f);
                bool prevVote = RimMindPersonalityMod.Settings.enableShapingVote;
                Widgets.CheckboxLabeled(toggleRect, voteToggleLabel, ref RimMindPersonalityMod.Settings.enableShapingVote);
                TooltipHandler.TipRegion(toggleRect, voteToggleTip);
                if (RimMindPersonalityMod.Settings.enableShapingVote != prevVote)
                {
                    RimMindPersonalityMod.Settings.Write();
                }
            }

            float btnW = (inRect.width - 8f) / 2f;
            float btnY = inRect.yMax - 36f;

            if (Widgets.ButtonText(new Rect(inRect.x, btnY, btnW, 30f), Tr("RimMind.Personality.UI.Save", "Save")))
            {
                var targetProfile = _profile ?? AIPersonalityWorldComponent.Instance?.GetOrCreate(_pawn);
                if (targetProfile != null)
                {
                    targetProfile.description = _editDesc.Trim();
                    targetProfile.workTendencies = _editWork.Trim();
                    targetProfile.socialTendencies = _editSocial.Trim();
                    _profile = targetProfile;
                }
                Messages.Message(Tr("RimMind.Personality.UI.ProfileSaved", "[RimMind] {0}'s personality profile saved.", _pawn.LabelShort),
                    MessageTypeDefOf.SilentInput, historical: false);
                Close();
            }

            if (Widgets.ButtonText(new Rect(inRect.x + btnW + 8f, btnY, btnW, 30f), Tr("RimMind.Personality.UI.Cancel", "Cancel")))
                Close();
        }
    }
}
