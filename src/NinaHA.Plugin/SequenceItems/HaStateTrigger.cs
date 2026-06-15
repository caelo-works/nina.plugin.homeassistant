using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Validations;
using NinaHA.Client;
using NinaHA.Client.Dto;

namespace NinaHA.Plugin.SequenceItems {

    /// <summary>
    /// Fires a Home Assistant service call when an entity transitions into a state that satisfies a
    /// comparison (rising edge, so the action runs once per transition rather than on every check).
    /// </summary>
    [ExportMetadata("Name", "On HA State")]
    [ExportMetadata("Description", "When a Home Assistant entity meets a comparison, call a Home Assistant service.")]
    [ExportMetadata("Icon", "HomeAssistant_SVG")]
    [ExportMetadata("Category", "Home Assistant")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    public class HaStateTrigger : SequenceTrigger, IValidatable {

        private readonly IProfileService profileService;
        private string entityId = string.Empty;
        private ComparisonOperator comparison = ComparisonOperator.Equals;
        private string value = string.Empty;
        private string actionDomain = string.Empty;
        private string actionService = string.Empty;
        private string actionEntityId = string.Empty;
        private string actionData = string.Empty;
        private IList<string> issues = new List<string>();
        private bool wasTrue;

        [ImportingConstructor]
        public HaStateTrigger(IProfileService profileService) {
            this.profileService = profileService;
            _ = HaCatalog.Instance.EnsureLoadedAsync(new HaSettingsStore(profileService).Load());
        }

        private HaStateTrigger(HaStateTrigger copyMe) : this(copyMe.profileService) {
            CopyMetaData(copyMe);
            EntityId = copyMe.EntityId;
            Comparison = copyMe.Comparison;
            Value = copyMe.Value;
            ActionDomain = copyMe.ActionDomain;
            ActionService = copyMe.ActionService;
            ActionEntityId = copyMe.ActionEntityId;
            ActionData = copyMe.ActionData;
        }

        [JsonProperty]
        public string EntityId { get => entityId; set { entityId = value; RaisePropertyChanged(); } }

        [JsonProperty]
        public ComparisonOperator Comparison { get => comparison; set { comparison = value; RaisePropertyChanged(); } }

        [JsonProperty]
        public string Value { get => value; set { this.value = value; RaisePropertyChanged(); } }

        [JsonProperty]
        public string ActionDomain { get => actionDomain; set { actionDomain = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(ActionServiceId)); } }

        [JsonProperty]
        public string ActionService { get => actionService; set { actionService = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(ActionServiceId)); } }

        /// <summary>Combined "domain.service" used by the searchable action-service picker.</summary>
        public string ActionServiceId {
            get => HaServiceId.Combine(ActionDomain, ActionService);
            set { HaServiceId.Split(value, out actionDomain, out actionService); RaisePropertyChanged(); RaisePropertyChanged(nameof(ActionDomain)); RaisePropertyChanged(nameof(ActionService)); }
        }

        public HaCatalog Catalog => HaCatalog.Instance;

        [JsonProperty]
        public string ActionEntityId { get => actionEntityId; set { actionEntityId = value; RaisePropertyChanged(); } }

        [JsonProperty]
        public string ActionData { get => actionData; set { actionData = value; RaisePropertyChanged(); } }

        public IList<string> Issues { get => issues; set { issues = value; RaisePropertyChanged(); } }

        public ComparisonOperator[] Operators { get; } = (ComparisonOperator[])Enum.GetValues(typeof(ComparisonOperator));

        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
            bool current;
            try {
                var config = new HaSettingsStore(profileService).Load();
                if (!config.HasConnection) {
                    return false;
                }
                using var rest = new HomeAssistantRestClient(config.BaseUrl, config.Token);
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                HaState? state = rest.GetStateAsync(EntityId, cts.Token).GetAwaiter().GetResult();
                current = state != null && StateComparer.Matches(state.State, Value, Comparison);
            } catch (Exception ex) {
                Logger.Error($"Home Assistant: trigger check failed for '{EntityId}'", ex);
                return false;
            }

            var fire = current && !wasTrue;
            wasTrue = current;
            return fire;
        }

        public override async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            var config = new HaSettingsStore(profileService).Load();
            using var rest = new HomeAssistantRestClient(config.BaseUrl, config.Token);
            var payload = HaServiceData.Parse(ActionData);
            if (!string.IsNullOrWhiteSpace(ActionEntityId)) {
                payload["entity_id"] = ActionEntityId;
            }
            await rest.CallServiceAsync(ActionDomain.Trim(), ActionService.Trim(), payload, token);
        }

        public bool Validate() {
            var found = new List<string>();
            if (!new HaSettingsStore(profileService).Load().HasConnection) {
                found.Add("Home Assistant is not configured (Options > Plugins > Home Assistant).");
            }
            if (string.IsNullOrWhiteSpace(EntityId)) {
                found.Add("Trigger entity id is required.");
            }
            if (string.IsNullOrWhiteSpace(ActionDomain) || string.IsNullOrWhiteSpace(ActionService)) {
                found.Add("Action service domain and name are required.");
            }
            if (!HaServiceData.TryParse(ActionData, out _)) {
                found.Add("Action data must be a valid JSON object.");
            }
            Issues = found;
            return found.Count == 0;
        }

        public override object Clone() => new HaStateTrigger(this);

        public override string ToString() => $"Category: {Category}, Trigger: {nameof(HaStateTrigger)}, When: {EntityId} {Comparison} {Value} -> {ActionDomain}.{ActionService}";
    }
}
