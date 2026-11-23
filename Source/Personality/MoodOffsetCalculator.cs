using System;

namespace RimMind.Personality
{
    /// <summary>
    /// Maps AI-returned intensity (-3..+3) to RimWorld mood offset values.
    /// Pure static helper with no RimWorld dependencies for direct unit testing.
    /// </summary>
    public static class MoodOffsetCalculator
    {
        // -3 => -10, -2 => -3, -1 => -1, 0 => 0, +1 => +1, +2 => +3, +3 => +10
        private static readonly float[] MoodTable = { -10f, -3f, -1f, 0f, +1f, +3f, +10f };

        /// <summary>
        /// Computes the RimWorld mood offset for the given AI intensity, clamping out-of-range
        /// values to [-3, 3].
        /// </summary>
        public static float CalcMoodOffset(float intensity)
        {
            int idx = Math.Max(0, Math.Min((int)intensity + 3, MoodTable.Length - 1));
            return MoodTable[idx];
        }
    }
}
