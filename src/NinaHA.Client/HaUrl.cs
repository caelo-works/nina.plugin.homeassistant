using System;

namespace NinaHA.Client {

    /// <summary>
    /// Normalizes a user-entered Home Assistant base URL so small input quirks don't cause connection
    /// failures: trims whitespace, defaults to <c>http://</c> when no scheme is given, and removes any
    /// trailing slash. Empty input returns empty.
    /// </summary>
    public static class HaUrl {

        public static string Normalize(string? baseUrl) {
            var s = (baseUrl ?? string.Empty).Trim();
            if (s.Length == 0) {
                return string.Empty;
            }
            if (!s.Contains("://", StringComparison.Ordinal)) {
                s = "http://" + s;
            }
            return s.TrimEnd('/');
        }
    }
}
