# Changelog

All notable changes to this project are documented in this file. The format is
based on [Keep a Changelog](https://keepachangelog.com/), and this project
adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [1.0.0] - 2026-06-22

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
  HA State loop condition, and a Publish to HA trigger that calls a service every N exposures.
- Live current-value preview on the Wait for HA State item and the HA State condition (the condition
  also interrupts its parent block when it becomes false).
- "$$TOKEN$$" pattern support (TARGETNAME, FILTER, CAMERA, GAIN, OFFSET, TEMPERATURE, SQM, DATE,
  TIME, DATETIME) in the data payload of Call HA Service and Publish to HA.
- Searchable, autocompleting pickers for entities and services everywhere (options grid and all
  sequencer items), backed by a shared catalog loaded from /api/states and /api/services.
- Unit tests for the value mapper, configuration serialization, REST client, state comparison, and
  service-data parsing.
