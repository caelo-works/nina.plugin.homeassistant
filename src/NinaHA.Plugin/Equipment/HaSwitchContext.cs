using System;
using NinaHA.Client;

namespace NinaHA.Plugin.Equipment {

    /// <summary>Shared runtime dependencies handed to every switch by the hub.</summary>
    public sealed class HaSwitchContext {

        public HaSwitchContext(HomeAssistantRestClient rest, EntityStateStore store, Func<bool> isWebSocketConnected) {
            Rest = rest;
            Store = store;
            IsWebSocketConnected = isWebSocketConnected;
        }

        public HomeAssistantRestClient Rest { get; }

        public EntityStateStore Store { get; }

        public Func<bool> IsWebSocketConnected { get; }
    }
}
