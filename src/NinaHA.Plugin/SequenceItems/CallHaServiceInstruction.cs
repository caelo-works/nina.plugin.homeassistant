using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Profile.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using NinaHA.Client;

namespace NinaHA.Plugin.SequenceItems {

    /// <summary>
    /// Calls an arbitrary Home Assistant service (e.g. <c>scene.turn_on</c>, <c>notify.mobile_app</c>,
    /// <c>switch.toggle</c>) with an optional entity id and JSON payload.
    /// </summary>
    [ExportMetadata("Name", "Call HA Service")]
    [ExportMetadata("Description", "Call an arbitrary Home Assistant service with an optional entity id and JSON data.")]
    [ExportMetadata("Icon", "HomeAssistant_SVG")]
    [ExportMetadata("Category", "Home Assistant")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class CallHaServiceInstruction : SequenceItem, IValidatable {

        private readonly IProfileService profileService;
        private string domain = string.Empty;
        private string service = string.Empty;
        private string entityId = string.Empty;
        private string data = string.Empty;
        private IList<string> issues = new List<string>();

        [ImportingConstructor]
        public CallHaServiceInstruction(IProfileService profileService) {
            this.profileService = profileService;
        }

        private CallHaServiceInstruction(CallHaServiceInstruction copyMe) : this(copyMe.profileService) {
            CopyMetaData(copyMe);
            Domain = copyMe.Domain;
            Service = copyMe.Service;
            EntityId = copyMe.EntityId;
            Data = copyMe.Data;
        }

        [JsonProperty]
        public string Domain { get => domain; set { domain = value; RaisePropertyChanged(); } }

        [JsonProperty]
        public string Service { get => service; set { service = value; RaisePropertyChanged(); } }

        [JsonProperty]
        public string EntityId { get => entityId; set { entityId = value; RaisePropertyChanged(); } }

        [JsonProperty]
        public string Data { get => data; set { data = value; RaisePropertyChanged(); } }

        public IList<string> Issues { get => issues; set { issues = value; RaisePropertyChanged(); } }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            var config = new HaSettingsStore(profileService).Load();
            using var rest = new HomeAssistantRestClient(config.BaseUrl, config.Token);
            var payload = HaServiceData.Parse(Data);
            if (!string.IsNullOrWhiteSpace(EntityId)) {
                payload["entity_id"] = EntityId;
            }
            await rest.CallServiceAsync(Domain.Trim(), Service.Trim(), payload, token);
        }

        public bool Validate() {
            var found = new List<string>();
            if (!new HaSettingsStore(profileService).Load().HasConnection) {
                found.Add("Home Assistant is not configured (Options > Plugins > Home Assistant).");
            }
            if (string.IsNullOrWhiteSpace(Domain)) {
                found.Add("Service domain is required.");
            }
            if (string.IsNullOrWhiteSpace(Service)) {
                found.Add("Service name is required.");
            }
            if (!HaServiceData.TryParse(Data, out _)) {
                found.Add("Data must be a valid JSON object.");
            }
            Issues = found;
            return found.Count == 0;
        }

        public override object Clone() => new CallHaServiceInstruction(this);

        public override string ToString() => $"Category: {Category}, Item: {nameof(CallHaServiceInstruction)}, Service: {Domain}.{Service}";
    }
}
