using NinaHA.Client;
using Xunit;

namespace NinaHA.Client.Tests {

    public class HaUrlTests {

        [Theory]
        [InlineData("http://homeassistant.local:8123", "http://homeassistant.local:8123")]
        [InlineData("http://homeassistant.local:8123/", "http://homeassistant.local:8123")]
        [InlineData("  http://host:8123/  ", "http://host:8123")]
        [InlineData("homeassistant.local:8123", "http://homeassistant.local:8123")]
        [InlineData("192.168.1.10:8123", "http://192.168.1.10:8123")]
        [InlineData("https://ha.example.com/", "https://ha.example.com")]
        [InlineData("https://ha.example.com///", "https://ha.example.com")]
        [InlineData("", "")]
        [InlineData("   ", "")]
        [InlineData(null, "")]
        public void Normalize(string? input, string expected) {
            Assert.Equal(expected, HaUrl.Normalize(input));
        }
    }
}
