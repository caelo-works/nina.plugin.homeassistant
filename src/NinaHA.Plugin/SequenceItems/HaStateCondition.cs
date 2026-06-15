using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using Newtonsoft.Json;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using NinaHA.Client;
using NinaHA.Client.Dto;

namespace NinaHA.Plugin.SequenceItems {

    /// <summary>
    /// A loop condition that stays true while a Home Assistant entity satisfies a comparison. When it
    /// becomes false, the surrounding loop stops.
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
        private IList<string> issues = new List<string>();

        [ImportingConstructor]
        public HaStateCondition(IProfileService profileService) {
            this.profileService = profileService;
            _ = HaCatalog.Instance.EnsureLoadedAsync(new HaSettingsStore(profileService).Load());
        }

        [JsonProperty]
        public string EntityId { get => entityId; set { entityId = value; RaisePropertyChanged(); } }

        [JsonProperty]
        public ComparisonOperator Comparison { get => comparison; set { comparison = value; RaisePropertyChanged(); } }

        [JsonProperty]
        public string Value { get => value; set { this.value = value; RaisePropertyChanged(); } }

        public IList<string> Issues { get => issues; set { issues = value; RaisePropertyChanged(); } }

        public ComparisonOperator[] Operators { get; } = (ComparisonOperator[])Enum.GetValues(typeof(ComparisonOperator));

        public HaCatalog Catalog => HaCatalog.Instance;

        public override bool Check(ISequenceItem previousItem, ISequenceItem nextItem) {
            try {
                var config = new HaSettingsStore(profileService).Load();
                if (!config.HasConnection) {
                    return false;
                }
                using var rest = new HomeAssistantRestClient(config.BaseUrl, config.Token);
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                HaState? state = rest.GetStateAsync(EntityId, cts.Token).GetAwaiter().GetResult();
                return state != null && StateComparer.Matches(state.State, Value, Comparison);
            } catch (Exception ex) {
                Logger.Error($"Home Assistant: condition check failed for '{EntityId}'", ex);
                return false;
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
