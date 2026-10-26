using System;
using System.Collections.Generic;

namespace RimMind.Personality
{
    /// <summary>
    /// Pure text helpers for Personality context blocks (no Verse dependency, unit-tested).
    /// </summary>
    internal static class PersonalityContextText
    {
        /// <summary>
        /// Formats a header followed by one line per non-empty entry. The header always ends its own
        /// line, so the first entry never runs into it. Returns null when there are no entries.
        /// </summary>
        internal static string? FormatHeaderedLines(string? header, IEnumerable<string?> lines)
        {
            if (lines == null) throw new ArgumentNullException(nameof(lines));
            var sb = new System.Text.StringBuilder();
            if (!string.IsNullOrWhiteSpace(header))
                sb.Append(header!.TrimEnd()).Append('\n');
            bool any = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                sb.Append(line!.TrimEnd()).Append('\n');
                any = true;
            }
            return any ? sb.ToString().TrimEnd() : null;
        }

        /// <summary>Returns the last <paramref name="maxCount"/> items of <paramref name="items"/> in order.</summary>
        internal static List<T> TakeLast<T>(IReadOnlyList<T> items, int maxCount)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            var result = new List<T>();
            if (maxCount <= 0) return result;
            for (int i = Math.Max(0, items.Count - maxCount); i < items.Count; i++)
                result.Add(items[i]);
            return result;
        }
    }
}
