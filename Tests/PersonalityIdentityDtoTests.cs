using Newtonsoft.Json;
using Xunit;

// 测试 PersonalityIdentityDto 的默认值、序列化/反序列化
// 纯逻辑，不依赖 RimWorld
namespace RimMind.Personality.Tests
{
    public class PersonalityIdentityDtoTests
    {
        // ── 默认值测试 ──────────────────────────────────────────────────

        [Fact]
        public void Defaults_AllFieldsNull()
        {
            var dto = new PersonalityIdentityDto();
            Assert.Null(dto.motivations);
            Assert.Null(dto.traits);
            Assert.Null(dto.core_values);
        }

        // ── 赋值测试 ──────────────────────────────────────────────────

        [Fact]
        public void SetMotivations_RoundTrip()
        {
            var dto = new PersonalityIdentityDto
            {
                motivations = new[] { "survival", "creativity" }
            };
            Assert.Equal(2, dto.motivations!.Length);
            Assert.Equal("survival", dto.motivations[0]);
            Assert.Equal("creativity", dto.motivations[1]);
        }

        [Fact]
        public void SetTraits_RoundTrip()
        {
            var dto = new PersonalityIdentityDto
            {
                traits = new[] { "brave", "kind" }
            };
            Assert.Equal(2, dto.traits!.Length);
            Assert.Equal("brave", dto.traits[0]);
        }

        [Fact]
        public void SetCoreValues_RoundTrip()
        {
            var dto = new PersonalityIdentityDto
            {
                core_values = new[] { "loyalty" }
            };
            Assert.Single(dto.core_values!);
            Assert.Equal("loyalty", dto.core_values[0]);
        }

        // ── JSON 序列化/反序列化测试 ──────────────────────────────────────

        [Fact]
        public void JsonSerialization_RoundTrip()
        {
            var original = new PersonalityIdentityDto
            {
                motivations = new[] { "freedom" },
                traits = new[] { "stubborn" },
                core_values = new[] { "honesty" }
            };

            string json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<PersonalityIdentityDto>(json);

            Assert.NotNull(deserialized);
            Assert.Single(deserialized!.motivations!);
            Assert.Equal("freedom", deserialized.motivations![0]);
            Assert.Single(deserialized.traits!);
            Assert.Equal("stubborn", deserialized.traits![0]);
            Assert.Single(deserialized.core_values!);
            Assert.Equal("honesty", deserialized.core_values![0]);
        }

        [Fact]
        public void JsonSerialization_NullFields()
        {
            // 序列化空 DTO，反序列化后字段仍为 null
            var original = new PersonalityIdentityDto();
            string json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<PersonalityIdentityDto>(json);

            Assert.NotNull(deserialized);
            Assert.Null(deserialized!.motivations);
            Assert.Null(deserialized.traits);
            Assert.Null(deserialized.core_values);
        }

        // ── PersonalityResultDto 含 identity 的完整反序列化 ──────────────

        [Fact]
        public void FullDto_WithIdentity_DeserializesCorrectly()
        {
            string json = @"{
                ""thoughts"": [{""type"":""state"",""label"":""test"",""description"":""desc"",""intensity"":1}],
                ""narrative"": ""narrative text"",
                ""identity"": {
                    ""motivations"": [""survival""],
                    ""traits"": [""brave""],
                    ""core_values"": [""loyalty""]
                }
            }";

            var dto = JsonConvert.DeserializeObject<PersonalityResultDto>(json);

            Assert.NotNull(dto);
            Assert.NotNull(dto!.identity);
            Assert.Single(dto.identity!.motivations!);
            Assert.Equal("survival", dto.identity.motivations![0]);
            Assert.Single(dto.identity.traits!);
            Assert.Equal("brave", dto.identity.traits![0]);
            Assert.Single(dto.identity.core_values!);
            Assert.Equal("loyalty", dto.identity.core_values![0]);
        }

        [Fact]
        public void FullDto_WithoutIdentity_IdentityIsNull()
        {
            string json = @"{""thoughts"":[],""narrative"":""test""}";
            var dto = JsonConvert.DeserializeObject<PersonalityResultDto>(json);

            Assert.NotNull(dto);
            Assert.Null(dto!.identity);
        }
    }
}
