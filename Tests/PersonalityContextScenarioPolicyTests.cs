using Xunit;

namespace RimMind.Personality.Tests
{
    public class PersonalityContextScenarioPolicyTests
    {
        [Theory]
        [InlineData("Personality")]
        [InlineData("Decision")]
        [InlineData("Dialogue")]
        public void Includes_Personality_Context_For_Consumer_Scenarios(string scenario)
        {
            Assert.True(PersonalityContextScenarioPolicy.IncludesPersonalityContext(scenario));
        }

        [Theory]
        [InlineData("Storyteller")]
        [InlineData("Memory")]
        [InlineData("")]
        [InlineData(null)]
        public void Excludes_Personality_Context_For_Unrelated_Scenarios(string? scenario)
        {
            Assert.False(PersonalityContextScenarioPolicy.IncludesPersonalityContext(scenario));
        }
    }
}
