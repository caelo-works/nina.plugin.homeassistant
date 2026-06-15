using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NinaHA.Client;
using NinaHA.Client.Configuration;
using NinaHA.Client.Dto;

namespace NinaHA.Plugin.Equipment {

    /// <summary>
    /// A NINA Switch hub that exposes configured Home Assistant entities as switch channels.
    /// </summary>
    public sealed class HomeAssistantSwitchHub : BaseINPC, ISwitchHub {

        private readonly HaSettingsStore settingsStore;

        private HomeAssistantRestClient? rest;
        private HomeAssistantWebSocketClient? webSocket;
        private EntityStateStore store = new EntityStateStore();
        private bool connected;

        public HomeAssistantSwitchHub(HaSettingsStore settingsStore) {
            this.settingsStore = settingsStore;
        }

        public ICollection<ISwitch> Switches { get; } = new AsyncObservableCollection<ISwitch>();

        public bool HasSetupDialog => false;

        public string Id => PluginConstants.PluginGuid + ":hub";

        public string Name => PluginConstants.PluginName;

        public string DisplayName => PluginConstants.PluginName;

        public string Category => "Home Assistant";

        public bool Connected {
            get => connected;
            private set { connected = value; RaisePropertyChanged(); }
        }

        public string Description => "Bridges Home Assistant entities to a NINA switch device.";

        public string DriverInfo => "Home Assistant REST/WebSocket bridge";

        public string DriverVersion => "1.0.0";

        public IList<string> SupportedActions => Array.Empty<string>();

        public async Task<bool> Connect(CancellationToken token) {
            var config = settingsStore.Load();
            if (!config.HasConnection) {
                Logger.Warning("Home Assistant: no URL/token configured. Configure the plugin in Options > Plugins.");
                return false;
            }

            try {
                store = new EntityStateStore();
                rest = new HomeAssistantRestClient(config.BaseUrl, config.Token);

                if (!await rest.PingAsync(token).ConfigureAwait(false)) {
                    Logger.Error("Home Assistant: connection test failed (check URL and token).");
                    Cleanup();
                    return false;
                }

                var snapshot = await rest.GetStatesAsync(token).ConfigureAwait(false);
                store.Seed(snapshot);

                webSocket = null;
                var context = new HaSwitchContext(rest, store, () => webSocket?.IsConnected ?? false);
                BuildSwitches(config, context);

                store.StateChanged += OnEntityStateChanged;

                if (config.UseWebSocket) {
                    webSocket = new HomeAssistantWebSocketClient(config.BaseUrl, config.Token);
                    webSocket.StateChanged += state => store.Set(state);
                    webSocket.Start();
                }

                Connected = true;
                Logger.Info($"Home Assistant: connected with {Switches.Count} channel(s).");
                return true;
            } catch (Exception ex) {
                Logger.Error("Home Assistant: connection failed.", ex);
                Cleanup();
                return false;
            }
        }

        private void BuildSwitches(HomeAssistantConfig config, HaSwitchContext context) {
            Switches.Clear();
            short id = 0;
            foreach (var channel in config.Channels) {
                if (string.IsNullOrWhiteSpace(channel.EntityId)) {
                    continue;
                }
                ISwitch sw = channel.IsWritable
                    ? new HaWritableSwitch(id, channel, context)
                    : new HaSwitch(id, channel, context);
                Switches.Add(sw);
                id++;
            }
        }

        private void OnEntityStateChanged(HaState state) {
            foreach (var sw in Switches.OfType<HaSwitch>()) {
                if (string.Equals(sw.EntityId, state.EntityId, StringComparison.OrdinalIgnoreCase)) {
                    sw.NotifyValueChanged();
                }
            }
        }

        public void Disconnect() {
            Cleanup();
            Connected = false;
            Logger.Info("Home Assistant: disconnected.");
        }

        private void Cleanup() {
            store.StateChanged -= OnEntityStateChanged;
            webSocket?.Dispose();
            webSocket = null;
            rest?.Dispose();
            rest = null;
            Switches.Clear();
            store.Clear();
        }

        public void SetupDialog() { }

        public string Action(string actionName, string actionParameters) => string.Empty;

        public string SendCommandString(string command, bool raw = true) => string.Empty;

        public bool SendCommandBool(string command, bool raw = true) => false;

        public void SendCommandBlind(string command, bool raw = true) { }
    }
}
