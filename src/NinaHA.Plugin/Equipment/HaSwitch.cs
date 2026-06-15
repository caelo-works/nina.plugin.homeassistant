using System;
using System.Threading;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NinaHA.Client;
using NinaHA.Client.Configuration;
using NinaHA.Client.Dto;

namespace NinaHA.Plugin.Equipment {

    /// <summary>
    /// A read-only NINA switch backed by a Home Assistant entity. Values come from the live
    /// <see cref="EntityStateStore"/>; when the WebSocket feed is down, <see cref="Poll"/> refreshes via REST.
    /// </summary>
    public class HaSwitch : BaseINPC, ISwitch {

        protected readonly SwitchChannel Channel;
        protected readonly HaSwitchContext Context;

        public HaSwitch(short id, SwitchChannel channel, HaSwitchContext context) {
            Id = id;
            Channel = channel;
            Context = context;
        }

        public short Id { get; }

        /// <summary>The Home Assistant entity backing this switch (used to route live updates).</summary>
        public string EntityId => Channel.EntityId;

        /// <summary>Raises a change notification for <see cref="Value"/> (e.g. on a live WebSocket update).</summary>
        public void NotifyValueChanged() => RaisePropertyChanged(nameof(Value));

        public string Name {
            get {
                var baseName = string.IsNullOrWhiteSpace(Channel.Name) ? Channel.EntityId : Channel.Name;
                var state = Context.Store.Get(Channel.EntityId);
                if (state != null && state.TryGetAttributeString("unit_of_measurement", out var unit) && !string.IsNullOrWhiteSpace(unit)) {
                    return $"{baseName} ({unit})";
                }
                return baseName;
            }
        }

        public string Description => $"{Channel.EntityId} ({Channel.Type})";

        public double Value => HaValueMapper.StateToValue(Channel, Context.Store.Get(Channel.EntityId));

        public bool Poll() {
            try {
                // When the WebSocket feed is live and we already have a cached value, that's authoritative.
                if (Context.IsWebSocketConnected() && Context.Store.Get(Channel.EntityId) != null) {
                    RaisePropertyChanged(nameof(Value));
                    return true;
                }

                // Otherwise refresh this entity over REST (bounded wait, since Poll is synchronous).
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                HaState? state = Context.Rest.GetStateAsync(Channel.EntityId, cts.Token).GetAwaiter().GetResult();
                if (state == null) {
                    return false;
                }
                Context.Store.Set(state);
                RaisePropertyChanged(nameof(Value));
                return true;
            } catch (Exception ex) {
                Logger.Error($"Home Assistant: failed to poll '{Channel.EntityId}'", ex);
                return false;
            }
        }
    }
}
