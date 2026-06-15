using System.Text.Json;
using NinaHA.Client;
using Xunit;

namespace NinaHA.Client.Tests {

    public class HaServiceDataTests {

        [Fact]
        public void Parse_empty_is_empty_dictionary() {
            Assert.Empty(HaServiceData.Parse(null));
            Assert.Empty(HaServiceData.Parse("   "));
        }

        [Fact]
        public void Parse_object_keeps_values() {
            var data = HaServiceData.Parse("{\"brightness_pct\": 60, \"flash\": \"short\"}");
            Assert.Equal(2, data.Count);
            Assert.Equal(60, ((JsonElement)data["brightness_pct"]!).GetInt32());
            Assert.Equal("short", ((JsonElement)data["flash"]!).GetString());
        }

        [Fact]
        public void Parse_rejects_non_object() {
            Assert.Throws<JsonException>(() => HaServiceData.Parse("[1,2,3]"));
        }

        [Fact]
        public void TryParse_reports_validity() {
            Assert.True(HaServiceData.TryParse("{\"a\":1}", out _));
            Assert.False(HaServiceData.TryParse("not json", out _));
            Assert.False(HaServiceData.TryParse("\"astring\"", out _));
        }
    }
}
