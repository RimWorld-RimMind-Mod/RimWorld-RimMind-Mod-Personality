using System;
using Xunit;

// 测试 CalcDurationTicks 的边界场景
// AIDecides 模式：duration_hours 为 null/0/负数时回退到 Fixed 模式
// Fixed 模式：thoughtDurationHours * TicksPerHour，最小为 1
namespace RimMind.Personality.Tests
{
    // 本地枚举，复刻 AIPersonalitySettings.ThoughtDurationMode
    // （AIPersonalitySettings 继承 ModSettings，无法在纯逻辑测试中引用）
    internal enum ThoughtDurationMode { Fixed, AIDecides }

    public class DurationCalcEdgeTests
    {
        private const int TicksPerHour = 2500;

        /// <summary>
        /// 复刻 PersonalityThoughtMapper.CalcDurationTicks 的纯逻辑
        /// </summary>
        private static int CalcDurationTicks(
            ThoughtDurationMode mode,
            float? durationHours,
            float thoughtDurationHours)
        {
            if (mode == ThoughtDurationMode.AIDecides
                && durationHours.HasValue
                && durationHours.Value > 0)
            {
                return (int)(Math.Clamp(durationHours.Value, 1f, 24f) * TicksPerHour);
            }
            return Math.Max(1, (int)(thoughtDurationHours * TicksPerHour));
        }

        // ── AIDecides 模式 ──────────────────────────────────────────────

        [Fact]
        public void AIDecides_NullDurationHours_FallsBackToFixed()
        {
            // duration_hours 为 null → 回退到 Fixed 模式
            int ticks = CalcDurationTicks(ThoughtDurationMode.AIDecides, durationHours: null, thoughtDurationHours: 24f);
            Assert.Equal(60000, ticks);
        }

        [Fact]
        public void AIDecides_DurationZero_FallsBackToFixed()
        {
            // duration_hours = 0 → 不满足 > 0 条件，回退到 Fixed
            int ticks = CalcDurationTicks(ThoughtDurationMode.AIDecides, durationHours: 0f, thoughtDurationHours: 8f);
            Assert.Equal(20000, ticks);
        }

        [Fact]
        public void AIDecides_DurationNegative_FallsBackToFixed()
        {
            // duration_hours 为负数 → 不满足 > 0 条件，回退到 Fixed
            int ticks = CalcDurationTicks(ThoughtDurationMode.AIDecides, durationHours: -5f, thoughtDurationHours: 12f);
            Assert.Equal(30000, ticks);
        }

        [Fact]
        public void AIDecides_DurationFractional_Truncates()
        {
            // duration_hours = 1.5f → (int)(1.5 * 2500) = (int)(3750) = 3750
            int ticks = CalcDurationTicks(ThoughtDurationMode.AIDecides, durationHours: 1.5f, thoughtDurationHours: 24f);
            Assert.Equal(3750, ticks);
        }

        // ── Fixed 模式 ──────────────────────────────────────────────────

        [Fact]
        public void FixedMode_ZeroHours_Min1Tick()
        {
            // thoughtDurationHours = 0 → Math.Max(1, 0) = 1
            int ticks = CalcDurationTicks(ThoughtDurationMode.Fixed, durationHours: null, thoughtDurationHours: 0f);
            Assert.Equal(1, ticks);
        }

        [Fact]
        public void FixedMode_VerySmallHours_Min1Tick()
        {
            // thoughtDurationHours = 0.0001f → (int)(0.0001 * 2500) = (int)(0.25) = 0 → Math.Max(1, 0) = 1
            int ticks = CalcDurationTicks(ThoughtDurationMode.Fixed, durationHours: null, thoughtDurationHours: 0.0001f);
            Assert.Equal(1, ticks);
        }

        [Fact]
        public void FixedMode_OneHour()
        {
            int ticks = CalcDurationTicks(ThoughtDurationMode.Fixed, durationHours: null, thoughtDurationHours: 1f);
            Assert.Equal(2500, ticks);
        }

        // ── ThoughtDurationMode 枚举值验证 ──────────────────────────────

        [Fact]
        public void ThoughtDurationMode_HasTwoValues()
        {
            var values = Enum.GetValues(typeof(ThoughtDurationMode));
            Assert.Equal(2, values.Length);
            Assert.Contains(ThoughtDurationMode.Fixed, values);
            Assert.Contains(ThoughtDurationMode.AIDecides, values);
        }
    }
}
