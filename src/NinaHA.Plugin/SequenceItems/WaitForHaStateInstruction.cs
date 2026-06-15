using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using NinaHA.Client;
using NinaHA.Client.Dto;

namespace NinaHA.Plugin.SequenceItems {

    /// <summary>
    /// Blocks the sequence until a Home Assistant entity's state satisfies a comparison, or a timeout
    /// elapses (0 = wait indefinitely).
    /// </summary>
    [ExportMetadata("Name", "Wait for HA State")]
    [ExportMetadata("Description", "Wait until a Home Assistant entity reaches a given state or threshold.")]
    [ExportMetadata("Icon", "HomeAssistant_SVG")]
    [ExportMetadata("Category", "Home Assistant")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class WaitForHaStateInstruction : SequenceItem, IValidatable {

        private readonly IProfileService profileService;
        private string entityId = string.Empty;
        private ComparisonOperator comparison = ComparisonOperator.Equals;
        private string value = string.Empty;
        private int timeoutSeconds = 300;
        private int pollIntervalSeconds = 5;
        private string currentValue = "—";
        private IList<string> issues = new List<string>();

        [ImportingConstructor]
        public WaitForHaStateInstruction(IProfileService profileService) {
            this.profileService = profileService;
            _ = HaCatalog.Instance.EnsureLoadedAsync(new HaSettingsStore(profileService).Load());
        }

        private WaitForHaStateInstruction(WaitForHaStateInstruction copyMe) : this(copyMe.profileService) {
            CopyMetaData(copyMe);
            EntityId = copyMe.EntityId;
            Comparison = copyMe.Comparison;
            Value = copyMe.Value;
            TimeoutSeconds = copyMe.TimeoutSeconds;
            PollIntervalSeconds = copyMe.PollIntervalSeconds;
        }

        [JsonProperty]
        public string EntityId {
            get => entityId;
            set { entityId = value; RaisePropertyChanged(); _ = RefreshCurrentValueAsync(); }
        }

        [JsonProperty]
        public ComparisonOperator Comparison { get => comparison; set { comparison = value; RaisePropertyChanged(); } }

        [JsonProperty]
        public string Value { get => value; set { this.value = value; RaisePropertyChanged(); } }

        [JsonProperty]
        public int TimeoutSeconds { get => timeoutSeconds; set { timeoutSeconds = value; RaisePropertyChanged(); } }

        [JsonProperty]
        public int PollIntervalSeconds { get => pollIntervalSeconds; set { pollIntervalSeconds = value; RaisePropertyChanged(); } }

        public IList<string> Issues { get => issues; set { issues = value; RaisePropertyChanged(); } }

        public ComparisonOperator[] Operators { get; } = (ComparisonOperator[])Enum.GetValues(typeof(ComparisonOperator));

        public HaCatalog Catalog => HaCatalog.Instance;

        /// <summary>Latest known value of the entity, shown live in the UI.</summary>
        public string CurrentValue { get => currentValue; private set { currentValue = value; RaisePropertyChanged(); } }

        private async Task RefreshCurrentValueAsync() {
            try {
                var config = new HaSettingsStore(profileService).Load();
                if (!config.HasConnection || string.IsNullOrWhiteSpace(EntityId)) {
                    CurrentValue = "—";
                    return;
                }
                using var rest = new HomeAssistantRestClient(config.BaseUrl, config.Token);
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                HaState? state = await rest.GetStateAsync(EntityId, cts.Token);
                CurrentValue = state?.State ?? "—";
            } catch (Exception ex) {
                Logger.Error($"Home Assistant: failed to read '{EntityId}'", ex);
                CurrentValue = "—";
            }
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            var config = new HaSettingsStore(profileService).Load();
            using var rest = new HomeAssistantRestClient(config.BaseUrl, config.Token);

            var start = DateTime.UtcNow;
            var interval = TimeSpan.FromSeconds(Math.Max(1, PollIntervalSeconds));

            while (true) {
                token.ThrowIfCancellationRequested();
                HaState? state = await rest.GetStateAsync(EntityId, token);
                CurrentValue = state?.State ?? "—";
                if (state != null && StateComparer.Matches(state.State, Value, Comparison)) {
                    return;
                }
                if (TimeoutSeconds > 0 && (DateTime.UtcNow - start).TotalSeconds >= TimeoutSeconds) {
                    throw new SequenceEntityFailedException($"Timed out after {TimeoutSeconds}s waiting for {EntityId} {Comparison} {Value}.");
                }
                progress?.Report(new ApplicationStatus { Status = $"Waiting for {EntityId} {Comparison} {Value}" });
                await Task.Delay(interval, token);
            }
        }

        public bool Validate() {
            var found = new List<string>();
            if (!new HaSettingsStore(profileService).Load().HasConnection) {
                found.Add("Home Assistant is not configured (Options > Plugins > Home Assistant).");
            }
            if (string.IsNullOrWhiteSpace(EntityId)) {
                found.Add("Entity id is required.");
            }
            if (PollIntervalSeconds < 1) {
                found.Add("Poll interval must be at least 1 second.");
            }
            Issues = found;
            return found.Count == 0;
        }

        public override object Clone() => new WaitForHaStateInstruction(this);

        public override string ToString() => $"Category: {Category}, Item: {nameof(WaitForHaStateInstruction)}, Entity: {EntityId} {Comparison} {Value}";
    }
}
