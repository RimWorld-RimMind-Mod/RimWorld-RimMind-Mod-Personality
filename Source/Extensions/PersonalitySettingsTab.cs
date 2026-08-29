using UnityEngine;
using RimMind.Presentation.Settings;
using Verse;

namespace RimMind.Personality
{
    internal sealed class PersonalitySettingsTab : ISettingsTab
    {
        public string Id => "personality";
        public string OwnerModId => "RimMindPersonality";
        public string Label => "RimMind.Personality.Settings.TabLabel".Translate();
        public void Draw(Rect rect) => PersonalitySettingsDrawer.Draw(rect);
    }
}
