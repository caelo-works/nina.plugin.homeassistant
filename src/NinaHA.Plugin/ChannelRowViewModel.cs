using System;
using NINA.Core.Utility;
using NinaHA.Client.Configuration;
using NinaHA.Client.Dto;

namespace NinaHA.Plugin {

    /// <summary>
    /// UI wrapper around a <see cref="SwitchChannel"/> for the options grid. Exposes the editable fields
    /// with change notification and a live <see cref="Preview"/> of the entity's current state.
    /// </summary>
    public sealed class ChannelRowViewModel : BaseINPC {

        private readonly Func<string, HaState?> lookup;

        public ChannelRowViewModel(SwitchChannel channel, Func<string, HaState?> lookup) {
            Channel = channel;
            this.lookup = lookup;
        }

        public SwitchChannel Channel { get; }

        public string Name {
            get => Channel.Name;
            set { Channel.Name = value; RaisePropertyChanged(); }
        }

        public string EntityId {
            get => Channel.EntityId;
            set {
                Channel.EntityId = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Preview));
            }
        }

        public ChannelType Type {
            get => Channel.Type;
            set {
                Channel.Type = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Preview));
            }
        }

        public ChannelDirection Direction {
            get => Channel.Direction;
            set { Channel.Direction = value; RaisePropertyChanged(); }
        }

        /// <summary>Current state of the mapped entity, e.g. "on" or "21.5 °C". Shows "—" when unknown.</summary>
        public string Preview {
            get {
                if (string.IsNullOrWhiteSpace(Channel.EntityId)) {
                    return "—";
                }
                var state = lookup(Channel.EntityId);
                if (state == null) {
                    return "—";
                }
                if (state.TryGetAttributeString("unit_of_measurement", out var unit) && !string.IsNullOrWhiteSpace(unit)) {
                    return $"{state.State} {unit}";
                }
                return string.IsNullOrEmpty(state.State) ? "—" : state.State;
            }
        }

        /// <summary>Re-evaluates <see cref="Preview"/> (e.g. after the entity list is refreshed).</summary>
        public void RefreshPreview() => RaisePropertyChanged(nameof(Preview));
    }
}
