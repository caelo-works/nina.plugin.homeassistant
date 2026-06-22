using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Utility;
using NinaHA.Client;
using NinaHA.Client.Configuration;

namespace NinaHA.Plugin {

    /// <summary>
    /// Process-wide cache of Home Assistant entity ids and <c>domain.service</c> identifiers, used to
    /// populate the searchable pickers in the options page and the sequencer items. Loaded lazily from
    /// the saved configuration and refreshable on demand. The lists are swapped wholesale on refresh so
    /// bindings see a single change notification rather than thousands.
    /// </summary>
    public sealed class HaCatalog : BaseINPC {

        public static HaCatalog Instance { get; } = new HaCatalog();

        private bool loadedOnce;
        private int loading;
        private IReadOnlyList<string> entities = Array.Empty<string>();
        private IReadOnlyList<string> services = Array.Empty<string>();

        public IReadOnlyList<string> Entities {
            get => entities;
            private set { entities = value; RaisePropertyChanged(); }
        }

        public IReadOnlyList<string> Services {
            get => services;
            private set { services = value; RaisePropertyChanged(); }
        }

        /// <summary>Loads the catalog the first time it is needed; subsequent calls are no-ops.</summary>
        public Task EnsureLoadedAsync(HomeAssistantConfig config) =>
            loadedOnce ? Task.CompletedTask : RefreshAsync(config);

        /// <summary>Reloads entities and services from Home Assistant. Concurrent calls collapse to one.</summary>
        public async Task RefreshAsync(HomeAssistantConfig config) {
            if (!config.HasConnection) {
                return;
            }
            if (Interlocked.Exchange(ref loading, 1) == 1) {
                return;
            }
            try {
                using var rest = new HomeAssistantRestClient(config.BaseUrl, config.Token);
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var statesTask = rest.GetStatesAsync(cts.Token);
                var servicesTask = rest.GetServicesAsync(cts.Token);
                await Task.WhenAll(statesTask, servicesTask).ConfigureAwait(false);
                var states = await statesTask.ConfigureAwait(false);
                var serviceList = await servicesTask.ConfigureAwait(false);

                Entities = states.Select(s => s.EntityId)
                    .OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
                Services = serviceList.ToList();
                loadedOnce = true;
            } catch (Exception ex) {
                Logger.Error("Home Assistant: failed to load entity/service catalog.", ex);
            } finally {
                Interlocked.Exchange(ref loading, 0);
            }
        }
    }
}
