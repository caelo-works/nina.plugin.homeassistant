using System;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NinaHA.Client;
using NinaHA.Client.Dto;

namespace NinaHA.Plugin.SequenceItems {

    /// <summary>
    /// Helpers shared by the Home Assistant sequencer items, so the connection check and live-value
    /// read are written once rather than copied into every item.
    /// </summary>
    internal static class HaSequenceSupport {

        /// <summary>Placeholder shown in the UI when no live value is available.</summary>
        public const string NoValue = "—";

        /// <summary>Validation message used when the plugin has no Home Assistant connection configured.</summary>
        public const string NotConfiguredMessage = "Home Assistant is not configured (Options > Plugins > Home Assistant).";

        /// <summary>True when a base URL and token are configured.</summary>
        public static bool IsConfigured(IProfileService profileService) =>
            new HaSettingsStore(profileService).Load().HasConnection;

        /// <summary>
        /// Reads an entity's current state over REST for the live preview, returning <see cref="NoValue"/>
        /// when unconfigured, when no entity is selected, or on any failure.
        /// </summary>
        public static async Task<string> ReadCurrentValueAsync(IProfileService profileService, string entityId) {
            try {
                var config = new HaSettingsStore(profileService).Load();
                if (!config.HasConnection || string.IsNullOrWhiteSpace(entityId)) {
                    return NoValue;
                }
                using var rest = new HomeAssistantRestClient(config.BaseUrl, config.Token);
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                HaState? state = await rest.GetStateAsync(entityId, cts.Token);
                return state?.State ?? NoValue;
            } catch (Exception ex) {
                Logger.Error($"Home Assistant: failed to read '{entityId}'", ex);
                return NoValue;
            }
        }
    }
}
