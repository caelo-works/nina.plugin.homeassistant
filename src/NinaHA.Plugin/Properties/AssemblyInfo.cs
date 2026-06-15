using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("Home Assistant")]
[assembly: AssemblyDescription("Expose Home Assistant entities as NINA switch channels.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("CaeloWorks")]
[assembly: AssemblyProduct("Home Assistant")]
[assembly: AssemblyCopyright("Copyright © 2026")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]

// Unique, permanent plugin identifier (also serves as the settings store key).
[assembly: Guid(NinaHA.Plugin.PluginConstants.PluginGuid)]

[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

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
[assembly: AssemblyMetadata("Tags", "Home Assistant,Switch,Automation,Equipment")]
[assembly: AssemblyMetadata("ShortDescription", "Bridge Home Assistant entities into NINA as switch channels (read/write, binary/stepped/analog).")]
[assembly: AssemblyMetadata("LongDescription",
    "Connect a Home Assistant instance and map its entities to channels of a NINA Switch device. " +
    "Each channel can be read-only or writable and represents a binary (on/off), stepped (discrete) " +
    "or analog (numeric) value. Once configured, the hub behaves like any native switch and is usable " +
    "by all built-in functions and other plugins. Live updates use the Home Assistant WebSocket API " +
    "with REST polling as a fallback.")]
