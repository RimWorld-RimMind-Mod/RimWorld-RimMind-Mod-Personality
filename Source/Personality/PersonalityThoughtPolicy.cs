using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RimMind.Personality
{
    public sealed class PersonalityThoughtProjection
    {
        public PersonalityThoughtProjection(
            string slotDefName,
            ThoughtEntryDto source,
            int durationTicks)
        {
            SlotDefName = slotDefName;
            Source = source;
            DurationTicks = durationTicks;
        }

        public string SlotDefName { get; }
        public ThoughtEntryDto Source { get; }
        public int DurationTicks { get; }
    }

    /// <summary>
    /// Pure policy for translating an AI personality response into owned Thought slots.
    /// RimWorld adapters consume this projection instead of duplicating slot and duration rules.
    /// </summary>
    public static class PersonalityThoughtPolicy
    {
        public const int TicksPerHour = 2500;

        private static readonly string[] OwnedSlotDefNames =
        {
            "AIPersonality_Slot_0",
            "AIPersonality_Slot_1",
            "AIPersonality_Slot_2",
        };

        public static int OwnedSlotCount => OwnedSlotDefNames.Length;

        public static bool IsOwnedThoughtDef(string? defName)
        {
            if (defName == null) return false;

            foreach (string slotDefName in OwnedSlotDefNames)
            {
                if (slotDefName == defName)
                    return true;
            }

            return false;
        }

        public static int CalculateDurationTicks(
            float? aiDurationHours,
            ThoughtDurationMode durationMode,
            float fixedDurationHours)
        {
            if (durationMode == ThoughtDurationMode.AIDecides
                && aiDurationHours.HasValue
                && aiDurationHours.Value > 0)
            {
                return (int)(Math.Clamp(aiDurationHours.Value, 1f, 24f) * TicksPerHour);
            }

            return Math.Max(1, (int)(fixedDurationHours * TicksPerHour));
        }

        public static IReadOnlyList<PersonalityThoughtProjection> Project(
            ThoughtEntryDto[]? entries,
            ThoughtDurationMode durationMode,
            float fixedDurationHours)
        {
            if (entries == null || entries.Length == 0)
                return Array.Empty<PersonalityThoughtProjection>();

            int count = Math.Min(entries.Length, OwnedSlotDefNames.Length);
            var projections = new List<PersonalityThoughtProjection>(count);
            for (int index = 0; index < count; index++)
            {
                ThoughtEntryDto? entry = entries[index];
                if (entry == null) continue;

                projections.Add(new PersonalityThoughtProjection(
                    OwnedSlotDefNames[index],
                    entry,
                    CalculateDurationTicks(entry.duration_hours, durationMode, fixedDurationHours)));
            }

            return projections;
        }

        public static PersonalityResultDto? ParseResponse(
            string? content,
            Func<string, string?> repairTruncatedJson)
        {
            if (string.IsNullOrEmpty(content))
                return null;
            if (repairTruncatedJson == null)
                throw new ArgumentNullException(nameof(repairTruncatedJson));

            string nonEmptyContent = content!;
            try
            {
                var direct = JsonConvert.DeserializeObject<PersonalityResultDto>(nonEmptyContent);
                if (direct != null)
                    return direct;
            }
            catch (JsonException)
            {
            }

            string? repaired = repairTruncatedJson(nonEmptyContent);
            if (repaired == null)
                return null;

            try
            {
                return JsonConvert.DeserializeObject<PersonalityResultDto>(repaired);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public static bool ShouldRecordShapingAction(ShapingAction action)
            => action != ShapingAction.Ignore;
    }
}
