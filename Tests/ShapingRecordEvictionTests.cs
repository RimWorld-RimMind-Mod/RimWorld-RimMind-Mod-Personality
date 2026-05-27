using System;
using System.Collections.Generic;
using Xunit;

// 测试 ShapingRecord 驱逐逻辑的边界场景
// 纯逻辑，不依赖 RimWorld
namespace RimMind.Personality.Tests
{
    public class ShapingRecordEvictionTests
    {
        // ── 辅助类：复刻 PersonalityProfile.AddShapingRecord 的纯逻辑 ──

        private class TestProfile
        {
            public List<TestRecord> History = new List<TestRecord>();

            public void AddShapingRecord(TestRecord record, int maxCount)
            {
                // 与 PersonalityProfile.AddShapingRecord 逻辑一致
                int effectiveMax = Math.Max(maxCount, 1);
                History.Add(record);
                if (History.Count > effectiveMax)
                    History.RemoveRange(0, History.Count - effectiveMax);
            }
        }

        private class TestRecord
        {
            public string label = "";
            public string action = "";
            public int tick;
        }

        // ── 测试用例 ──────────────────────────────────────────────────

        [Fact]
        public void Eviction_NegativeMaxCount_ClampedToOne()
        {
            // maxCount 为负数时，Math.Max(maxCount, 1) = 1，只保留最后一条
            var profile = new TestProfile();
            profile.AddShapingRecord(new TestRecord { label = "first", action = "reinforce" }, -5);
            profile.AddShapingRecord(new TestRecord { label = "second", action = "suppress" }, -5);

            Assert.Single(profile.History);
            Assert.Equal("second", profile.History[0].label);
        }

        [Fact]
        public void Eviction_ExactMaxCount_NoEviction()
        {
            // 记录数恰好等于 maxCount，不触发驱逐
            var profile = new TestProfile();
            profile.AddShapingRecord(new TestRecord { label = "r0", action = "a0" }, 3);
            profile.AddShapingRecord(new TestRecord { label = "r1", action = "a1" }, 3);
            profile.AddShapingRecord(new TestRecord { label = "r2", action = "a2" }, 3);

            Assert.Equal(3, profile.History.Count);
            Assert.Equal("r0", profile.History[0].label);
            Assert.Equal("r2", profile.History[2].label);
        }

        [Fact]
        public void Eviction_LargeHistory_EvictsOldest()
        {
            // 添加 100 条记录，maxCount=10，只保留最后 10 条
            var profile = new TestProfile();
            for (int i = 0; i < 100; i++)
                profile.AddShapingRecord(new TestRecord { label = $"r{i}", action = "a" }, 10);

            Assert.Equal(10, profile.History.Count);
            Assert.Equal("r90", profile.History[0].label);
            Assert.Equal("r99", profile.History[9].label);
        }

        [Fact]
        public void Eviction_RecordsRetainCorrectData()
        {
            // 驱逐后，保留记录的 action 和 tick 字段未被篡改
            var profile = new TestProfile();
            profile.AddShapingRecord(new TestRecord { label = "old", action = "reinforce", tick = 100 }, 2);
            profile.AddShapingRecord(new TestRecord { label = "mid", action = "suppress", tick = 200 }, 2);
            profile.AddShapingRecord(new TestRecord { label = "new", action = "ignored", tick = 300 }, 2);

            Assert.Equal(2, profile.History.Count);
            Assert.Equal("mid", profile.History[0].label);
            Assert.Equal("suppress", profile.History[0].action);
            Assert.Equal(200, profile.History[0].tick);
            Assert.Equal("new", profile.History[1].label);
            Assert.Equal("ignored", profile.History[1].action);
            Assert.Equal(300, profile.History[1].tick);
        }

        [Fact]
        public void Eviction_MultipleRounds_CorrectState()
        {
            // 多轮添加+驱逐后，状态正确
            var profile = new TestProfile();
            // 第一轮：maxCount=2
            profile.AddShapingRecord(new TestRecord { label = "a" }, 2);
            profile.AddShapingRecord(new TestRecord { label = "b" }, 2);
            Assert.Equal(2, profile.History.Count);

            // 第二轮：添加第三条，驱逐最旧
            profile.AddShapingRecord(new TestRecord { label = "c" }, 2);
            Assert.Equal(2, profile.History.Count);
            Assert.Equal("b", profile.History[0].label);

            // 第三轮：缩小 maxCount=1，下次添加时驱逐
            profile.AddShapingRecord(new TestRecord { label = "d" }, 1);
            Assert.Single(profile.History);
            Assert.Equal("d", profile.History[0].label);
        }

        [Fact]
        public void Eviction_SingleRecord_NoEviction()
        {
            // 只添加一条记录，任何 maxCount >= 1 都不驱逐
            var profile = new TestProfile();
            profile.AddShapingRecord(new TestRecord { label = "only", action = "reinforce" }, 5);

            Assert.Single(profile.History);
            Assert.Equal("only", profile.History[0].label);
        }
    }
}
