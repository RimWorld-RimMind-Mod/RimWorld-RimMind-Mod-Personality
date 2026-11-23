using RimMind.Application.Common.Constants;
using RimMind.Presentation.Api;
using RimMind.Presentation.Settings;
using UnityEngine;

namespace RimMind.Personality
{
    internal sealed class PersonalitySettingsTab : ISettingsTab
    {
        public string Id => "personality";
        public string OwnerModId => RimMindOwnerConsts.PersonalityModId;
        public string Label => RimMindAPI.Text.TrOr("RimMind.Personality.Settings.TabLabel", "Personality");
        public void Draw(Rect rect) => PersonalitySettingsDrawer.Draw(rect);
    }
}
