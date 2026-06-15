using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Profile.Interfaces;
using NinaHA.Client;
using NinaHA.Client.Configuration;
using NinaHA.Client.Dto;
using NinaHA.Plugin.Mvvm;

namespace NinaHA.Plugin {

    /// <summary>
    /// Plugin manifest and options view-model. An instance of this class is the data context of the
    /// plugin options page (DataTemplate key "Home Assistant Switch_Options").
    /// </summary>
    [Export(typeof(IPluginManifest))]
    public class HomeAssistantSwitchPlugin : PluginBase, INotifyPropertyChanged {

        private readonly HaSettingsStore settingsStore;

        private string baseUrl = string.Empty;
        private string token = string.Empty;
        private bool useWebSocket = true;
        private string statusMessage = string.Empty;
        private bool isBusy;

        [ImportingConstructor]
        public HomeAssistantSwitchPlugin(IProfileService profileService) {
            settingsStore = new HaSettingsStore(profileService);

            var config = settingsStore.Load();
            baseUrl = config.BaseUrl;
            token = config.Token;
            useWebSocket = config.UseWebSocket;
            Channels = new ObservableCollection<SwitchChannel>(config.Channels);

            TestConnectionCommand = new AsyncDelegateCommand(TestConnectionAsync);
            AddChannelCommand = new DelegateCommand(_ => AddChannel());
            RemoveChannelCommand = new DelegateCommand(p => RemoveChannel(p as SwitchChannel));
            SaveCommand = new DelegateCommand(_ => SaveConfig());
        }

        public string BaseUrl {
            get => baseUrl;
            set { baseUrl = value; RaisePropertyChanged(); SaveConfig(); }
        }

        public string Token {
            get => token;
            set { token = value; RaisePropertyChanged(); SaveConfig(); }
        }

        public bool UseWebSocket {
            get => useWebSocket;
            set { useWebSocket = value; RaisePropertyChanged(); SaveConfig(); }
        }

        public string StatusMessage {
            get => statusMessage;
            private set { statusMessage = value; RaisePropertyChanged(); }
        }

        public bool IsBusy {
            get => isBusy;
            private set { isBusy = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<SwitchChannel> Channels { get; }

        public ObservableCollection<HaState> AvailableEntities { get; } = new ObservableCollection<HaState>();

        public IReadOnlyList<ChannelType> ChannelTypes { get; } = (ChannelType[])Enum.GetValues(typeof(ChannelType));

        public IReadOnlyList<ChannelDirection> ChannelDirections { get; } = (ChannelDirection[])Enum.GetValues(typeof(ChannelDirection));

        public ICommand TestConnectionCommand { get; }

        public ICommand AddChannelCommand { get; }

        public ICommand RemoveChannelCommand { get; }

        public ICommand SaveCommand { get; }

        private async Task TestConnectionAsync() {
            IsBusy = true;
            StatusMessage = "Connecting...";
            try {
                using var rest = new HomeAssistantRestClient(BaseUrl, Token);
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

                if (!await rest.PingAsync(cts.Token).ConfigureAwait(true)) {
                    StatusMessage = "Connection failed. Check the URL and token.";
                    return;
                }

                var states = await rest.GetStatesAsync(cts.Token).ConfigureAwait(true);
                AvailableEntities.Clear();
                foreach (var s in states.OrderBy(s => s.EntityId, StringComparer.OrdinalIgnoreCase)) {
                    AvailableEntities.Add(s);
                }
                StatusMessage = $"Connected. {AvailableEntities.Count} entities found.";
            } catch (Exception ex) {
                StatusMessage = "Connection error: " + ex.Message;
            } finally {
                IsBusy = false;
            }
        }

        private void AddChannel() {
            Channels.Add(new SwitchChannel { Name = "New channel" });
            SaveConfig();
        }

        private void RemoveChannel(SwitchChannel? channel) {
            if (channel != null && Channels.Remove(channel)) {
                SaveConfig();
            }
        }

        private void SaveConfig() {
            settingsStore.Save(new HomeAssistantConfig {
                BaseUrl = baseUrl,
                Token = token,
                UseWebSocket = useWebSocket,
                Channels = Channels.ToList()
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
