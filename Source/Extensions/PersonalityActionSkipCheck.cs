using RimMind.Application.Common.Constants;
using RimMind.Application.Common.Interfaces.Extension;

namespace RimMind.Personality
{
    internal sealed class PersonalityActionSkipCheck : ISkipCheck
    {
        public string Id => "personality.action";
        public string OwnerModId => RimMindOwnerConsts.PersonalityModId;
        public SkipCheckKind Kind => SkipCheckKind.Action;
        // Personality never vetoes Advisor/Actions when personality evaluation is disabled;
        // personality enablement only gates Personality's own triggers, evaluation, and context injection.
        public bool ShouldSkip(in SkipCheckArgs args) => false;
    }
}
