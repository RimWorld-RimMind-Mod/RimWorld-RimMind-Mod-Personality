using Verse;

namespace RimMind.Personality
{
    public enum ThoughtDurationMode { Fixed, AIDecides }

    public class AIPersonalitySettings : ModSettings
    {
        public bool enablePersonality = true;
        public bool showNotifications = true;

        // Trigger sources
        public bool enableDailyEval = true;
        public bool enableInjuryTrigger = true;
        public bool enableSkillTrigger = true;
        public bool enableIncidentTrigger = true;
        public bool enableDeathTrigger = true;

        // State-transition triggers & probabilities
        public bool enableSunriseTrigger = true;
        public float sunriseTriggerChance = 0.30f;

        public bool enableRecreationTrigger = true;
        public float recreationTriggerChance = 0.25f;

        public bool enableSkygazeTrigger = true;
        public float skygazeTriggerChance = 0.40f;

        public bool enableMoodSwingTrigger = true;
        public float moodSwingTriggerChance = 0.50f;

        public float thoughtDurationHours = 24f;

        /// <summary>Fixed = Use thoughtDurationHours; AIDecides = Determined by AI in response.</summary>
        public ThoughtDurationMode durationMode = ThoughtDurationMode.AIDecides;

        /// <summary>Show [RimMind] prefix in thought label.</summary>
        public bool showLabelPrefix = true;

        public bool enableShapingVote = true;

        public int requestExpireTicks = 30000;

        public int shapingHistoryMaxCount = 20;

        public int dailyIntervalTicks = 60000;
        public int jitterRangeTicks = 3000;
        public int eventCooldownTicks = 1200;
        public int requestTimeoutTicks = 60000;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enablePersonality, "enablePersonality", true);
            Scribe_Values.Look(ref showNotifications, "showNotifications", true);
            Scribe_Values.Look(ref enableDailyEval, "enableDailyEval", true);
            Scribe_Values.Look(ref enableInjuryTrigger, "enableInjuryTrigger", true);
            Scribe_Values.Look(ref enableSkillTrigger, "enableSkillTrigger", true);
            Scribe_Values.Look(ref enableIncidentTrigger, "enableIncidentTrigger", true);
            Scribe_Values.Look(ref enableDeathTrigger, "enableDeathTrigger", true);

            Scribe_Values.Look(ref enableSunriseTrigger, "enableSunriseTrigger", true);
            Scribe_Values.Look(ref sunriseTriggerChance, "sunriseTriggerChance", 0.30f);
            Scribe_Values.Look(ref enableRecreationTrigger, "enableRecreationTrigger", true);
            Scribe_Values.Look(ref recreationTriggerChance, "recreationTriggerChance", 0.25f);
            Scribe_Values.Look(ref enableSkygazeTrigger, "enableSkygazeTrigger", true);
            Scribe_Values.Look(ref skygazeTriggerChance, "skygazeTriggerChance", 0.40f);
            Scribe_Values.Look(ref enableMoodSwingTrigger, "enableMoodSwingTrigger", true);
            Scribe_Values.Look(ref moodSwingTriggerChance, "moodSwingTriggerChance", 0.50f);

            Scribe_Values.Look(ref thoughtDurationHours, "thoughtDurationHours", 24f);
            Scribe_Values.Look(ref durationMode, "durationMode", ThoughtDurationMode.AIDecides);
            Scribe_Values.Look(ref showLabelPrefix, "showLabelPrefix", true);
            Scribe_Values.Look(ref requestExpireTicks, "requestExpireTicks", 30000);
            Scribe_Values.Look(ref enableShapingVote, "enableShapingVote", true);
            Scribe_Values.Look(ref shapingHistoryMaxCount, "shapingHistoryMaxCount", 20);
            Scribe_Values.Look(ref dailyIntervalTicks, "dailyIntervalTicks", 60000);
            Scribe_Values.Look(ref jitterRangeTicks, "jitterRangeTicks", 3000);
            Scribe_Values.Look(ref eventCooldownTicks, "eventCooldownTicks", 1200);
            Scribe_Values.Look(ref requestTimeoutTicks, "requestTimeoutTicks", 60000);
        }
    }
}
