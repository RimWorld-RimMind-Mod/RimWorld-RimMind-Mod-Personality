using System;
using Xunit;

// 测试 Thought_AIPersonality 中的纯逻辑
// DurationTicks: customDurationTicks > 0 时返回自定义值，否则返回 base
// MoodOffset: 委托给 MoodOffsetCalculator.CalcMoodOffset
namespace RimMind.Personality.Tests
{
    public class ThoughtAIPersonalityPureTests
    {
        // ── 辅助方法：复刻 Thought_AIPersonality.DurationTicks 的纯逻辑 ──

        /// <summary>
        /// 复刻 Thought_AIPersonality.DurationTicks 属性逻辑：
        /// customDurationTicks > 0 → 返回 customDurationTicks
        /// 否则 → 返回 baseDurationTicks
        /// </summary>
        private static int CalcDurationTicks(int customDurationTicks, int baseDurationTicks)
        {
            return customDurationTicks > 0 ? customDurationTicks : baseDurationTicks;
        }

        // ── DurationTicks 测试 ──────────────────────────────────────────

        [Fact]
        public void DurationTicks_CustomPositive_ReturnsCustom()
        {
            // customDurationTicks > 0 → 使用自定义值
            Assert.Equal(5000, CalcDurationTicks(customDurationTicks: 5000, baseDurationTicks: 2500));
        }

        [Fact]
        public void DurationTicks_CustomZero_ReturnsBase()
        {
            // customDurationTicks = 0 → 使用 base
            Assert.Equal(2500, CalcDurationTicks(customDurationTicks: 0, baseDurationTicks: 2500));
        }

        [Fact]
        public void DurationTicks_CustomNegative_ReturnsBase()
        {
            // customDurationTicks = -1（默认值）→ 使用 base
            Assert.Equal(2500, CalcDurationTicks(customDurationTicks: -1, baseDurationTicks: 2500));
        }

        [Fact]
        public void DurationTicks_CustomOne_ReturnsCustom()
        {
            // customDurationTicks = 1（最小正整数）→ 使用自定义值
            Assert.Equal(1, CalcDurationTicks(customDurationTicks: 1, baseDurationTicks: 2500));
        }

        // ── MoodOffset 委托测试 ──────────────────────────────────────────

        [Theory]
        [InlineData(-3, -10f)]
        [InlineData(0, 0f)]
        [InlineData(3, 10f)]
        public void MoodOffset_DelegatesToCalcMoodOffset(int aiIntensity, float expected)
        {
            // Thought_AIPersonality.MoodOffset() 直接调用 MoodOffsetCalculator.CalcMoodOffset(aiIntensity)
            float result = MoodOffsetCalculator.CalcMoodOffset(aiIntensity);
            Assert.Equal(expected, result);
        }

        // ── CalcDurationTicks 与 MoodOffset 联合验证 ──────────────────

        [Fact]
        public void Combined_DurationAndMoodOffset_AIDecides12Hours()
        {
            // 模拟 AIDecides 模式下 duration_hours=12, intensity=2
            float durationHours = 12f;
            int ticksPerHour = 2500;
            int expectedTicks = (int)(Math.Clamp(durationHours, 1f, 24f) * ticksPerHour);
            Assert.Equal(30000, expectedTicks);

            float moodOffset = MoodOffsetCalculator.CalcMoodOffset(2);
            Assert.Equal(3f, moodOffset);
        }
    }
}
