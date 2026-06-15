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
- Unit tests for the value mapper, configuration serialization, and REST client.
