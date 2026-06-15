namespace NinaHA.Client {

    /// <summary>Helpers to convert between a combined <c>domain.service</c> string and its parts.</summary>
    public static class HaServiceId {

        public static string Combine(string? domain, string? service) {
            if (string.IsNullOrWhiteSpace(domain)) {
                return string.Empty;
            }
            return string.IsNullOrWhiteSpace(service) ? domain!.Trim() : $"{domain!.Trim()}.{service!.Trim()}";
        }

        public static void Split(string? combined, out string domain, out string service) {
            var value = (combined ?? string.Empty).Trim();
            var idx = value.IndexOf('.');
            if (idx > 0) {
                domain = value.Substring(0, idx);
                service = value.Substring(idx + 1);
            } else {
                domain = value;
                service = string.Empty;
            }
        }
    }
}
