namespace NinaHA.Plugin {

    /// <summary>Shared identifiers for the plugin.</summary>
    public static class PluginConstants {

        /// <summary>
        /// Stable plugin identifier. Mirrors the assembly <c>[Guid]</c> attribute and is used as the
        /// key for the per-profile settings store. Must never change once published.
        /// </summary>
        public const string PluginGuid = "f2134d4c-5b3c-4382-b5da-7523023855d9";

        /// <summary>Display name of the plugin and the equipment hub.</summary>
        public const string PluginName = "Home Assistant";
    }
}
