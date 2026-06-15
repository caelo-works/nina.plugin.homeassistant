# Changelog

## [Unreleased]

### Added
- Initial plugin scaffold (NINA 3.2, .NET 8).
- `NinaHA.Client` library: Home Assistant REST client, WebSocket client (auth + `state_changed`
  subscription with reconnection), in-memory entity state cache, configuration model, and value mapper.
- Switch equipment provider, hub, and read-only/writable switches.
- Options page: connection settings, connection test, and channel mapping (binary/stepped/analog,
  read/write/read-write), split into Connection and Switch sections, with button icons and a live
  channel-state preview column.
- Plugin logo, embedded and used as the plugin-manager icon (pack:// URI to the embedded resource).
- Advanced sequencer entities (category "Home Assistant"): Call HA Service, Wait for HA State,
  HA State loop condition, and an On HA State trigger (rising-edge) that calls a service.
- Searchable, autocompleting pickers for entities and services everywhere (options grid and all
  sequencer items), backed by a shared catalog loaded from /api/states and /api/services.
- Unit tests for the value mapper, configuration serialization, REST client, state comparison, and
  service-data parsing.
