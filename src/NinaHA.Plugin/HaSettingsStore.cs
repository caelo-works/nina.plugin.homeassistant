using System;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NinaHA.Client.Configuration;

namespace NinaHA.Plugin {

    /// <summary>
    /// Persists the <see cref="HomeAssistantConfig"/> in the NINA per-profile plugin settings store,
    /// keyed by the plugin GUID. Both the options page and the equipment provider/hub use this so they
    /// always share the same configuration.
    /// </summary>
    public sealed class HaSettingsStore {

        private const string ConfigKey = "Configuration";

        private readonly IPluginOptionsAccessor accessor;

        public HaSettingsStore(IProfileService profileService) {
            accessor = new PluginOptionsAccessor(profileService, Guid.Parse(PluginConstants.PluginGuid));
        }

        public HomeAssistantConfig Load() {
            var json = accessor.GetValueString(ConfigKey, string.Empty);
            return HomeAssistantConfig.Deserialize(json);
        }

        public void Save(HomeAssistantConfig config) {
            accessor.SetValueString(ConfigKey, config.Serialize());
        }
    }
}
