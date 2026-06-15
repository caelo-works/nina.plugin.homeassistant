using NinaHA.Client;
using Xunit;

namespace NinaHA.Client.Tests {

    public class ParseServicesTests {

        [Fact]
        public void Flattens_domains_and_services_sorted() {
            const string json = @"[
                { ""domain"": ""switch"", ""services"": { ""turn_on"": {}, ""turn_off"": {} } },
                { ""domain"": ""light"",  ""services"": { ""turn_on"": {} } }
            ]";
            var result = HomeAssistantRestClient.ParseServices(json);
            Assert.Equal(new[] { "light.turn_on", "switch.turn_off", "switch.turn_on" }, result);
        }

        [Fact]
        public void Handles_empty_and_malformed() {
            Assert.Empty(HomeAssistantRestClient.ParseServices("[]"));
            Assert.Empty(HomeAssistantRestClient.ParseServices("{}"));
        }
    }
}
