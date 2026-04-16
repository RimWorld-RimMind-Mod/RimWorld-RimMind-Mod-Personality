using RimMind.Core;
using Verse;

namespace RimMind.Personality
{
    /// <summary>
    /// 构建发送给 AI 的每日人格评估 Prompt。
    /// 纯字符串拼接逻辑提取在 EvaluationInstructionHelper（无 RimWorld 依赖，可单元测试）。
    /// </summary>
    public static class PersonalityContextBuilder
    {
        /// <summary>
        /// 组装完整评估 Prompt。
        /// 排除 personality_state（避免旧 Thought 干扰当次评估）。
        /// targetCount 由调用方 Poisson 抽样后传入。
        /// </summary>
        public static string BuildEvaluationPrompt(Pawn pawn, string? eventContext = null, int targetCount = 2)
        {
            string basePrompt = RimMindAPI.BuildFullPawnPrompt(
                pawn,
                excludeProviders: new[] { "personality_state" });

            bool aiDecidesDuration = RimMindPersonalityMod.Settings.durationMode == ThoughtDurationMode.AIDecides;
            return EvaluationInstructionHelper.Append(basePrompt, eventContext, targetCount, aiDecidesDuration);
        }
    }
}
