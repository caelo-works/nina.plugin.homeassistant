# Home Assistant — NINA plugin

A plugin for [N.I.N.A. (Nighttime Imaging 'N' Astronomy)](https://nighttime-imaging.eu/) that
exposes [Home Assistant](https://www.home-assistant.io/) entities as channels of a NINA **Switch**
device.

Once configured, the hub behaves like any native switch: it can be driven by NINA's built-in
functions (e.g. the *Set Switch Value* sequence instruction), conditions, and other plugins —
without them needing to know Home Assistant is behind it.

## Features

- Connect to a Home Assistant instance with a long-lived access token.
- Map any number of entities to switch channels, each:
  - **read-only**, **write-only** or **read/write**, and
  - typed as **binary** (on/off), **stepped** (discrete options, e.g. a `select`) or **analog**
    (numeric, with min/max/step taken from the entity).
- Live updates via the Home Assistant **WebSocket** API, with **REST polling** as a fallback.

## Channel type mapping

| Type | Read (state → value) | Min/Max/Step | Write (value → service) |
|------|----------------------|--------------|--------------------------|
| Binary | `on`/`true`/… → 1, else 0 | 0 / 1 / 1 | `<domain>.turn_on` / `turn_off` |
| Stepped | index of state in options | 0 / N-1 / 1 | `select.select_option` |
| Analog | numeric state | entity `min`/`max`/`step` (or overrides) | `number`/`input_number.set_value`, `light` brightness, `fan` percentage |

## Repository layout

```
src/NinaHA.Client    .NET 8 library: HA REST + WebSocket clients, state cache, config, value mapping
src/NinaHA.Plugin    NINA plugin (WPF): equipment provider/hub/switches + options page
tests/               xUnit tests for the client library
```

## Build & install

Requires the .NET 8 SDK on Windows (NINA is Windows/WPF only).

```
dotnet build src/NinaHA.Plugin/NinaHA.Plugin.csproj -c Release
```

The build copies `NinaHA.Plugin.dll` and `NinaHA.Client.dll` into
`%LOCALAPPDATA%\NINA\Plugins\3.0.0\NinaHA.Plugin\`. Restart NINA to load the plugin.

## Configuration

1. In NINA, open **Options ▸ Plugins ▸ Home Assistant**.
2. Enter the base URL (e.g. `http://homeassistant.local:8123`) and a long-lived access token,
   then click **Test connection** to load the entity list.
3. Add channels, pick an entity, type and direction, then **Save**.
4. Go to **Equipment ▸ Switch**, choose **Home Assistant** and connect.

## License

[MPL-2.0](LICENSE.txt).
