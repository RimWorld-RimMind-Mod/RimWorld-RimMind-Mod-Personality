using RimMind.Application.Common.Constants;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Presentation.Api;

namespace RimMind.Personality
{
    internal sealed class PersonalityModCooldown : IModCooldown
    {
        // Id matches envelope ModId so Core's QueueCircuitBreaker resolves cooldowns cleanly,
        // while OwnerModId matches RimMindOwnerConsts for consistent UnregisterByOwner tracking.
        public string Id => PersonalityRequestDefaults.ModId;
        public string OwnerModId => RimMindOwnerConsts.PersonalityModId;
        public int CooldownTicks => RimMindAPI.Settings.ScaleCooldown(1200);
    }
}
