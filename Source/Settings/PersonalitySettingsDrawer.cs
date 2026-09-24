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

            listing.Label("RimMind.Personality.Settings.ThoughtDuration".Translate());
            GUI.color = UnityEngine.Color.gray;
            listing.Label("  " + "RimMind.Personality.Settings.ThoughtDuration.Desc".Translate());
            GUI.color = UnityEngine.Color.white;
            bool aiDecides = RimMindPersonalityMod.Settings.durationMode == ThoughtDurationMode.AIDecides;
            listing.CheckboxLabeled("RimMind.Personality.Settings.AIDecidesDuration".Translate(), ref aiDecides,
                "RimMind.Personality.Settings.AIDecidesDuration.Desc".Translate());
            RimMindPersonalityMod.Settings.durationMode = aiDecides ? ThoughtDurationMode.AIDecides : ThoughtDurationMode.Fixed;

            if (!aiDecides)
            {
                listing.Label("RimMind.Personality.Settings.FixedDuration".Translate($"{RimMindPersonalityMod.Settings.thoughtDurationHours:F0}"));
                GUI.color = UnityEngine.Color.gray;
                listing.Label("  " + "RimMind.Personality.Settings.FixedDuration.Desc".Translate());
                GUI.color = UnityEngine.Color.white;
                RimMindPersonalityMod.Settings.thoughtDurationHours = listing.Slider(RimMindPersonalityMod.Settings.thoughtDurationHours, 1f, 24f);
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
            listing.Label("RimMind.Personality.Settings.RequestExpire".Translate($"{RimMindPersonalityMod.Settings.requestExpireTicks / 60000f:F2}"));
            GUI.color = UnityEngine.Color.gray;
            listing.Label("  " + "RimMind.Personality.Settings.RequestExpire.Desc".Translate());
            GUI.color = UnityEngine.Color.white;
            RimMindPersonalityMod.Settings.requestExpireTicks = (int)listing.Slider(RimMindPersonalityMod.Settings.requestExpireTicks, 3600f, 120000f);
            RimMindPersonalityMod.Settings.requestExpireTicks = (RimMindPersonalityMod.Settings.requestExpireTicks / 1500) * 1500;
            listing.Label("RimMind.Personality.Settings.ShapingHistoryMax".Translate($"{RimMindPersonalityMod.Settings.shapingHistoryMaxCount}"));
            GUI.color = UnityEngine.Color.gray;
            listing.Label("  " + "RimMind.Personality.Settings.ShapingHistoryMax.Desc".Translate());
            GUI.color = UnityEngine.Color.white;
            RimMindPersonalityMod.Settings.shapingHistoryMaxCount = (int)listing.Slider(RimMindPersonalityMod.Settings.shapingHistoryMaxCount, 10f, 200f);

            SettingsUIDrawer.DrawSectionHeader(listing, "RimMind.Personality.Settings.Section.Timing".Translate());

            listing.Label("RimMind.Personality.Settings.DailyInterval".Translate($"{RimMindPersonalityMod.Settings.dailyIntervalTicks / 2500f:F1}"));
            GUI.color = UnityEngine.Color.gray;
            listing.Label("  " + "RimMind.Personality.Settings.DailyInterval.Desc".Translate());
            GUI.color = UnityEngine.Color.white;
            RimMindPersonalityMod.Settings.dailyIntervalTicks = (int)listing.Slider(RimMindPersonalityMod.Settings.dailyIntervalTicks, 12000f, 120000f);
            RimMindPersonalityMod.Settings.dailyIntervalTicks = (RimMindPersonalityMod.Settings.dailyIntervalTicks / 3000) * 3000;

            listing.Label("RimMind.Personality.Settings.JitterRange".Translate($"{RimMindPersonalityMod.Settings.jitterRangeTicks / 2500f:F1}"));
            GUI.color = UnityEngine.Color.gray;
            listing.Label("  " + "RimMind.Personality.Settings.JitterRange.Desc".Translate());
            GUI.color = UnityEngine.Color.white;
            RimMindPersonalityMod.Settings.jitterRangeTicks = (int)listing.Slider(RimMindPersonalityMod.Settings.jitterRangeTicks, 0f, 12000f);
            RimMindPersonalityMod.Settings.jitterRangeTicks = (RimMindPersonalityMod.Settings.jitterRangeTicks / 600) * 600;

            listing.Label("RimMind.Personality.Settings.EventCooldown".Translate($"{RimMindPersonalityMod.Settings.eventCooldownTicks / 2500f:F1}"));
            GUI.color = UnityEngine.Color.gray;
            listing.Label("  " + "RimMind.Personality.Settings.EventCooldown.Desc".Translate());
            GUI.color = UnityEngine.Color.white;
            RimMindPersonalityMod.Settings.eventCooldownTicks = (int)listing.Slider(RimMindPersonalityMod.Settings.eventCooldownTicks, 600f, 6000f);
            RimMindPersonalityMod.Settings.eventCooldownTicks = (RimMindPersonalityMod.Settings.eventCooldownTicks / 300) * 300;

            listing.Label("RimMind.Personality.Settings.RequestTimeout".Translate($"{RimMindPersonalityMod.Settings.requestTimeoutTicks / 2500f:F1}"));
            GUI.color = UnityEngine.Color.gray;
            listing.Label("  " + "RimMind.Personality.Settings.RequestTimeout.Desc".Translate());
            GUI.color = UnityEngine.Color.white;
            RimMindPersonalityMod.Settings.requestTimeoutTicks = (int)listing.Slider(RimMindPersonalityMod.Settings.requestTimeoutTicks, 12000f, 120000f);
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
            float h = 30f;
            h += 24f;
            h += 24f + 24f * 5;
            h += 24f + 24f + 32f;
            h += 24f + 24f;
            if (RimMindPersonalityMod.Settings.durationMode != ThoughtDurationMode.AIDecides)
                h += 24f + 32f;
            else
                h += 24f;
            h += 24f + 24f * 2;
            h += 24f + 24f + 32f;
            h += 24f + (24f + 24f + 32f) * 4;
            return h + 40f;
        }
    }
}
