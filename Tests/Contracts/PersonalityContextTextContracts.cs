using System;
using System.Linq;
using Xunit;

namespace RimMind.Personality.Tests.Contracts
{
    public sealed class PersonalityContextTextContracts
    {
        [Fact]
        public void Header_ends_its_own_line_so_first_entry_does_not_run_into_it()
        {
            string? text = PersonalityContextText.FormatHeaderedLines(
                "[RimMind Shaping]", new[] { "- [reinforce] brave", "- [suppress] lazy" });

            Assert.Equal("[RimMind Shaping]\n- [reinforce] brave\n- [suppress] lazy", text);
        }

        [Fact]
        public void No_entries_yields_null_and_blank_entries_are_skipped()
        {
            Assert.Null(PersonalityContextText.FormatHeaderedLines("Header", new string?[] { null, "  " }));
            Assert.Equal("only", PersonalityContextText.FormatHeaderedLines(null, new[] { "only" }));
        }

        [Fact]
        public void TakeLast_keeps_most_recent_items_in_original_order()
        {
            var items = Enumerable.Range(1, 8).ToArray();

            Assert.Equal(new[] { 4, 5, 6, 7, 8 }, PersonalityContextText.TakeLast(items, 5));
            Assert.Equal(items, PersonalityContextText.TakeLast(items, 20));
            Assert.Empty(PersonalityContextText.TakeLast(items, 0));
        }
    }
}
