using System;
using Xunit;

// 测试从 Patch 过滤逻辑中提取的纯逻辑
// Injury: isBad != false + Severity >= 0.2f + IsFreeNonSlaveColonist
// Incident: category.defName == "ThreatBig" || "ThreatSmall"
// Skill: levelInt > preLevel
// Death: DirectRelations 包含 killedPawn
namespace RimMind.Personality.Tests
{
    public class TriggerFilterPureTests
    {
        // ── Injury 过滤纯逻辑 ──────────────────────────────────────────

        /// <summary>
        /// 复刻 Patch_PersonalityInjury 的过滤条件：
        /// hediff.def.isBad == false → 过滤掉
        /// hediff.Severity < 0.2f → 过滤掉
        /// !IsFreeNonSlaveColonist → 过滤掉
        /// </summary>
        private static bool ShouldTriggerInjury(bool isBad, float severity, bool isFreeNonSlaveColonist)
        {
            // isBad == false 表示不是坏 Hediff，应过滤
            if (isBad == false) return false;
            // 严重度低于阈值，应过滤
            if (severity < 0.2f) return false;
            // 不是自由非奴隶殖民者，应过滤
            if (!isFreeNonSlaveColonist) return false;
            return true;
        }

        [Fact]
        public void InjuryFilter_IsBadFalse_ShouldFilter()
        {
            // isBad == false → 不是伤害，不触发
            Assert.False(ShouldTriggerInjury(isBad: false, severity: 0.5f, isFreeNonSlaveColonist: true));
        }

        [Fact]
        public void InjuryFilter_IsBadTrue_SeverityBelowThreshold_ShouldFilter()
        {
            // isBad == true 但严重度 < 0.2f，不触发
            Assert.False(ShouldTriggerInjury(isBad: true, severity: 0.1f, isFreeNonSlaveColonist: true));
        }

        [Fact]
        public void InjuryFilter_IsBadTrue_SeverityAtThreshold_ShouldPass()
        {
            // 严重度恰好 0.2f，应触发
            Assert.True(ShouldTriggerInjury(isBad: true, severity: 0.2f, isFreeNonSlaveColonist: true));
        }

        [Fact]
        public void InjuryFilter_IsBadTrue_SeverityAboveThreshold_ShouldPass()
        {
            // 严重度高于阈值，应触发
            Assert.True(ShouldTriggerInjury(isBad: true, severity: 0.8f, isFreeNonSlaveColonist: true));
        }

        [Fact]
        public void InjuryFilter_NotFreeColonist_ShouldFilter()
        {
            // 不是自由殖民者（如奴隶），不触发
            Assert.False(ShouldTriggerInjury(isBad: true, severity: 0.5f, isFreeNonSlaveColonist: false));
        }

        // ── Incident 过滤纯逻辑 ──────────────────────────────────────────

        /// <summary>
        /// 复刻 Patch_PersonalityIncident 的过滤条件：
        /// def.category.defName 必须是 "ThreatBig" 或 "ThreatSmall"
        /// </summary>
        private static bool ShouldTriggerIncident(string? categoryDefName)
        {
            if (categoryDefName == null) return false;
            return categoryDefName == "ThreatBig" || categoryDefName == "ThreatSmall";
        }

        [Fact]
        public void IncidentFilter_ThreatBig_ShouldPass()
        {
            Assert.True(ShouldTriggerIncident("ThreatBig"));
        }

        [Fact]
        public void IncidentFilter_ThreatSmall_ShouldPass()
        {
            Assert.True(ShouldTriggerIncident("ThreatSmall"));
        }

        [Fact]
        public void IncidentFilter_OtherCategory_ShouldFilter()
        {
            Assert.False(ShouldTriggerIncident("Misc"));
        }

        [Fact]
        public void IncidentFilter_NullCategory_ShouldFilter()
        {
            Assert.False(ShouldTriggerIncident(null));
        }

        [Fact]
        public void IncidentFilter_EmptyString_ShouldFilter()
        {
            Assert.False(ShouldTriggerIncident(""));
        }

        // ── Skill 过滤纯逻辑 ──────────────────────────────────────────

        /// <summary>
        /// 复刻 Patch_PersonalitySkill 的过滤条件：
        /// __instance.levelInt > preLevel
        /// </summary>
        private static bool ShouldTriggerSkill(int currentLevel, int previousLevel)
        {
            return currentLevel > previousLevel;
        }

        [Fact]
        public void SkillFilter_LevelIncreased_ShouldPass()
        {
            Assert.True(ShouldTriggerSkill(currentLevel: 5, previousLevel: 4));
        }

        [Fact]
        public void SkillFilter_LevelNotIncreased_ShouldFilter()
        {
            Assert.False(ShouldTriggerSkill(currentLevel: 4, previousLevel: 4));
        }

        [Fact]
        public void SkillFilter_LevelDecreased_ShouldFilter()
        {
            Assert.False(ShouldTriggerSkill(currentLevel: 3, previousLevel: 4));
        }
    }
}
