using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NINA.Core.Enum;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Utility;
using NINA.Sequencer.Validations;
using NinaHA.Client;
using NinaHA.Client.Dto;

namespace NinaHA.Plugin.SequenceItems {

    /// <summary>
    /// A loop condition that stays true while a Home Assistant entity satisfies a comparison. The current
    /// value is polled in the background (live preview) and the surrounding loop is interrupted when the
    /// condition becomes false.
    /// </summary>
    [ExportMetadata("Name", "HA State")]
    [ExportMetadata("Description", "Run while a Home Assistant entity satisfies a comparison.")]
    [ExportMetadata("Icon", "HomeAssistant_SVG")]
    [ExportMetadata("Category", "Home Assistant")]
    [Export(typeof(ISequenceCondition))]
    [JsonObject(MemberSerialization.OptIn)]
    public class HaStateCondition : SequenceCondition, IValidatable {

        private readonly IProfileService profileService;
        private string entityId = string.Empty;
        private ComparisonOperator comparison = ComparisonOperator.Equals;
        private string value = string.Empty;
        private string currentValue = "—";
        private IList<string> issues = new List<string>();

        [ImportingConstructor]
        public HaStateCondition(IProfileService profileService) {
            this.profileService = profileService;
            _ = HaCatalog.Instance.EnsureLoadedAsync(new HaSettingsStore(profileService).Load());
            ConditionWatchdog = new ConditionWatchdog(MonitorAsync, TimeSpan.FromSeconds(5));
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

        /// <summary>Latest known value of the entity, shown live in the UI.</summary>
        public string CurrentValue { get => currentValue; private set { currentValue = value; RaisePropertyChanged(); } }

        public IList<string> Issues { get => issues; set { issues = value; RaisePropertyChanged(); } }

        public ComparisonOperator[] Operators { get; } = (ComparisonOperator[])Enum.GetValues(typeof(ComparisonOperator));

        public HaCatalog Catalog => HaCatalog.Instance;

        public override bool Check(ISequenceItem previousItem, ISequenceItem nextItem) {
            return StateComparer.Matches(CurrentValue, Value, Comparison);
        }

        private async Task MonitorAsync() {
            await RefreshCurrentValueAsync();
            if (!Check(null, null)
                && Parent != null
                && ItemUtility.IsInRootContainer(Parent)
                && Parent.Status == SequenceEntityStatus.RUNNING
                && Status != SequenceEntityStatus.DISABLED) {
                Logger.Info($"Home Assistant: '{EntityId}' no longer {Comparison} {Value} - interrupting parent.");
                await Parent.Interrupt();
            }
        }

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

        public override void AfterParentChanged() {
            Validate();
            RunWatchdogIfInsideSequenceRoot();
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context) {
            RunWatchdogIfInsideSequenceRoot();
        }

        public bool Validate() {
            var found = new List<string>();
            if (!new HaSettingsStore(profileService).Load().HasConnection) {
                found.Add("Home Assistant is not configured (Options > Plugins > Home Assistant).");
            }
            if (string.IsNullOrWhiteSpace(EntityId)) {
                found.Add("Entity id is required.");
            }
            Issues = found;
            return found.Count == 0;
        }

        public override object Clone() => new HaStateCondition(profileService) {
            Icon = Icon,
            Name = Name,
            Category = Category,
            Description = Description,
            EntityId = EntityId,
            Comparison = Comparison,
            Value = Value
        };

        public override string ToString() => $"Category: {Category}, Condition: {nameof(HaStateCondition)}, Entity: {EntityId} {Comparison} {Value}";
    }
}
