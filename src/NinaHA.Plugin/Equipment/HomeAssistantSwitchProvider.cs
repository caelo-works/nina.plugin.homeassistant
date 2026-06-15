using System.Collections.Generic;
using System.ComponentModel.Composition;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Profile.Interfaces;

namespace NinaHA.Plugin.Equipment {

    /// <summary>
    /// Advertises the Home Assistant switch hub to NINA. When NINA scans for switch equipment it calls
    /// <see cref="GetEquipment"/>, which is how the hub appears in the Switch device chooser.
    /// </summary>
    [Export(typeof(IEquipmentProvider))]
    public sealed class HomeAssistantSwitchProvider : IEquipmentProvider<ISwitchHub> {

        private readonly IProfileService profileService;

        [ImportingConstructor]
        public HomeAssistantSwitchProvider(IProfileService profileService) {
            this.profileService = profileService;
        }

        public string Name => PluginConstants.PluginName;

        public IList<ISwitchHub> GetEquipment() {
            return new List<ISwitchHub> {
                new HomeAssistantSwitchHub(new HaSettingsStore(profileService))
            };
        }
    }
}
