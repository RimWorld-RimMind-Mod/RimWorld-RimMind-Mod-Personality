namespace RimMind.Personality.Comps
{
    public enum TriggerEventType { Injury, Skill, Incident, Death, Sunrise, Recreation, Skygaze, MoodSwing }

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
                TriggerEventType.Sunrise => settings.enableSunriseTrigger,
                TriggerEventType.Recreation => settings.enableRecreationTrigger,
                TriggerEventType.Skygaze => settings.enableSkygazeTrigger,
                TriggerEventType.MoodSwing => settings.enableMoodSwingTrigger,
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

        public static bool ShouldTriggerSunrise(
            bool triggerEnabled,
            float triggerChance,
            float roll,
            bool isAwake)
            => triggerEnabled && isAwake && roll < triggerChance;

        public static bool ShouldTriggerAwakening(
            bool triggerEnabled,
            float triggerChance,
            float roll,
            bool justWokeUp)
            => triggerEnabled && justWokeUp && roll < triggerChance;

        public static bool ShouldTriggerDusk(
            bool triggerEnabled,
            float triggerChance,
            float roll,
            bool isAwake)
            => triggerEnabled && isAwake && roll < triggerChance;

        public static bool ShouldTriggerContemplationPulse(
            bool triggerEnabled,
            float triggerChance,
            float roll,
            bool isAwake,
            bool isIdleOrWalking)
            => triggerEnabled && isAwake && isIdleOrWalking && roll < triggerChance;

        public static float CalculateDynamicPersonalityChance(float baseChance, float moodLevel, float jitter = 1.0f)
        public static float CalculateDynamicPersonalityChance(float baseChance, float moodLevel, float jitter = 1.0f, float activityScale = 1.0f)
        {
            float chance = baseChance;
            float chance = baseChance * activityScale;
            if (moodLevel <= 0.30f) chance *= 1.4f;
            else if (moodLevel >= 0.85f) chance *= 1.2f;
            chance *= jitter;
            return (float)System.Math.Clamp(chance, 0.05f, 0.95f);
            return (float)System.Math.Clamp(chance, 0.01f, 0.98f);
        }

        public static bool ShouldTriggerRecreation(
            bool triggerEnabled,
            float triggerChance,
            float roll)
            => triggerEnabled && roll < triggerChance;

        public static bool ShouldTriggerSkygaze(
            bool triggerEnabled,
            float triggerChance,
            float roll)
            => triggerEnabled && roll < triggerChance;

        public static bool ShouldTriggerMoodSwing(
            bool triggerEnabled,
            float triggerChance,
            float roll,
            float moodDelta,
            float threshold = 0.15f)
            => triggerEnabled && System.Math.Abs(moodDelta) >= threshold && roll < triggerChance;
    }
}
