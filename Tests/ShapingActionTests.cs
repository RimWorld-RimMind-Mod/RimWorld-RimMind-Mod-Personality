using Xunit;
using RimMind.Personality;

namespace RimMind.Personality.Tests
{
    public class ShapingActionTests
    {
        [Theory]
        [InlineData(ShapingAction.Reinforce, "reinforce")]
        [InlineData(ShapingAction.Suppress, "suppress")]
        [InlineData(ShapingAction.Ignore, "ignored")]
        public void ToActionString_ReturnsCorrectString(ShapingAction action, string expected)
        {
            Assert.Equal(expected, action.ToActionString());
        }

        [Theory]
        [InlineData("reinforce", ShapingAction.Reinforce)]
        [InlineData("suppress", ShapingAction.Suppress)]
        [InlineData("ignored", ShapingAction.Ignore)]
        public void FromString_ReturnsCorrectEnum(string value, ShapingAction expected)
        {
            Assert.Equal(expected, ShapingActionExtensions.FromString(value));
        }

        [Fact]
        public void FromString_UnknownString_ReturnsIgnore()
        {
            Assert.Equal(ShapingAction.Ignore, ShapingActionExtensions.FromString("unknown"));
        }

        [Fact]
        public void FromString_NullString_ReturnsIgnore()
        {
            Assert.Equal(ShapingAction.Ignore, ShapingActionExtensions.FromString(null!));
        }

        [Fact]
        public void Ignore_ToActionString_IsIgnored()
        {
            Assert.Equal("ignored", ShapingAction.Ignore.ToActionString());
        }
    }
}
