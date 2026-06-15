using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NinaHA.Client.Dto;

namespace NinaHA.Client {

    /// <summary>
    /// Thread-safe cache of the latest known <see cref="HaState"/> per entity.
    /// Seeded from a REST snapshot and kept current by WebSocket state-change events.
    /// </summary>
    public sealed class EntityStateStore {

        private readonly ConcurrentDictionary<string, HaState> states =
            new ConcurrentDictionary<string, HaState>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Raised whenever an entity state is added or updated.</summary>
        public event Action<HaState>? StateChanged;

        public void Set(HaState state) {
            if (state == null || string.IsNullOrEmpty(state.EntityId)) {
                return;
            }
            states[state.EntityId] = state;
            StateChanged?.Invoke(state);
        }

        public void Seed(IEnumerable<HaState> snapshot) {
            foreach (var state in snapshot) {
                if (state != null && !string.IsNullOrEmpty(state.EntityId)) {
                    states[state.EntityId] = state;
                }
            }
        }

        public bool TryGet(string entityId, out HaState state) => states.TryGetValue(entityId, out state!);

        public HaState? Get(string entityId) => states.TryGetValue(entityId, out var s) ? s : null;

        public void Clear() => states.Clear();
    }
}
