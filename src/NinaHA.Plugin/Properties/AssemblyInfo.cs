using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("Home Assistant")]
[assembly: AssemblyDescription("Expose Home Assistant entities as NINA switch channels and drive/read them from advanced sequencer instructions.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("CaeloWorks")]
[assembly: AssemblyProduct("Home Assistant")]
[assembly: AssemblyCopyright("Copyright © 2026")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]

// Unique, permanent plugin identifier (also serves as the settings store key).
[assembly: Guid(NinaHA.Plugin.PluginConstants.PluginGuid)]

[assembly: AssemblyVersion("1.2.1.0")]
[assembly: AssemblyFileVersion("1.2.1.0")]

// --- NINA plugin manifest metadata (surfaced in the NINA plugin manager) ---
[assembly: AssemblyMetadata("MinimumApplicationVersion", "3.2.0.0")]
[assembly: AssemblyMetadata("License", "MPL-2.0")]
[assembly: AssemblyMetadata("LicenseURL", "https://www.mozilla.org/en-US/MPL/2.0/")]
[assembly: AssemblyMetadata("Repository", "https://github.com/caelo-works/nina.plugin.homeassistant")]
[assembly: AssemblyMetadata("Homepage", "https://github.com/caelo-works/nina.plugin.homeassistant")]
[assembly: AssemblyMetadata("ChangelogURL", "https://github.com/caelo-works/nina.plugin.homeassistant/releases")]
// Plugin-manager list/description icon. A pack:// URI to the embedded resource renders fine for an
// installed plugin (confirmed on N.I.N.A. 3.2), so no externally hosted image is required.
// Note: if this plugin is ever published to the public N.I.N.A. plugin catalog, that catalog needs a
// real https URL instead (e.g. a public release asset:
// https://github.com/caelo-works/nina.plugin.homeassistant/releases/latest/download/logo.png).
[assembly: AssemblyMetadata("FeaturedImageURL", "pack://application:,,,/NinaHA.Plugin;component/Resources/logo.png")]
[assembly: AssemblyMetadata("Tags", "Home Assistant,Switch,Automation,Equipment,Sequencer")]
[assembly: AssemblyMetadata("ShortDescription", "Bridge Home Assistant into NINA: expose entities as switch channels and drive/read them from advanced sequencer instructions.")]
[assembly: AssemblyMetadata("LongDescription",
    "Connect a Home Assistant instance and integrate it into NINA two ways.\r\n\r\n" +
    "Switch equipment: map any HA entity to a channel of a NINA Switch device. Each channel can be " +
    "read-only or writable and represents a binary (on/off), stepped (discrete, e.g. select) or analog " +
    "(numeric) value, with the unit of measurement shown in the channel name. Once configured, the hub " +
    "behaves like any native switch and is usable by all built-in functions and other plugins. Live " +
    "updates use the Home Assistant WebSocket API with REST polling as a fallback.\r\n\r\n" +
    "Advanced sequencer (category 'Home Assistant'):\r\n" +
    "- Call HA Service: invoke any domain.service with an optional entity and JSON data. The data " +
    "supports NINA patterns ($$TARGETNAME$$, $$FILTER$$, $$CAMERA$$, $$GAIN$$, $$OFFSET$$, " +
    "$$TEMPERATURE$$, $$SQM$$, $$DATE$$, $$TIME$$, $$DATETIME$$).\r\n" +
    "- Wait for HA State: pause until an entity reaches a state or threshold (with timeout), showing " +
    "the live value.\r\n" +
    "- HA State condition: loop while an entity satisfies a comparison; the live value is displayed and " +
    "the surrounding block is interrupted when it no longer holds.\r\n" +
    "- Publish to HA: every N exposures, call a HA service to push NINA status (target, filter, frame " +
    "count, sensor temperature...) for a Home Assistant dashboard.\r\n\r\n" +
    "Entity and service pickers throughout the UI offer searchable autocompletion.")]
