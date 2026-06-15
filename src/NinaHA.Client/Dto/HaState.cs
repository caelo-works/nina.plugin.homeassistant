using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NinaHA.Client.Dto {

    /// <summary>
    /// Represents a single Home Assistant entity state as returned by
    /// <c>GET /api/states</c> and <c>GET /api/states/&lt;entity_id&gt;</c>.
    /// </summary>
    public sealed class HaState {

        [JsonPropertyName("entity_id")]
        public string EntityId { get; set; } = string.Empty;

        [JsonPropertyName("state")]
        public string State { get; set; } = string.Empty;

        [JsonPropertyName("attributes")]
        public Dictionary<string, JsonElement> Attributes { get; set; } = new Dictionary<string, JsonElement>();

        [JsonPropertyName("last_changed")]
        public DateTimeOffset? LastChanged { get; set; }

        [JsonPropertyName("last_updated")]
        public DateTimeOffset? LastUpdated { get; set; }

        /// <summary>The entity domain, i.e. the part before the first dot in the entity id.</summary>
        [JsonIgnore]
        public string Domain {
            get {
                var idx = EntityId.IndexOf('.');
                return idx > 0 ? EntityId.Substring(0, idx) : string.Empty;
            }
        }

        /// <summary>The human friendly name if present, otherwise the entity id.</summary>
        [JsonIgnore]
        public string FriendlyName => TryGetAttributeString("friendly_name", out var name) && !string.IsNullOrWhiteSpace(name)
            ? name!
            : EntityId;

        /// <summary>Reads an attribute as a string, regardless of its underlying JSON type.</summary>
        public bool TryGetAttributeString(string name, out string? value) {
            value = null;
            if (!Attributes.TryGetValue(name, out var el)) {
                return false;
            }
            value = el.ValueKind == JsonValueKind.String ? el.GetString() : el.ToString();
            return true;
        }

        /// <summary>Reads a numeric attribute (number/string encoded) as a double.</summary>
        public bool TryGetAttributeDouble(string name, out double value) {
            value = 0d;
            if (!Attributes.TryGetValue(name, out var el)) {
                return false;
            }
            switch (el.ValueKind) {
                case JsonValueKind.Number:
                    value = el.GetDouble();
                    return true;
                case JsonValueKind.String:
                    return double.TryParse(el.GetString(), System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out value);
                default:
                    return false;
            }
        }

        /// <summary>Reads a string-list attribute (e.g. a <c>select</c> entity's <c>options</c>).</summary>
        public bool TryGetAttributeStringList(string name, out IReadOnlyList<string> value) {
            value = Array.Empty<string>();
            if (!Attributes.TryGetValue(name, out var el) || el.ValueKind != JsonValueKind.Array) {
                return false;
            }
            var list = new List<string>();
            foreach (var item in el.EnumerateArray()) {
                list.Add(item.ValueKind == JsonValueKind.String ? item.GetString() ?? string.Empty : item.ToString());
            }
            value = list;
            return true;
        }
    }
}
