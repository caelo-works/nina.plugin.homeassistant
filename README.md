<div align="center">

# Home Assistant

### Expose your smart-home entities as N.I.N.A. switches and drive Home Assistant from the advanced sequencer.

[![Version](https://img.shields.io/github/v/release/caelo-works/nina.plugin.homeassistant?style=for-the-badge&labelColor=0f172a&color=22d3ee&label=version)](https://github.com/caelo-works/nina.plugin.homeassistant/releases/latest)
[![N.I.N.A.](https://img.shields.io/badge/N.I.N.A.-%E2%89%A5%203.2-67e8f9?style=for-the-badge&labelColor=0f172a)](https://nighttime-imaging.eu/)
[![Status](https://img.shields.io/badge/status-active-34d399?style=for-the-badge&labelColor=0f172a)](https://nina-plugins.caelo.works/en/plugins/home-assistant)
[![License](https://img.shields.io/badge/license-MPL--2.0-94a3b8?style=for-the-badge&labelColor=0f172a)](LICENSE.txt)
[![Website](https://img.shields.io/badge/%E2%86%92%20see%20all%20plugins-nina--plugins.caelo.works-0f172a?style=for-the-badge&labelColor=22d3ee)](https://nina-plugins.caelo.works/en)

<a href="https://nina-plugins.caelo.works/en"><img src="https://nina-plugins.caelo.works/assets/readme-banner.png" alt="CaeloWorks · N.I.N.A. Plugins" width="75%"></a>

</div>

---

## Overview

Home Assistant bridges your [Home Assistant](https://www.home-assistant.io/) instance into
[N.I.N.A.](https://nighttime-imaging.eu/) in two ways. It exposes any HA entity as a channel of a
native **Switch** device, so power boxes, dew heaters, relays, sensors and more become usable by all
of N.I.N.A.'s built-in functions and other plugins, without them knowing Home Assistant is behind it.
And it adds a set of **advanced sequencer** instructions to call services, wait on states, branch on
conditions and publish N.I.N.A. status back to Home Assistant from a sequence.

> 📖 **Full details, screenshots & docs:** **[nina-plugins.caelo.works/en/plugins/home-assistant](https://nina-plugins.caelo.works/en/plugins/home-assistant)**

## Features

| | |
|---|---|
| 🔌 **Switch equipment** | Map any HA entity to a Switch channel: **read-only / write / read-write**, typed **binary** (on/off), **stepped** (discrete, e.g. a `select`) or **analog** (numeric, with min/max/step from the entity). The unit of measurement is shown in the channel name, e.g. `Terrace temp (°C)`. |
| 🧩 **Advanced sequencer** | *Call HA Service*, *Wait for HA State*, *HA State* loop condition and *Publish to HA* trigger, in the **Home Assistant** category. The data payload supports N.I.N.A. patterns (`$$TARGETNAME$$`, `$$FILTER$$`, `$$CAMERA$$`, `$$GAIN$$`, `$$OFFSET$$`, `$$TEMPERATURE$$`, `$$SQM$$`, `$$DATE$$`, `$$TIME$$`, `$$DATETIME$$`). |
| ⚡ **Live updates** | State changes stream in over the Home Assistant **WebSocket** API, with **REST polling** as an automatic fallback. |
| 🔎 **Searchable everywhere** | Entity and service fields use searchable, autocompleting pickers; the options grid preview fills automatically at startup once configured. |

**Channel type mapping**

| Type | Read (state → value) | Min / Max / Step | Write (value → service) |
|------|----------------------|------------------|--------------------------|
| Binary | `on`/`true`/… → 1, else 0 | 0 / 1 / 1 | `<domain>.turn_on` / `turn_off` |
| Stepped | index of state in options | 0 / N-1 / 1 | `select.select_option` |
| Analog | numeric state | entity `min`/`max`/`step` (or overrides) | `number`/`input_number.set_value`, `light` brightness, `fan` percentage |

## Installation

### From N.I.N.A.'s plugin manager (recommended)

1. In N.I.N.A., go to **Plugins → Available**.
2. Find **Home Assistant** (CaeloWorks) in the list and click **Install**.
3. **Restart N.I.N.A.** The plugin appears under **Plugins** and adds a **Home Assistant** Switch device.

### Manual install

Download `NinaHA.Plugin.zip` from the
**[Releases](https://github.com/caelo-works/nina.plugin.homeassistant/releases/latest)**, extract the
two DLLs (`NinaHA.Plugin.dll` + `NinaHA.Client.dll`) into your N.I.N.A. plugins folder
(`%LOCALAPPDATA%\NINA\Plugins\<NINA version>\NinaHA.Plugin\`), then restart N.I.N.A.

> **Requires N.I.N.A. 3.2 or newer.**

## Getting started

1. Open **Options → Plugins → Home Assistant**. Enter the base URL (e.g.
   `http://homeassistant.local:8123`) and a long-lived access token, then click **Test connection** to
   load the entity and service lists.
2. **Add channels**: pick an entity, a type (binary / stepped / analog) and a direction, then **Save**.
3. Go to **Equipment → Switch**, choose **Home Assistant** and **Connect**. Your channels are now
   driveable by native functions (e.g. *Set Switch Value*), conditions and other plugins.
4. In the **Advanced Sequencer**, use the **Home Assistant** instructions (they reuse the same
   connection settings).

## Links

- 🌐 **Plugin page:** [nina-plugins.caelo.works/en/plugins/home-assistant](https://nina-plugins.caelo.works/en/plugins/home-assistant)
- 📦 **Releases:** [github.com/caelo-works/nina.plugin.homeassistant/releases](https://github.com/caelo-works/nina.plugin.homeassistant/releases)
- 🏠 **Home Assistant:** [home-assistant.io](https://www.home-assistant.io/)

## Screenshots

<div align="center">

![Home Assistant plugin options page in N.I.N.A.](https://nina-plugins.caelo.works/assets/plugins/home-assistant-1-options.webp)

![Home Assistant entities as a N.I.N.A. Switch device](https://nina-plugins.caelo.works/assets/plugins/home-assistant-2-switch.webp)

![Home Assistant advanced sequencer instructions](https://nina-plugins.caelo.works/assets/plugins/home-assistant-3-sequencer.webp)

</div>

---

<div align="center">

### 🌌 More N.I.N.A. plugins by CaeloWorks

**[Explore the full catalogue → nina-plugins.caelo.works](https://nina-plugins.caelo.works/en)**

<sub>Made by <a href="https://caelo.works">CaeloWorks</a> · astrophotography software, firmware & hardware · MPL-2.0 License</sub>

</div>
