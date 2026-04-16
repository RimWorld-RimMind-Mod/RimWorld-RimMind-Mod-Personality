using RimMind.Personality;
using Xunit;

// 测试 EvaluationInstructionHelper.Append 的纯字符串逻辑
// 不依赖 RimWorld，不需要 Pawn / DefDatabase
namespace RimMind.Personality.Tests
{
    public class EvaluationInstructionBuilderTests
    {
        // ── 1. 无事件上下文：不含 [触发原因] 行 ──────────────────────────

        [Fact]
        public void Append_NoEventContext_NoTriggerLine()
        {
            string result = EvaluationInstructionHelper.Append("basePrompt", targetCount: 2);

            Assert.DoesNotContain("[触发原因]", result);
            Assert.Contains("JSON", result);
        }

        // ── 2. 有事件上下文：含 [触发原因] 行 ───────────────────────────

        [Fact]
        public void Append_WithEventContext_ContainsTriggerLine()
        {
            string result = EvaluationInstructionHelper.Append("basePrompt", "刚刚在战斗中受重伤", targetCount: 2);

            Assert.Contains("[触发原因] 刚刚在战斗中受重伤", result);
            Assert.Contains("JSON", result);
        }

        // ── 3. 空字符串事件上下文视为无上下文 ──────────────────────────

        [Fact]
        public void Append_EmptyEventContext_NoTriggerLine()
        {
            string result = EvaluationInstructionHelper.Append("basePrompt", "", targetCount: 2);

            Assert.DoesNotContain("[触发原因]", result);
        }

        // ── 4. null 事件上下文视为无上下文 ──────────────────────────────

        [Fact]
        public void Append_NullEventContext_NoTriggerLine()
        {
            string result = EvaluationInstructionHelper.Append("basePrompt", null, targetCount: 2);

            Assert.DoesNotContain("[触发原因]", result);
        }

        // ── 5. 基础 prompt 内容原样保留在开头 ───────────────────────────

        [Fact]
        public void Append_PreservesBasePrompt()
        {
            string base_ = "Alice 的游戏状态：心情 80%，当前任务：挖矿";
            string result = EvaluationInstructionHelper.Append(base_);

            Assert.StartsWith(base_, result);
        }

        // ── 6. 评估指令包含必要的格式关键字 ────────────────────────────

        [Fact]
        public void Append_ContainsFormatInstruction()
        {
            string result = EvaluationInstructionHelper.Append("base", targetCount: 2);

            Assert.Contains("\"thoughts\"", result);
            Assert.Contains("\"narrative\"", result);
            Assert.Contains("intensity", result);
        }

        // ── 7. 事件上下文出现在基础 prompt 之后、格式指令之前 ────────────

        [Fact]
        public void Append_EventContextOrder_IsCorrect()
        {
            string result = EvaluationInstructionHelper.Append("基础", "技能升级", targetCount: 2);

            int baseIdx    = result.IndexOf("基础");
            int triggerIdx = result.IndexOf("[触发原因]");
            int jsonIdx    = result.IndexOf("\"thoughts\"");

            Assert.True(baseIdx < triggerIdx, "基础 prompt 应在触发原因之前");
            Assert.True(triggerIdx < jsonIdx,  "触发原因应在格式指令之前");
        }
    }
}
