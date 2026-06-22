# Changelog

All notable changes to this project are documented in this file. The format is
based on [Keep a Changelog](https://keepachangelog.com/), and this project
adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [1.2.2] - 2026-06-22

### Changed
- `FeaturedImageURL` now points to the logo published as a release asset (https),
  as required by the public N.I.N.A. plugin catalog.
- Release workflow now builds and packages the plugin (both DLLs) into a zip with
  a SHA256 checksum and a generated manifest, for submission to the N.I.N.A.
  community plugin manifest repository.

## [1.2.1] - 2026-06-22

### Fixed
- Options page: the custom controls now follow the user's NINA theme — the
  searchable picker adopts the ambient ComboBox style, the token field (masked
  and revealed) and the show/hide toggle use the NINA theme brushes.

## [1.2.0] - 2026-06-22

### Added
- Options page: a colored connection indicator (grey / green / red) next to the
  status message, reflecting the last load or connection test.
- Continuous integration (GitHub Actions) running the `NinaHA.Client` unit tests
  on every push and pull request.
- Release workflow publishing `logo.png` as a release asset when a `v*` tag is
  pushed, giving the logo a stable https URL.

### Changed
- Options page: the access token is now masked by default, with an eye toggle to
  reveal it.
- The Home Assistant base URL is normalized (scheme defaulted to `http://`,
  trailing slash removed), so inputs like `homeassistant.local:8123` connect.

## [1.1.0] - 2026-06-22

### Changed
- Options page: when a connection is already configured, the channel grid's
  Preview column now fills automatically at NINA startup, without having to click
  "Test connection" first.

## [1.0.1] - 2026-06-22

### Fixed
- Options page: the "Use WebSocket" connection toggle now has a visible label
  ("Live updates"), since the NINA theme renders a checkbox as an unlabelled
  on/off switch.
- Searchable entity/service picker: editing an already-entered value no longer
  wipes it. The editable combo box selected all of its text when its drop-down
  opened, so the first keystroke replaced the whole value; the automatic
  full-selection is now collapsed so typing inserts at the caret.

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
