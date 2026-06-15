using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using NINA.Core.Utility;
using NinaHA.Client;
using NinaHA.Client.Configuration;

namespace NinaHA.Plugin {

    /// <summary>
    /// Process-wide cache of Home Assistant entity ids and <c>domain.service</c> identifiers, used to
    /// populate the searchable pickers in the options page and the sequencer items. Loaded lazily from
    /// the saved configuration and refreshable on demand.
    /// </summary>
    public sealed class HaCatalog {

        public static HaCatalog Instance { get; } = new HaCatalog();

        private bool loadedOnce;
        private int loading;

        public ObservableCollection<string> Entities { get; } = new ObservableCollection<string>();

        public ObservableCollection<string> Services { get; } = new ObservableCollection<string>();

        /// <summary>Loads the catalog the first time it is needed; subsequent calls are no-ops.</summary>
        public Task EnsureLoadedAsync(HomeAssistantConfig config) =>
            loadedOnce ? Task.CompletedTask : RefreshAsync(config);

        /// <summary>Reloads entities and services from Home Assistant. Safe to call concurrently.</summary>
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
                var states = await rest.GetStatesAsync(cts.Token).ConfigureAwait(false);
                var services = await rest.GetServicesAsync(cts.Token).ConfigureAwait(false);

                var entityIds = states.Select(s => s.EntityId)
                    .OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();

                Replace(Entities, entityIds);
                Replace(Services, services);
                loadedOnce = true;
            } catch (Exception ex) {
                Logger.Error("Home Assistant: failed to load entity/service catalog.", ex);
            } finally {
                Interlocked.Exchange(ref loading, 0);
            }
        }

        private static void Replace(ObservableCollection<string> target, System.Collections.Generic.IEnumerable<string> items) {
            void Apply() {
                target.Clear();
                foreach (var item in items) {
                    target.Add(item);
                }
            }
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess()) {
                dispatcher.Invoke(Apply);
            } else {
                Apply();
            }
        }
    }
}
