using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NinaHA.Client.Configuration {

    /// <summary>
    /// The full plugin configuration: how to reach Home Assistant and which channels to expose.
    /// Serialized to / from JSON for persistence in the NINA plugin settings store.
    /// </summary>
    public sealed class HomeAssistantConfig {

        /// <summary>Base URL of the Home Assistant instance, e.g. <c>http://homeassistant.local:8123</c>.</summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>Long-lived access token used as a bearer token.</summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>When true, subscribe to live state updates over the WebSocket API (with REST fallback).</summary>
        public bool UseWebSocket { get; set; } = true;

        public List<SwitchChannel> Channels { get; set; } = new List<SwitchChannel>();

        [JsonIgnore]
        public bool HasConnection => !string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(Token);

        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        public string Serialize() => JsonSerializer.Serialize(this, SerializerOptions);

        public static HomeAssistantConfig Deserialize(string? json) {
            if (string.IsNullOrWhiteSpace(json)) {
                return new HomeAssistantConfig();
            }
            try {
                return JsonSerializer.Deserialize<HomeAssistantConfig>(json!, SerializerOptions) ?? new HomeAssistantConfig();
            } catch (JsonException) {
                return new HomeAssistantConfig();
            }
        }
    }
}
