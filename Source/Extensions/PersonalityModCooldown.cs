using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Presentation.Api;
using UnityEngine;

namespace RimMind.Personality
{
    internal sealed class PersonalityModCooldown : IModCooldown
    {
        public string Id => "Personality";
        public string OwnerModId => "RimMind.Personality";
        public int CooldownTicks => 1200;
        public int CooldownTicks
        {
            get
            {
                float scale = RimMindAPI.Settings.ActivityFrequencyScale;
                float multiplier = scale > 0.01f ? (1.0f / scale) : 1.0f;
                multiplier = Mathf.Clamp(multiplier, 0.35f, 3.5f);
                return Mathf.RoundToInt(1200 * multiplier);
            }
        }
    }
}
