namespace RimMind.Personality.Comps
{
    public enum TriggerEventType { Injury, Skill, Incident, Death }

    /// <summary>
    /// Pure event and Pawn eligibility rules shared by Harmony adapters and the component.
    /// </summary>
    public static class PersonalityTriggerPolicy
    {
        public static bool IsTriggerEnabled(
            TriggerEventType eventType,
            AIPersonalitySettings settings)
        {
            return eventType switch
            {
                TriggerEventType.Injury => settings.enableInjuryTrigger,
                TriggerEventType.Skill => settings.enableSkillTrigger,
                TriggerEventType.Incident => settings.enableIncidentTrigger,
                TriggerEventType.Death => settings.enableDeathTrigger,
                _ => false,
            };
        }

        public static bool IsPawnEligible(
            bool isFreeNonSlaveColonist,
            bool isDead,
            bool hasMap,
            bool hasMood)
            => isFreeNonSlaveColonist && !isDead && hasMap && hasMood;

        public static bool ShouldTriggerInjury(
            bool triggerEnabled,
            bool hasHediff,
            bool isBad,
            float severity,
            bool isFreeNonSlaveColonist)
            => triggerEnabled
               && hasHediff
               && isBad
               && severity >= 0.2f
               && isFreeNonSlaveColonist;

        public static bool ShouldTriggerIncident(
            bool executionSucceeded,
            bool triggerEnabled,
            bool targetIsMap,
            string? categoryDefName)
            => executionSucceeded
               && triggerEnabled
               && targetIsMap
               && (categoryDefName == "ThreatBig" || categoryDefName == "ThreatSmall");

        public static bool ShouldTriggerSkill(
            bool triggerEnabled,
            bool preLevelWasCaptured,
            int previousLevel,
            int currentLevel)
            => triggerEnabled && preLevelWasCaptured && currentLevel > previousLevel;
    }
}
