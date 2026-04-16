using System;

namespace RimMind.Personality
{
    /// <summary>
    /// AI 响应中 &lt;Personality&gt;...&lt;/Personality&gt; 标签内的 JSON 对应 DTO。
    /// 无 RimWorld 依赖，供单元测试和 PersonalityThoughtMapper 共用。
    /// </summary>
    public class PersonalityResultDto
    {
        public ThoughtEntryDto[] thoughts { get; set; } = Array.Empty<ThoughtEntryDto>();
        public string narrative { get; set; } = string.Empty;
    }

    public class ThoughtEntryDto
    {
        /// <summary>"state" = 影响心情 | "behavior" = 纯行为标志</summary>
        public string type { get; set; } = "state";
        /// <summary>≤8字，显示为 Thought 名称</summary>
        public string label { get; set; } = string.Empty;
        /// <summary>≤20字，第三人称，显示为 Thought hover 说明</summary>
        public string description { get; set; } = string.Empty;
        /// <summary>-3~+3，由 MoodOffsetCalculator.CalcMoodOffset 转换为实际心情值</summary>
        public int intensity { get; set; }
        /// <summary>可选，仅 durationMode=AIDecides 时由 AI 填写；游戏小时数（1~24）。</summary>
        public int? duration_hours { get; set; } = null;
    }
}
