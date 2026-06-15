using System.Collections.Generic;
using System.Text.Json;

namespace NinaHA.Client {

    /// <summary>Parses a JSON object string into a service-call payload dictionary.</summary>
    public static class HaServiceData {

        /// <summary>
        /// Parses <paramref name="json"/> (a JSON object) into a dictionary. Empty/whitespace yields an
        /// empty dictionary. Values are kept as <see cref="JsonElement"/> and serialize back faithfully.
        /// </summary>
        public static Dictionary<string, object?> Parse(string? json) {
            var result = new Dictionary<string, object?>();
            if (string.IsNullOrWhiteSpace(json)) {
                return result;
            }
            using var doc = JsonDocument.Parse(json!);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) {
                throw new JsonException("Service data must be a JSON object.");
            }
            foreach (var prop in doc.RootElement.EnumerateObject()) {
                result[prop.Name] = prop.Value.Clone();
            }
            return result;
        }

        /// <summary>Non-throwing variant of <see cref="Parse"/>.</summary>
        public static bool TryParse(string? json, out Dictionary<string, object?> data) {
            try {
                data = Parse(json);
                return true;
            } catch (JsonException) {
                data = new Dictionary<string, object?>();
                return false;
            }
        }
    }
}
