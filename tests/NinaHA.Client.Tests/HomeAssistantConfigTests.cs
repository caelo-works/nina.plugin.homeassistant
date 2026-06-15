using System.Collections.Generic;
using NinaHA.Client.Configuration;
using Xunit;

namespace NinaHA.Client.Tests {

    public class HomeAssistantConfigTests {

        [Fact]
        public void Roundtrips_through_json() {
            var config = new HomeAssistantConfig {
                BaseUrl = "http://ha.local:8123",
                Token = "secret",
                UseWebSocket = false,
                Channels = new List<SwitchChannel> {
                    new SwitchChannel {
                        Name = "Power",
                        EntityId = "switch.power",
                        Type = ChannelType.Binary,
                        Direction = ChannelDirection.ReadWrite
                    },
                    new SwitchChannel {
                        Name = "Mode",
                        EntityId = "select.mode",
                        Type = ChannelType.Stepped,
                        Direction = ChannelDirection.ReadWrite,
                        Options = new List<string> { "a", "b" }
                    }
                }
            };

            var restored = HomeAssistantConfig.Deserialize(config.Serialize());

            Assert.Equal("http://ha.local:8123", restored.BaseUrl);
            Assert.Equal("secret", restored.Token);
            Assert.False(restored.UseWebSocket);
            Assert.Equal(2, restored.Channels.Count);
            Assert.Equal(ChannelType.Stepped, restored.Channels[1].Type);
            Assert.Equal(new[] { "a", "b" }, restored.Channels[1].Options);
        }

        [Fact]
        public void Deserialize_handles_null_and_garbage() {
            Assert.NotNull(HomeAssistantConfig.Deserialize(null));
            Assert.Empty(HomeAssistantConfig.Deserialize(null).Channels);
            Assert.NotNull(HomeAssistantConfig.Deserialize("not json"));
        }

        [Fact]
        public void HasConnection_requires_url_and_token() {
            Assert.False(new HomeAssistantConfig().HasConnection);
            Assert.False(new HomeAssistantConfig { BaseUrl = "x" }.HasConnection);
            Assert.True(new HomeAssistantConfig { BaseUrl = "x", Token = "y" }.HasConnection);
        }
    }
}
