using RimMind.Application.Common.Helpers;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Presentation.Api;

namespace RimMind.Personality
{
    internal sealed class PersonalityModCooldown : IModCooldown
    {
        public string Id => "Personality";
        // Must match the envelope ModId: Core resolves queue cooldowns by that key.
        public string OwnerModId => PersonalityRequestDefaults.ModId;
        public int CooldownTicks =>
            ActivityCooldownScaler.Scale(1200, RimMindAPI.Settings.ActivityFrequencyScale);
    }
}
