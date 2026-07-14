# Home Assistant (N.I.N.A. plugin): support knowledge base

**This is written for a support agent, not for a user.** Quote it, do not
paraphrase it: the sentences here are checked against the code, a paraphrase is
not.

Applies to **1.2.2**. To check what the user is running: the version is shown next
to the plugin name in N.I.N.A.'s plugin manager, under **Plugins → Installed →
Home Assistant**.

**The plugin's own interface is English only**, whatever language N.I.N.A. itself
is set to. Every label quoted in this document is the exact string the user sees.
Two vocabularies collide here and the user will mix them: Home Assistant says
*entity*, *service*, *domain*, *long-lived access token*; N.I.N.A. says *switch*,
*channel*, *instruction*, *trigger*, *condition*. When a user says "switch" they
usually mean a N.I.N.A. channel, not the HA `switch` domain. Ask which one they
mean rather than guessing.

**Never invent a figure, a path, an entity domain or a compatibility claim.** If
the answer is not in this document, the correct answer is *"I don't know, I'm
passing this to the team."*

**Never ask a user for their access token, and never accept one.** If a user
pastes a Home Assistant token in the chat, tell them to revoke it immediately in
Home Assistant (**Profile → Security → Long-lived access tokens**) and create a
new one. A token is a full-control credential for their home.

- Repository and issue tracker: https://github.com/caelo-works/nina.plugin.homeassistant
- Product page: https://nina-plugins.caelo.works/en/plugins/home-assistant

---

## The product card: what the Home Assistant plugin is

The Home Assistant plugin bridges a [Home Assistant](https://www.home-assistant.io/)
instance into N.I.N.A. in two ways: it exposes HA entities as channels of a native
**Switch** device, and it adds a set of **advanced sequencer** instructions that
call HA services and read HA states.

| | |
|---|---|
| Version | 1.2.2 |
| Publisher | CaeloWorks |
| Licence | MPL-2.0, free and open source |
| Requires | **N.I.N.A. 3.2 or newer**, Windows |
| Requires | A reachable **Home Assistant** instance and a **long-lived access token** |
| Name in the plugin manager | **Home Assistant** |
| Where it appears | **Options → Plugins → Home Assistant** (settings), **Equipment → Switch** (device), **Advanced Sequencer** (category **Home Assistant**) |

**What it needs from Home Assistant**, and nothing more: the base URL of the
instance (for example `http://homeassistant.local:8123`) and a long-lived access
token, created by the user in Home Assistant under **Profile → Security →
Long-lived access tokens**. No custom integration, no add-on, no YAML is installed
on the Home Assistant side.

**Everything the plugin does goes through the standard Home Assistant HTTP API**
(`/api/states`, `/api/services`) and, for live updates, the WebSocket API
(`/api/websocket`). If a user's token cannot read `/api/states`, nothing in the
plugin will work.

---

## Installation: installing the Home Assistant plugin

### From N.I.N.A.'s plugin manager (recommended)

The plugin is published in the official N.I.N.A. community plugin list. In the
plugin manager, under **Available**, it is listed as **Home Assistant**, publisher
**CaeloWorks**. Install it, then **restart N.I.N.A.**

### Manual install

The release ships **two DLLs**, and both are required:

- `NinaHA.Plugin.dll`
- `NinaHA.Client.dll`

They are downloaded as `NinaHA.Plugin.zip` from
https://github.com/caelo-works/nina.plugin.homeassistant/releases/latest and
extracted into the plugin folder:

`%LOCALAPPDATA%\NINA\Plugins\<N.I.N.A. version>\NinaHA.Plugin\`

Then **restart N.I.N.A.** If a user copied only `NinaHA.Plugin.dll` and left
`NinaHA.Client.dll` behind, the plugin will not load. That is the classic manual
install mistake.

### "I installed it and I don't see it"

After a restart, the plugin adds three things. If any one of them is missing, the
plugin did not load:

1. A settings page at **Options → Plugins → Home Assistant**.
2. A **Home Assistant** device in the **Equipment → Switch** device list.
3. A **Home Assistant** category in the **Advanced Sequencer** instruction list.

Almost always one of:

- **N.I.N.A. was not restarted** after the install. This is the number one cause.
- The user is running **N.I.N.A. older than 3.2**. The plugin declares a minimum
  application version of **3.2.0.0** and N.I.N.A. will not load it below that.
- **Manual install with only one of the two DLLs** (see above).

---

## The options page: Options → Plugins → Home Assistant

The page has two boxes: **Home Assistant connection** and **Switch channels**.

### The "Home Assistant connection" box

- **Base URL**: for example `http://homeassistant.local:8123`. Since **1.2.0**
  the URL is normalized: if the scheme is missing, `http://` is assumed, and a
  trailing slash is removed. So `homeassistant.local:8123` is accepted.
- **Token**: the Home Assistant **long-lived access token**. It is **masked by
  default** since 1.2.0; the **eye button** on its right reveals it. Never ask the
  user to show or send it.
- **Live updates**: a checkbox labelled **Use WebSocket (REST polling fallback)**,
  **on by default**. On: state changes are streamed over the Home Assistant
  WebSocket API. Off: values refresh only when N.I.N.A. polls the switch device
  over REST. Both work; WebSocket is just faster.
- **Test connection**: pings the instance, then loads the entity list and the
  service list.
- **The coloured dot** next to the button (since 1.2.0) is the connection status:
  **grey** = not tried yet, **green** = connected, **red** = the last attempt
  failed. Next to it, the status message.

### The "Switch channels" grid

One row is one Home Assistant entity exposed as one N.I.N.A. switch channel.
Columns:

- **Name**: free text, what N.I.N.A. will show. **If left empty, the entity id is
  used as the name.** If the entity has a `unit_of_measurement`, that unit is
  appended in parentheses, so a channel named `Terrace temp` on a °C sensor shows
  up in N.I.N.A. as **`Terrace temp (°C)`**.
- **Entity**: a searchable picker, filled from the entity list loaded from Home
  Assistant. Type to filter.
- **Type**: **Binary**, **Stepped** or **Analog**. See the channel types section.
- **Direction**: **Read**, **Write** or **ReadWrite**.
- **Preview**: the entity's current state, from the last load. It is **a snapshot,
  not a live value**: it is filled when N.I.N.A. starts (since 1.1.0, if a
  connection is already configured) and again on **Test connection**. A dash
  (`—`) means the entity is unknown, has no value, or nothing has been loaded yet.
- The **trash button** removes the row.

Below the grid, **Add channel** and **Save**.

### Saving: what saves itself, and what needs the Save button

This distinction causes real support cases.

- **Base URL**, **Token** and **Live updates** save themselves as they are typed
  or toggled.
- **The grid rows do not.** Editing a row's Name, Entity, Type or Direction is
  only persisted when the user clicks **Save**. Adding or removing a row saves
  immediately.

So: *"I set up my channels, restarted N.I.N.A., and they are gone / back to their
old values."* → they edited the rows and never clicked **Save**.

### The pickers are filled from a snapshot, not live

The entity and service lists behind every searchable picker (options page and
sequencer alike) are loaded when N.I.N.A. starts and refreshed by **Test
connection**. An entity created in Home Assistant **after** N.I.N.A. started will
not be in the list until the user clicks **Test connection** (or restarts
N.I.N.A.). The field is free text, so a correct entity id typed by hand works even
if it is not in the list.

---

## The Switch device: Equipment → Switch → Home Assistant

### Connecting

**Equipment → Switch**, choose **Home Assistant** in the device list, click
**Connect**. The channels then behave like any native N.I.N.A. switch: built-in
functions (for example *Set Switch Value*), other plugins and the sequencer can
all drive them without knowing Home Assistant is behind.

**Connecting does three things, in order:** it checks the URL and the token, it
reads a snapshot of all entity states, and it builds one switch per configured
channel. **Rows with an empty Entity are skipped**, no channel is created for
them.

**The channel list is built at connect time and never afterwards.** Adding,
removing, retyping or reordering a channel in the options page has **no effect on
a device that is already connected**. The user must **disconnect and reconnect**
the Switch device. Same for the **Live updates** checkbox: it is read at connect.

**Careful with deleting a row.** A channel's switch index comes from its position
in the grid. Deleting or reordering a row shifts the index of every channel below
it. Anything that already targets a switch by index, in a saved sequence for
example, keeps pointing at the old index and will then drive a different channel.
After deleting a row, have the user re-check the sequence instructions that drive
switches.

### Channel types: Binary, Stepped, Analog

**Binary**: an on/off entity. Reading: the states `on`, `true`, `open`, `home`,
`1`, `yes`, `active`, `unlocked` (case-insensitive) become **1**, and
**everything else becomes 0**. Writing calls **`<entity domain>.turn_on`** or
**`turn_off`**, so `switch.`, `light.`, `input_boolean.` and similar all work.
Min/max/step are fixed at 0/1/1.

**Stepped**: a discrete multi-option entity. Reading: the value is the **index of
the current state in the entity's `options` attribute**. Writing calls
**`select.select_option`**. It is designed for `select` entities, and the entity
**must expose an `options` attribute**; without one the channel has no options to
choose from and is useless. Min/max/step are 0/(N-1)/1.

**Analog**: a numeric entity. Reading: the state is parsed as a number, and **a
state that is not a number reads as 0**. Writing depends on the entity's domain:

- `light` → `light.turn_on` with `brightness_pct`
- `fan` → `fan.set_percentage` with `percentage`
- `input_number` → `input_number.set_value` with `value`
- `number`, and any other domain → `<domain>.set_value` with `value`

**Min, max and step for an Analog channel** are read from the entity's attributes
(`min`/`max`/`step`, or `native_min_value`/`native_max_value`/`native_step`), and
fall back to **0, 100 and 1** when the entity does not publish them. **They cannot
be overridden in the options page in 1.2.2.** They are captured **at connect
time**, so if the entity's range changes in Home Assistant, N.I.N.A. keeps the old
one until the device is reconnected.

### Unavailable entities read as 0, they do not raise an error

This trips people up. An entity that is `unavailable` or `unknown` is not a
number, and it is not in the truthy list, so **a Binary channel reads it as off
and an Analog channel reads it as 0**. Nothing in the N.I.N.A. UI says the entity
is dead. If a user says a sensor "reads zero", check in Home Assistant that the
entity is actually alive.

### Writing a value, and why nothing seems to happen

A write builds one Home Assistant service call from the channel's type and domain
(see above) and posts it. **If Home Assistant rejects the call, the failure is
written to the N.I.N.A. log and nothing at all appears in the interface**: the
value simply does not change. The usual causes:

- The entity is **read-only in Home Assistant** (a `sensor`, a `binary_sensor`),
  and the channel was set to Write or ReadWrite. Sensors must be **Read**.
- The **token does not have permission** to call the service.
- The channel **Type** does not match the entity, so the service built for it does
  not exist for that entity's domain.

The answer is always: set the channel to Read if the entity is a sensor, otherwise
check the N.I.N.A. log for the rejected call, and escalate with it.

### Live updates, WebSocket and REST polling

With **Live updates** on, the plugin holds a WebSocket subscription to Home
Assistant's `state_changed` events and pushes new values into the switches as they
happen. If the socket drops, it **reconnects on its own** with a backoff that grows
from 1 second up to 30 seconds, and in the meantime N.I.N.A.'s normal switch
polling refreshes values over REST. **A dropped WebSocket does not break
anything**, it only makes updates slower. With Live updates off, every refresh is
a REST read, which is entirely supported.

---

## The advanced sequencer instructions (category "Home Assistant")

The plugin adds two instructions, one loop condition and one trigger, all under the
**Home Assistant** category. **They do not need the Switch device to be
connected**: they reuse the plugin's connection settings directly. They do need the
plugin to be configured, and they all show the same red validation message when it
is not.

### Call HA Service (instruction)

Calls any Home Assistant service. Fields:

- **Service**: a searchable picker holding `domain.service`, for example
  `light.turn_on`. Both a domain and a service name are required.
- **Entity**: optional. When filled it is sent as `entity_id`.
- **Data**: optional, and if present it **must be a valid JSON object**, for
  example `{"brightness_pct": 40}`. `$$TOKEN$$` patterns are expanded in it, see
  below.

### Wait for HA State (instruction)

Blocks the sequence until an entity's state satisfies a comparison. Fields:

- **Wait until** *(entity)*, a comparison operator, and a value.
- **Timeout (s)**: default **300**. **`0` means wait indefinitely.** On timeout
  the instruction **fails the sequence entity** with the message quoted in the
  error-messages section.
- **Poll (s)**: default **5**, minimum 1. How often the entity is re-read.
- **Current:** shows the entity's live value. A dash (`—`) means it could not be
  read.

### HA State (loop condition)

Keeps a loop running **while** an entity satisfies a comparison. Fields: an
entity, a comparison operator and a value, plus a **Current:** live value.

Two facts that matter:

- The condition is evaluated against a value **refreshed every 5 seconds**, and
  that interval is **not configurable**. The condition can therefore be up to 5
  seconds stale.
- When the condition becomes false, it does not wait for the current instruction to
  finish: it **interrupts the surrounding block**.

### Publish to HA (trigger)

Every **N** exposures, calls a Home Assistant service, so a sequence can push its
status to a Home Assistant dashboard. Fields: **Every _N_ exposure(s), call**
*(service)*, **Entity**, **Data**.

- **Every** must be **at least 1**. It counts N.I.N.A.'s captured images.
- The **Data** field is where the `$$TOKEN$$` patterns are used, for example
  `{"state": "$$TARGETNAME$$"}`.

### The $$TOKEN$$ patterns accepted in a Data field

**Exactly these ten, and no others:**

`$$TARGETNAME$$`, `$$FILTER$$`, `$$CAMERA$$`, `$$GAIN$$`, `$$OFFSET$$`,
`$$TEMPERATURE$$`, `$$SQM$$`, `$$DATE$$`, `$$TIME$$`, `$$DATETIME$$`

This is **not** N.I.N.A.'s full file-naming pattern list. Any other `$$...$$` token
is left in the payload untouched, as literal text.

**A pattern must be inside a quoted JSON string.** `{"gain": "$$GAIN$$"}` is
correct. `{"gain": $$GAIN$$}` is not valid JSON and the instruction will refuse to
validate with *"Data must be a valid JSON object."*

**A pattern whose source is not available resolves to an empty string**, it does
not fail. No filter wheel connected means `$$FILTER$$` becomes `""`. No camera
connected means `$$GAIN$$`, `$$OFFSET$$`, `$$TEMPERATURE$$` and `$$CAMERA$$` become
`""`. `$$TARGETNAME$$` is empty unless the item sits inside a target container.
`$$SQM$$` needs a weather device. `$$DATE$$`, `$$TIME$$` and `$$DATETIME$$` are the
local clock and always resolve.

### How a comparison is evaluated (Wait for HA State and HA State)

Operators: **Equals**, **NotEquals**, **GreaterThan**, **GreaterOrEqual**,
**LessThan**, **LessOrEqual**.

- **Equals / NotEquals**: if both sides are numbers they are compared as numbers,
  otherwise as text, **case-insensitively** and trimmed. So `on` equals `ON`.
- **The four ordering operators are numeric only.** If either side is not a number,
  the comparison is simply **false**. An entity that is `unavailable` compared with
  `> 5` is false, forever. That is why a *Wait for HA State* on a dead sensor sits
  there until it times out.
- Numbers are parsed with a dot as decimal separator, regardless of the user's
  Windows locale. `21.5`, not `21,5`.

---

## Error messages, word for word

The user will paste the message. These are the exact strings.

### Validation messages (red, shown on the sequencer item)

**"Home Assistant is not configured (Options > Plugins > Home Assistant)."**
No base URL, or no token, is saved in the plugin settings. Every Home Assistant
sequencer item shows this until both are filled in.

**"Entity id is required."**
*Wait for HA State* or *HA State* with an empty entity field.

**"Service domain is required."** / **"Service name is required."**
*Call HA Service* with an empty or incomplete **Service** field. It must be
`domain.service`, for example `light.turn_on`.

**"Service domain and name are required."**
The same thing on the *Publish to HA* trigger.

**"Data must be a valid JSON object."**
The **Data** field is not a JSON object. Two usual causes: an unquoted `$$TOKEN$$`
(`{"gain": $$GAIN$$}` instead of `{"gain": "$$GAIN$$"}`), or a bare value instead of
an object (`brightness_pct: 40` instead of `{"brightness_pct": 40}`).

**"Poll interval must be at least 1 second."**
*Wait for HA State* with **Poll (s)** below 1.

**"'After exposures' must be at least 1."**
*Publish to HA* with **Every** below 1.

### Connection messages on the options page

**"Connecting..."**
Transient, shown while **Test connection** runs.

**"Connected. N entities, M services."**
Success. The dot turns green. N and M come from the user's own Home Assistant.

**"Connection failed. Check the URL and token."**
Home Assistant did not answer `GET /api/` with a success. The dot turns red. Either
the URL is wrong or unreachable from the N.I.N.A. machine, or the token is wrong,
expired or revoked. The plugin cannot tell which.

**"Connection error: <message>"**
The request threw before getting an answer. The tail of the message is the network
error and it matters: name resolution, refused connection, timeout, TLS failure.
Ask for the whole line.

**"Could not load Home Assistant entities: <message>"**
Same thing, but raised by the automatic load at N.I.N.A. startup rather than by the
**Test connection** button.

### Runtime message in a sequence

**"Timed out after Ns waiting for <entity> <operator> <value>."**
Raised by *Wait for HA State* when the timeout elapses. The message names the
entity, the operator and the value it was waiting for, which is usually enough to
see why. Remember that an ordering comparison against a non-numeric state is always
false, so it will always time out.

---

## Known bugs and limits: read before answering

**No bug is currently open in 1.2.2.** What follows is, first, the bugs that were
real in earlier versions, and then the deliberate limits of 1.2.2. Both matter: a
user on an old version has a real bug, and a user on 1.2.2 may be hitting a limit
that is not their fault.

### Bugs fixed in earlier versions: check what the user is running

If the user's symptom is one of these, **the answer is to update the plugin**, not
to change their settings.

- **Typing in an entity or service picker wipes the whole value.** Real in
  **1.0.0**, fixed in **1.0.1**. Editing an already-filled picker erased it on the
  first keystroke.
- **An unlabelled on/off toggle in the connection box.** Real in **1.0.0**, fixed
  in **1.0.1**: it is the **Live updates** checkbox and it now has a label.
- **The Preview column stays empty until "Test connection" is clicked.** Real up to
  **1.0.1**, fixed in **1.1.0**: it now fills at N.I.N.A. startup when a connection
  is already configured.
- **A base URL without `http://` does not connect.** Real up to **1.1.0**, fixed in
  **1.2.0**: the URL is now normalized.
- **The token field and the pickers ignore the N.I.N.A. theme.** Real up to
  **1.2.0**, fixed in **1.2.1**.

### Limit: min, max, step and the option list cannot be edited

The options grid exposes **Name, Entity, Type, Direction** and nothing else. For an
**Analog** channel, min/max/step come from the entity's own attributes and fall
back to **0/100/1**; for a **Stepped** channel the options come from the entity's
`options` attribute. **There is no way to override them in 1.2.2.** If a user needs
a different range, the range has to be fixed on the Home Assistant side. Confirm
the limit, do not send them looking for a field that does not exist.

### Limit: a Stepped channel only writes `select.select_option`

Stepped writing is built for `select` entities. Another domain with discrete
options will not be driven correctly on write in 1.2.2. Reading still works as long
as the entity publishes an `options` attribute.

### Limit: HTTPS with a certificate Windows does not trust will not connect

The plugin does nothing to bypass certificate validation. An `https://` base URL
whose certificate is self-signed, or otherwise not trusted by Windows, fails to
connect. The fix is to use the plain `http://` LAN URL of the instance, or a
certificate the machine actually trusts.

### Limit: settings are stored per N.I.N.A. profile

The connection and the channel list are saved in the **active N.I.N.A. profile**.
Switching to another profile means another, empty configuration. *"All my channels
have disappeared"* is very often a profile switch, not data loss: switching back
brings them back.

### Limit: a failed write is silent in the interface

When Home Assistant rejects a service call made by a writable switch, the value
does not change and **nothing is shown in the N.I.N.A. interface**. The failure only
reaches the N.I.N.A. log.

---

## Troubleshooting: symptom → cause → answer

### Install and connection problems

**"I installed it but I don't see it anywhere."**
**Restart N.I.N.A.** If it still is not there: the plugin needs **N.I.N.A. 3.2 or
newer**, and a manual install needs **both** `NinaHA.Plugin.dll` **and**
`NinaHA.Client.dll` in the plugin folder.

**"Test connection says: Connection failed. Check the URL and token."**
Home Assistant did not accept the request. Have the user check, in this order: that
the base URL opens Home Assistant **in a browser on the N.I.N.A. machine**; that the
token was created under **Profile → Security → Long-lived access tokens** and has
not been revoked; that the token was pasted whole, with no leading or trailing
space. Note that an `https://` URL with a self-signed certificate cannot work.

### Channels that vanish, or that never appear

**"My channels are gone after restarting N.I.N.A."**
Either they edited the grid rows and never clicked **Save** (only the connection
fields save themselves, the rows need the **Save** button), or they switched
N.I.N.A. **profile**, since the settings are per profile.

**"I added a channel but it is not in the Switch device."**
The channel list is built when the Switch device connects. **Disconnect and
reconnect** the Switch device. And check the row actually has an entity: rows with
an empty Entity are skipped.

**"My new Home Assistant entity is not in the entity picker."**
The entity list is a snapshot taken at N.I.N.A. startup. Click **Test connection**
to reload it. The field is free text, so a correct entity id typed by hand also
works.

### Wrong values, and writes that do nothing

**"My sensor reads 0 / my switch shows off, but in Home Assistant it is fine."**
Check the entity is not `unavailable` or `unknown` in Home Assistant: an unreadable
state is not a number and is not truthy, so it reads as 0 / off with no error.
Otherwise check the channel **Type** matches the entity: an Analog channel on a
text entity reads 0, and a Binary channel only reads `on`, `true`, `open`, `home`,
`1`, `yes`, `active`, `unlocked` as on.

**"I set a value on a channel and nothing happens."**
The service call was rejected by Home Assistant and the failure is only in the
N.I.N.A. log. The usual cause is that the entity is **read-only in Home Assistant**
(a `sensor` or `binary_sensor`) while the channel is set to Write or ReadWrite. Set
it to **Read**. If the entity really is writable, ask for the N.I.N.A. log and
escalate.

**"My analog channel's slider goes 0 to 100 and that is wrong."**
The entity does not publish `min`/`max`/`step` attributes, so the plugin falls back
to 0/100/1, and **1.2.2 has no way to override that in the options page**. It is a
known limit. The range has to be set on the Home Assistant side.

### Sequencer problems

**"Wait for HA State always times out."**
Almost always an ordering comparison (`>`, `>=`, `<`, `<=`) against a state that is
not a number, which is **always false**. Check the entity's state in Home Assistant:
if it is `unavailable`, or text like `on`, an ordering comparison can never match.
Also check the decimal separator: values are parsed with a **dot**, never a comma.

**"My loop with the HA State condition reacts slowly."**
Expected: the condition re-reads the entity **every 5 seconds** and that interval is
not configurable.

**"Data must be a valid JSON object", but my JSON looks fine.**
The most common cause is an unquoted pattern. Write `{"gain": "$$GAIN$$"}`, not
`{"gain": $$GAIN$$}`. The Data field must also be an **object**, wrapped in `{ }`.

**"My published data has empty values."**
A `$$TOKEN$$` whose source is not connected resolves to an empty string. No filter
wheel means `$$FILTER$$` is empty; no camera means `$$GAIN$$`, `$$OFFSET$$`,
`$$TEMPERATURE$$` and `$$CAMERA$$` are empty; `$$TARGETNAME$$` is empty outside a
target container.

### Live updates

**"Live updates stopped / the WebSocket disconnected."**
Not a problem. It reconnects on its own with a growing backoff, up to 30 seconds
between attempts, and values keep refreshing over REST in the meantime. Nothing is
lost.

---

## Escalation: when to stop and hand over to a human

**Stop and escalate, do not improvise, when:**

- **A user posts their Home Assistant token, or any part of it.** Tell them
  immediately to revoke it in Home Assistant (**Profile → Security → Long-lived
  access tokens**) and create a new one. Never repeat the token back. Never ask for
  one.
- The user hits one of the **limits** listed above and needs it lifted. Confirm the
  limit honestly, then hand over. Do not promise a version or a date.
- **A write is rejected by Home Assistant** and the entity really is writable. That
  needs the log.
- The question is about **Home Assistant itself** rather than the plugin, such as
  how to build an automation, a template sensor or a dashboard. That is not this
  plugin's scope.
- Anything this document does not cover. Say *"I don't know, I'm passing this to the
  team."* A confident guess about someone's observatory hardware is worse than
  silence.
- Anything involving payment or a commercial commitment. The plugin is free and
  MPL-2.0, and that is the whole story.

**Collect these before escalating. Without them the report is not actionable:**

1. The **plugin version** (**Plugins → Installed → Home Assistant**) and the
   **N.I.N.A. version**.
2. The **exact message**, copied, whether it is a red validation message on a
   sequencer item or the status line on the options page.
3. **What the entity looks like in Home Assistant**: its full `entity_id`, its
   current state, and its attributes. A screenshot of the entity in Home Assistant's
   developer tools is ideal.
4. The **channel setup**: Type and Direction of the row, or a screenshot of the
   options page. **The token must be masked**, and it is by default.
5. The **N.I.N.A. log** for the session, which is where a rejected service call and
   any read failure are recorded.

Bugs can also be filed directly at
https://github.com/caelo-works/nina.plugin.homeassistant/issues
