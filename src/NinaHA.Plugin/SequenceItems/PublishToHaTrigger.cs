using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Validations;
using NINA.WPF.Base.Interfaces.ViewModel;
using NinaHA.Client;

namespace NinaHA.Plugin.SequenceItems {

    /// <summary>
    /// Pushes N.I.N.A. status to Home Assistant by calling a service every N exposures. The data payload
    /// supports "$$TOKEN$$" patterns (target, filter, camera, etc.), so it can update an HA entity for a
    /// dashboard (e.g. current target, filter, frame count).
    /// </summary>
    [ExportMetadata("Name", "Publish to HA")]
    [ExportMetadata("Description", "Every N exposures, call a Home Assistant service to publish NINA status (supports $$TOKEN$$ patterns in the data).")]
    [ExportMetadata("Icon", "HomeAssistant_SVG")]
    [ExportMetadata("Category", "Home Assistant")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    public class PublishToHaTrigger : SequenceTrigger, IValidatable {

        private readonly IProfileService profileService;
        private readonly IImageHistoryVM history;
        private readonly ICameraMediator cameraMediator;
        private readonly IFilterWheelMediator filterWheelMediator;
        private readonly IWeatherDataMediator weatherMediator;

        private string domain = string.Empty;
        private string service = string.Empty;
        private string entityId = string.Empty;
        private string data = string.Empty;
        private int afterExposures = 1;
        private IList<string> issues = new List<string>();
        private int lastTriggerId;

        [ImportingConstructor]
        public PublishToHaTrigger(IProfileService profileService, IImageHistoryVM history, ICameraMediator cameraMediator, IFilterWheelMediator filterWheelMediator, IWeatherDataMediator weatherMediator) {
            this.profileService = profileService;
            this.history = history;
            this.cameraMediator = cameraMediator;
            this.filterWheelMediator = filterWheelMediator;
            this.weatherMediator = weatherMediator;
            _ = HaCatalog.Instance.EnsureLoadedAsync(new HaSettingsStore(profileService).Load());
        }

        private PublishToHaTrigger(PublishToHaTrigger copyMe)
            : this(copyMe.profileService, copyMe.history, copyMe.cameraMediator, copyMe.filterWheelMediator, copyMe.weatherMediator) {
            CopyMetaData(copyMe);
            Domain = copyMe.Domain;
            Service = copyMe.Service;
            EntityId = copyMe.EntityId;
            Data = copyMe.Data;
            AfterExposures = copyMe.AfterExposures;
        }

        [JsonProperty]
        public string Domain { get => domain; set { domain = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(ServiceId)); } }

        [JsonProperty]
        public string Service { get => service; set { service = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(ServiceId)); } }

        [JsonProperty]
        public string EntityId { get => entityId; set { entityId = value; RaisePropertyChanged(); } }

        [JsonProperty]
        public string Data { get => data; set { data = value; RaisePropertyChanged(); } }

        [JsonProperty]
        public int AfterExposures { get => afterExposures; set { afterExposures = value; RaisePropertyChanged(); } }

        /// <summary>Combined "domain.service" used by the searchable service picker.</summary>
        public string ServiceId {
            get => HaServiceId.Combine(Domain, Service);
            set { HaServiceId.Split(value, out domain, out service); RaisePropertyChanged(); RaisePropertyChanged(nameof(Domain)); RaisePropertyChanged(nameof(Service)); }
        }

        public HaCatalog Catalog => HaCatalog.Instance;

        public IList<string> Issues { get => issues; set { issues = value; RaisePropertyChanged(); } }

        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
            var count = history.ImageHistory.Count;
            if (lastTriggerId > count) {
                lastTriggerId = 0; // history was cleared
            }
            return AfterExposures > 0 && count > 0 && count > lastTriggerId && count % AfterExposures == 0;
        }

        public override async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            lastTriggerId = history.ImageHistory.Count;
            var config = new HaSettingsStore(profileService).Load();
            using var rest = new HomeAssistantRestClient(config.BaseUrl, config.Token);
            var resolvedData = HaPatternResolver.Resolve(Data, Parent, cameraMediator, filterWheelMediator, weatherMediator);
            var payload = HaServiceData.Parse(resolvedData);
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
            if (string.IsNullOrWhiteSpace(Domain) || string.IsNullOrWhiteSpace(Service)) {
                found.Add("Service domain and name are required.");
            }
            if (!HaServiceData.TryParse(Data, out _)) {
                found.Add("Data must be a valid JSON object.");
            }
            if (AfterExposures < 1) {
                found.Add("'After exposures' must be at least 1.");
            }
            Issues = found;
            return found.Count == 0;
        }

        public override object Clone() => new PublishToHaTrigger(this);

        public override string ToString() => $"Category: {Category}, Trigger: {nameof(PublishToHaTrigger)}, Every: {AfterExposures}, Call: {Domain}.{Service}";
    }
}
