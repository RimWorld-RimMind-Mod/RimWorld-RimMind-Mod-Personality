using RimMind.Presentation.Api;
using RimMind.Presentation.UI;
using UnityEngine;
using Verse;

namespace RimMind.Personality
{
    internal static class PersonalitySettingsDrawer
    {
        private static Vector2 _scrollPos = Vector2.zero;
        private static float _measuredHeight = 760f;

        private static string Tr(string key, string fallback, params object[] args)
            => RimMindAPI.Text.TrOr(key, fallback, args);

        internal static void Draw(Rect inRect)
        {
            SettingsUIDrawer.DrawScrollableSettingsPage(
                inRect,
                ref _scrollPos,
                ref _measuredHeight,
                DrawContent,
                ResetDefaults,
                () => RimMindPersonalityMod.Settings.Write());
        }

        private static void DrawContent(Listing_Standard listing)
        {
            var s = RimMindPersonalityMod.Settings;

            listing.CheckboxLabeled(
                Tr("RimMind.Personality.Settings.EnablePersonality", "Enable AI Personality System"),
                ref s.enablePersonality,
                Tr("RimMind.Personality.Settings.EnablePersonality.Desc", "When enabled, AI dynamically generates psychological states (Thoughts) based on game events and colonist conditions."));

            SettingsUIDrawer.DrawSectionHeader(listing, Tr("RimMind.Personality.Settings.TriggerSources", "Trigger Sources"));
            listing.CheckboxLabeled(
                Tr("RimMind.Personality.Settings.DailyEval", "Daily Evaluation"),
                ref s.enableDailyEval,
                Tr("RimMind.Personality.Settings.DailyEval.Desc", "Automatically evaluate colonist personality each day, generating psychological states."));
            listing.CheckboxLabeled(
                Tr("RimMind.Personality.Settings.InjuryTrigger", "Injury / Illness"),
                ref s.enableInjuryTrigger,
                Tr("RimMind.Personality.Settings.InjuryTrigger.Desc", "Trigger personality evaluation when a colonist is injured."));
            listing.CheckboxLabeled(
                Tr("RimMind.Personality.Settings.SkillTrigger", "Skill Level Up"),
                ref s.enableSkillTrigger,
                Tr("RimMind.Personality.Settings.SkillTrigger.Desc", "Trigger personality evaluation when a colonist levels up a skill."));
            listing.CheckboxLabeled(
                Tr("RimMind.Personality.Settings.IncidentTrigger", "Major Incident"),
                ref s.enableIncidentTrigger,
                Tr("RimMind.Personality.Settings.IncidentTrigger.Desc", "Trigger personality evaluation when a major colony incident occurs."));
            listing.CheckboxLabeled(
                Tr("RimMind.Personality.Settings.DeathTrigger", "Close One Died"),
                ref s.enableDeathTrigger,
                Tr("RimMind.Personality.Settings.DeathTrigger.Desc", "Trigger personality evaluation for other colonists when someone dies."));

            SettingsUIDrawer.DrawSectionHeader(listing, Tr("RimMind.Personality.Settings.Section.Thought", "Thought Settings"));

            listing.LabelWithTooltip(
                Tr("RimMind.Personality.Settings.ThoughtDuration", "Thought duration:"),
                Tr("RimMind.Personality.Settings.ThoughtDuration.Desc", "Set the duration mode for AI-generated psychological states (Thoughts)."));
            bool aiDecides = s.durationMode == ThoughtDurationMode.AIDecides;
            listing.CheckboxLabeled(
                Tr("RimMind.Personality.Settings.AIDecidesDuration", "Let AI decide each Thought's duration"),
                ref aiDecides,
                Tr("RimMind.Personality.Settings.AIDecidesDuration.Desc", "When enabled, AI outputs duration_hours (1~24) in JSON, automatically setting duration based on emotional intensity."));
            s.durationMode = aiDecides ? ThoughtDurationMode.AIDecides : ThoughtDurationMode.Fixed;

            if (!aiDecides)
            {
                string fixedTip = Tr("RimMind.Personality.Settings.FixedDuration.Desc", "All AI-generated Thoughts use this fixed duration uniformly.");
                listing.LabelWithTooltip(
                    Tr("RimMind.Personality.Settings.FixedDuration", "  Fixed duration: {0} game hours", $"{s.thoughtDurationHours:F0}"),
                    fixedTip);
                s.thoughtDurationHours = listing.SliderWithTooltip(s.thoughtDurationHours, 1f, 24f, fixedTip);
            }

            SettingsUIDrawer.DrawSectionHeader(listing, Tr("RimMind.Personality.Settings.Section.Display", "Display"));
            listing.CheckboxLabeled(
                Tr("RimMind.Personality.Settings.ShowNotifications", "Show Personality Change Notifications"),
                ref s.showNotifications,
                Tr("RimMind.Personality.Settings.ShowNotifications.Desc", "Display a notification on screen when AI generates a new psychological state."));
            listing.CheckboxLabeled(
                Tr("RimMind.Personality.Settings.ShowLabelPrefix", "Show [RimMind] Prefix"),
                ref s.showLabelPrefix,
                Tr("RimMind.Personality.Settings.ShowLabelPrefix.Desc", "Display a RimMind prefix before psychological state labels."));

            SettingsUIDrawer.DrawSectionHeader(listing, Tr("RimMind.Personality.Settings.Section.Request", "Request Settings"));
            listing.CheckboxLabeled(
                Tr("RimMind.Personality.Settings.EnableShapingVote", "Enable Personality Shaping Vote"),
                ref s.enableShapingVote,
                Tr("RimMind.Personality.Settings.EnableShapingVote.Desc", "When enabled, AI proposes new personality traits during evaluation, and the player can vote."));

            string reqExpireTip = Tr("RimMind.Personality.Settings.RequestExpire.Desc", "Shaping vote requests not responded to within this time are cancelled.");
            listing.LabelWithTooltip(
                Tr("RimMind.Personality.Settings.RequestExpire", "Shaping vote expiry: {0} game days", $"{s.requestExpireTicks / 60000f:F2}"),
                reqExpireTip);
            s.requestExpireTicks = (int)listing.SliderWithTooltip(s.requestExpireTicks, 3600f, 120000f, reqExpireTip);
            s.requestExpireTicks = (s.requestExpireTicks / 1500) * 1500;

            string shapingHistoryTip = Tr("RimMind.Personality.Settings.ShapingHistoryMax.Desc", "Keep the last N personality shaping vote records for AI evaluation reference.");
            listing.LabelWithTooltip(
                Tr("RimMind.Personality.Settings.ShapingHistoryMax", "Shaping history limit: {0}", $"{s.shapingHistoryMaxCount}"),
                shapingHistoryTip);
            s.shapingHistoryMaxCount = (int)listing.SliderWithTooltip(s.shapingHistoryMaxCount, 10f, 200f, shapingHistoryTip);

            SettingsUIDrawer.DrawSectionHeader(listing, Tr("RimMind.Personality.Settings.Section.Timing", "Timing"));

            string dailyIntervalTip = Tr("RimMind.Personality.Settings.DailyInterval.Desc", "Time between automatic daily personality evaluations.");
            listing.LabelWithTooltip(
                Tr("RimMind.Personality.Settings.DailyInterval", "Daily evaluation interval: {0} game hours", $"{s.dailyIntervalTicks / 2500f:F1}"),
                dailyIntervalTip);
            s.dailyIntervalTicks = (int)listing.SliderWithTooltip(s.dailyIntervalTicks, 12000f, 120000f, dailyIntervalTip);
            s.dailyIntervalTicks = (s.dailyIntervalTicks / 3000) * 3000;

            string jitterTip = Tr("RimMind.Personality.Settings.JitterRange.Desc", "Variation added to daily interval to stagger colonist evaluations.");
            listing.LabelWithTooltip(
                Tr("RimMind.Personality.Settings.JitterRange", "Jitter range: {0} game hours", $"{s.jitterRangeTicks / 2500f:F1}"),
                jitterTip);
            s.jitterRangeTicks = (int)listing.SliderWithTooltip(s.jitterRangeTicks, 0f, 12000f, jitterTip);
            s.jitterRangeTicks = (s.jitterRangeTicks / 600) * 600;

            string eventCdTip = Tr("RimMind.Personality.Settings.EventCooldown.Desc", "Minimum time between event-triggered evaluations.");
            listing.LabelWithTooltip(
                Tr("RimMind.Personality.Settings.EventCooldown", "Event cooldown: {0} game hours", $"{s.eventCooldownTicks / 2500f:F1}"),
                eventCdTip);
            s.eventCooldownTicks = (int)listing.SliderWithTooltip(s.eventCooldownTicks, 600f, 6000f, eventCdTip);
            s.eventCooldownTicks = (s.eventCooldownTicks / 300) * 300;

            string timeoutTip = Tr("RimMind.Personality.Settings.RequestTimeout.Desc", "Cancel pending AI requests that exceed this duration.");
            listing.LabelWithTooltip(
                Tr("RimMind.Personality.Settings.RequestTimeout", "Request timeout: {0} game hours", $"{s.requestTimeoutTicks / 2500f:F1}"),
                timeoutTip);
            s.requestTimeoutTicks = (int)listing.SliderWithTooltip(s.requestTimeoutTicks, 12000f, 120000f, timeoutTip);
            s.requestTimeoutTicks = (s.requestTimeoutTicks / 3000) * 3000;
        }

        private static void ResetDefaults()
        {
            RimMindPersonalityMod.Settings.enablePersonality = true;
            RimMindPersonalityMod.Settings.showNotifications = true;
            RimMindPersonalityMod.Settings.showLabelPrefix = true;
            RimMindPersonalityMod.Settings.enableDailyEval = true;
            RimMindPersonalityMod.Settings.enableInjuryTrigger = true;
            RimMindPersonalityMod.Settings.enableSkillTrigger = true;
            RimMindPersonalityMod.Settings.enableIncidentTrigger = true;
            RimMindPersonalityMod.Settings.enableDeathTrigger = true;
            RimMindPersonalityMod.Settings.enableSunriseTrigger = true;
            RimMindPersonalityMod.Settings.sunriseTriggerChance = 0.30f;
            RimMindPersonalityMod.Settings.enableRecreationTrigger = true;
            RimMindPersonalityMod.Settings.recreationTriggerChance = 0.25f;
            RimMindPersonalityMod.Settings.enableSkygazeTrigger = true;
            RimMindPersonalityMod.Settings.skygazeTriggerChance = 0.40f;
            RimMindPersonalityMod.Settings.enableMoodSwingTrigger = true;
            RimMindPersonalityMod.Settings.moodSwingTriggerChance = 0.50f;
            RimMindPersonalityMod.Settings.thoughtDurationHours = 24f;
            RimMindPersonalityMod.Settings.durationMode = ThoughtDurationMode.AIDecides;
            RimMindPersonalityMod.Settings.requestExpireTicks = 30000;
            RimMindPersonalityMod.Settings.enableShapingVote = true;
            RimMindPersonalityMod.Settings.shapingHistoryMaxCount = 20;
            RimMindPersonalityMod.Settings.dailyIntervalTicks = 60000;
            RimMindPersonalityMod.Settings.jitterRangeTicks = 3000;
            RimMindPersonalityMod.Settings.eventCooldownTicks = 1200;
            RimMindPersonalityMod.Settings.requestTimeoutTicks = 60000;
        }
    }
}
