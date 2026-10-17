using RimMind.Presentation.UI;
using UnityEngine;
using Verse;

namespace RimMind.Personality
{
    internal static class PersonalitySettingsDrawer
    {
        private static UnityEngine.Vector2 _scrollPos = UnityEngine.Vector2.zero;

        internal static void Draw(UnityEngine.Rect inRect)
        {
            UnityEngine.Rect contentArea = SettingsUIDrawer.SplitContentArea(inRect);
            UnityEngine.Rect bottomBar = SettingsUIDrawer.SplitBottomBar(inRect);

            float contentH = EstimateHeight();
            UnityEngine.Rect viewRect = new UnityEngine.Rect(0f, 0f, contentArea.width - 16f, contentH);
            Widgets.BeginScrollView(contentArea, ref _scrollPos, viewRect);

            var listing = new Verse.Listing_Standard();
            listing.Begin(viewRect);

            listing.CheckboxLabeled("RimMind.Personality.Settings.EnablePersonality".Translate(), ref RimMindPersonalityMod.Settings.enablePersonality,
                "RimMind.Personality.Settings.EnablePersonality.Desc".Translate());

            SettingsUIDrawer.DrawSectionHeader(listing, "RimMind.Personality.Settings.TriggerSources".Translate());
            listing.CheckboxLabeled("RimMind.Personality.Settings.DailyEval".Translate(), ref RimMindPersonalityMod.Settings.enableDailyEval,
                "RimMind.Personality.Settings.DailyEval.Desc".Translate());
            listing.CheckboxLabeled("RimMind.Personality.Settings.InjuryTrigger".Translate(), ref RimMindPersonalityMod.Settings.enableInjuryTrigger,
                "RimMind.Personality.Settings.InjuryTrigger.Desc".Translate());
            listing.CheckboxLabeled("RimMind.Personality.Settings.SkillTrigger".Translate(), ref RimMindPersonalityMod.Settings.enableSkillTrigger,
                "RimMind.Personality.Settings.SkillTrigger.Desc".Translate());
            listing.CheckboxLabeled("RimMind.Personality.Settings.IncidentTrigger".Translate(), ref RimMindPersonalityMod.Settings.enableIncidentTrigger,
                "RimMind.Personality.Settings.IncidentTrigger.Desc".Translate());
            listing.CheckboxLabeled("RimMind.Personality.Settings.DeathTrigger".Translate(), ref RimMindPersonalityMod.Settings.enableDeathTrigger,
                "RimMind.Personality.Settings.DeathTrigger.Desc".Translate());

            SettingsUIDrawer.DrawSectionHeader(listing, "RimMind.Personality.Settings.Section.Thought".Translate());

            listing.LabelWithTooltip("RimMind.Personality.Settings.ThoughtDuration".Translate(),
                "RimMind.Personality.Settings.ThoughtDuration.Desc".Translate());
            bool aiDecides = RimMindPersonalityMod.Settings.durationMode == ThoughtDurationMode.AIDecides;
            listing.CheckboxLabeled("RimMind.Personality.Settings.AIDecidesDuration".Translate(), ref aiDecides,
                "RimMind.Personality.Settings.AIDecidesDuration.Desc".Translate());
            RimMindPersonalityMod.Settings.durationMode = aiDecides ? ThoughtDurationMode.AIDecides : ThoughtDurationMode.Fixed;

            if (!aiDecides)
            {
                string fixedTip = "RimMind.Personality.Settings.FixedDuration.Desc".Translate();
                listing.LabelWithTooltip("RimMind.Personality.Settings.FixedDuration".Translate($"{RimMindPersonalityMod.Settings.thoughtDurationHours:F0}"), fixedTip);
                RimMindPersonalityMod.Settings.thoughtDurationHours = listing.SliderWithTooltip(RimMindPersonalityMod.Settings.thoughtDurationHours, 1f, 24f, fixedTip);
            }
            else
            {
                listing.Label("RimMind.Personality.Settings.AIDurationHint".Translate());
            }

            SettingsUIDrawer.DrawSectionHeader(listing, "RimMind.Personality.Settings.Section.Display".Translate());
            listing.CheckboxLabeled("RimMind.Personality.Settings.ShowNotifications".Translate(), ref RimMindPersonalityMod.Settings.showNotifications,
                "RimMind.Personality.Settings.ShowNotifications.Desc".Translate());
            listing.CheckboxLabeled("RimMind.Personality.Settings.ShowLabelPrefix".Translate(), ref RimMindPersonalityMod.Settings.showLabelPrefix,
                "RimMind.Personality.Settings.ShowLabelPrefix.Desc".Translate());

            SettingsUIDrawer.DrawSectionHeader(listing, "RimMind.Personality.Settings.Section.Request".Translate());
            listing.CheckboxLabeled("RimMind.Personality.Settings.EnableShapingVote".Translate(), ref RimMindPersonalityMod.Settings.enableShapingVote,
                "RimMind.Personality.Settings.EnableShapingVote.Desc".Translate());

            string reqExpireTip = "RimMind.Personality.Settings.RequestExpire.Desc".Translate();
            listing.LabelWithTooltip("RimMind.Personality.Settings.RequestExpire".Translate($"{RimMindPersonalityMod.Settings.requestExpireTicks / 60000f:F2}"), reqExpireTip);
            RimMindPersonalityMod.Settings.requestExpireTicks = (int)listing.SliderWithTooltip(RimMindPersonalityMod.Settings.requestExpireTicks, 3600f, 120000f, reqExpireTip);
            RimMindPersonalityMod.Settings.requestExpireTicks = (RimMindPersonalityMod.Settings.requestExpireTicks / 1500) * 1500;

            string shapingHistoryTip = "RimMind.Personality.Settings.ShapingHistoryMax.Desc".Translate();
            listing.LabelWithTooltip("RimMind.Personality.Settings.ShapingHistoryMax".Translate($"{RimMindPersonalityMod.Settings.shapingHistoryMaxCount}"), shapingHistoryTip);
            RimMindPersonalityMod.Settings.shapingHistoryMaxCount = (int)listing.SliderWithTooltip(RimMindPersonalityMod.Settings.shapingHistoryMaxCount, 10f, 200f, shapingHistoryTip);

            SettingsUIDrawer.DrawSectionHeader(listing, "RimMind.Personality.Settings.Section.Timing".Translate());

            string dailyIntervalTip = "RimMind.Personality.Settings.DailyInterval.Desc".Translate();
            listing.LabelWithTooltip("RimMind.Personality.Settings.DailyInterval".Translate($"{RimMindPersonalityMod.Settings.dailyIntervalTicks / 2500f:F1}"), dailyIntervalTip);
            RimMindPersonalityMod.Settings.dailyIntervalTicks = (int)listing.SliderWithTooltip(RimMindPersonalityMod.Settings.dailyIntervalTicks, 12000f, 120000f, dailyIntervalTip);
            RimMindPersonalityMod.Settings.dailyIntervalTicks = (RimMindPersonalityMod.Settings.dailyIntervalTicks / 3000) * 3000;

            string jitterTip = "RimMind.Personality.Settings.JitterRange.Desc".Translate();
            listing.LabelWithTooltip("RimMind.Personality.Settings.JitterRange".Translate($"{RimMindPersonalityMod.Settings.jitterRangeTicks / 2500f:F1}"), jitterTip);
            RimMindPersonalityMod.Settings.jitterRangeTicks = (int)listing.SliderWithTooltip(RimMindPersonalityMod.Settings.jitterRangeTicks, 0f, 12000f, jitterTip);
            RimMindPersonalityMod.Settings.jitterRangeTicks = (RimMindPersonalityMod.Settings.jitterRangeTicks / 600) * 600;

            string eventCdTip = "RimMind.Personality.Settings.EventCooldown.Desc".Translate();
            listing.LabelWithTooltip("RimMind.Personality.Settings.EventCooldown".Translate($"{RimMindPersonalityMod.Settings.eventCooldownTicks / 2500f:F1}"), eventCdTip);
            RimMindPersonalityMod.Settings.eventCooldownTicks = (int)listing.SliderWithTooltip(RimMindPersonalityMod.Settings.eventCooldownTicks, 600f, 6000f, eventCdTip);
            RimMindPersonalityMod.Settings.eventCooldownTicks = (RimMindPersonalityMod.Settings.eventCooldownTicks / 300) * 300;

            string timeoutTip = "RimMind.Personality.Settings.RequestTimeout.Desc".Translate();
            listing.LabelWithTooltip("RimMind.Personality.Settings.RequestTimeout".Translate($"{RimMindPersonalityMod.Settings.requestTimeoutTicks / 2500f:F1}"), timeoutTip);
            RimMindPersonalityMod.Settings.requestTimeoutTicks = (int)listing.SliderWithTooltip(RimMindPersonalityMod.Settings.requestTimeoutTicks, 12000f, 120000f, timeoutTip);
            RimMindPersonalityMod.Settings.requestTimeoutTicks = (RimMindPersonalityMod.Settings.requestTimeoutTicks / 3000) * 3000;

            listing.End();
            Widgets.EndScrollView();

            SettingsUIDrawer.DrawBottomBar(bottomBar, () =>
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
                RimMindPersonalityMod.Settings.sunriseTriggerChance = 0.25f;
                RimMindPersonalityMod.Settings.enableRecreationTrigger = true;
                RimMindPersonalityMod.Settings.recreationTriggerChance = 0.25f;
                RimMindPersonalityMod.Settings.enableSkygazeTrigger = true;
                RimMindPersonalityMod.Settings.skygazeTriggerChance = 0.40f;
                RimMindPersonalityMod.Settings.enableMoodSwingTrigger = true;
                RimMindPersonalityMod.Settings.moodSwingTriggerChance = 0.35f;
                RimMindPersonalityMod.Settings.thoughtDurationHours = 24f;
                RimMindPersonalityMod.Settings.durationMode = ThoughtDurationMode.AIDecides;
                RimMindPersonalityMod.Settings.requestExpireTicks = 30000;
                RimMindPersonalityMod.Settings.enableShapingVote = true;
                RimMindPersonalityMod.Settings.shapingHistoryMaxCount = 20;
                RimMindPersonalityMod.Settings.dailyIntervalTicks = 60000;
                RimMindPersonalityMod.Settings.jitterRangeTicks = 3000;
                RimMindPersonalityMod.Settings.eventCooldownTicks = 1200;
                RimMindPersonalityMod.Settings.requestTimeoutTicks = 60000;
            });

            RimMindPersonalityMod.Settings.Write();
        }

        private static float EstimateHeight()
        {
            float h = 24f;
            h += 24f;
            h += 24f + 24f * 5;
            h += 24f + 24f + 24f;
            if (RimMindPersonalityMod.Settings.durationMode != ThoughtDurationMode.AIDecides)
                h += 46f;
            else
                h += 24f;
            h += 24f + 24f * 2;
            h += 24f + 24f + 46f * 2;
            h += 24f + 46f * 4;
            return h + 16f;
        }
    }
}
