using Xunit;
using RimMind.Personality;

namespace RimMind.Personality.Tests
{
    public class PersonalityRequestBuilderTests
    {
        [Theory]
        [InlineData(1, "NPC-1")]
        [InlineData(42, "NPC-42")]
        [InlineData(99999, "NPC-99999")]
        public void BuildNpcId_ReturnsFormattedId(int thingId, string expected)
        {
            Assert.Equal(expected, PersonalityRequestBuilder.BuildNpcId(thingId));
        }

        [Fact]
        public void BuildNpcId_Zero_ReturnsNpcDashZero()
        {
            Assert.Equal("NPC-0", PersonalityRequestBuilder.BuildNpcId(0));
        }

        [Fact]
        public void BuildNpcId_Negative_ReturnsNpcDashNegative()
        {
            Assert.Equal("NPC--1", PersonalityRequestBuilder.BuildNpcId(-1));
        }
    }
}
