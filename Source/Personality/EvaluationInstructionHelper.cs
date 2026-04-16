using System.Text;
using Verse;

namespace RimMind.Personality
{
    public static class EvaluationInstructionHelper
    {
        private static readonly System.Random _rng = new System.Random();

        public static int SampleThoughtCount(float mu)
        {
            if (mu <= 0f) return 1;
            double L = System.Math.Exp(-mu);
            int k = 0;
            double p = 1.0;
            do { k++; p *= _rng.NextDouble(); } while (p > L);
            return System.Math.Clamp(k - 1, 1, 3);
        }

        public static string Append(string basePrompt, string? eventContext = null, int targetCount = 2, bool aiDecidesDuration = false)
        {
            var sb = new StringBuilder(basePrompt);
            sb.AppendLine();

            if (!string.IsNullOrEmpty(eventContext))
            {
                sb.AppendLine("RimMind.Personality.Prompt.TriggerReason".Translate(eventContext));
                sb.AppendLine();
            }

            sb.Append(BuildInstruction(targetCount, aiDecidesDuration));
            return sb.ToString();
        }

        private static string BuildInstruction(int targetCount, bool aiDecidesDuration)
        {
            var sb = new StringBuilder();
            sb.AppendLine("RimMind.Personality.Prompt.EvalInstruction".Translate(targetCount));
            sb.AppendLine();
            sb.AppendLine("RimMind.Personality.Prompt.JsonFormatDirect".Translate());
            if (aiDecidesDuration)
            {
                sb.AppendLine("RimMind.Personality.Prompt.DurationHint".Translate());
                sb.AppendLine("RimMind.Personality.Prompt.JsonTemplateWithDuration".Translate());
            }
            else
                sb.AppendLine("RimMind.Personality.Prompt.JsonTemplateNoDuration".Translate());
            return sb.ToString();
        }
    }
}
