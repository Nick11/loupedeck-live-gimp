# Loupedeck Plugin for GIMP

Plugin for the Logi Actions SDK (Loupedeck software 6.4+ / Logi Options+) to control GIMP 3 with the Loupedeck Live.

> Disclaimer: This plugin was generated with AI

## Installation

**Requires:** Loupedeck Live (or another Loupedeck / MX Creative device) · Loupedeck software ≥ 6.4.1 or Logi Options+ ·
GIMP ≥ 3.0 with Python (macOS: included; Windows: keep the installer's "Python" option on) · macOS or Windows 11

### 1. Loupedeck plugin

1. Double-click `Gimp_<version>.lplug4` (GitHub *Releases*).
2. **macOS only:** System Settings → Privacy & Security → *Accessibility* → enable **LogiPluginService** (needed for the shortcut buttons).

### 2. GIMP extension (separate step, once per GIMP version)

The value dials need the small bridge plug-in, packed as GIMP extension `io.github.nick11.loupedeck-bridge.gex`
(GitHub *Releases*). It only adds this extension; GIMP's settings and shortcuts stay untouched.

1. In GIMP: *File → Open…* and choose the `.gex` file (or drag it onto the GIMP window). GIMP installs it.
2. Restart GIMP. The first start takes a little longer (GIMP registers the plug-in).
3. Check: *Edit → Manage Extensions* lists "Loupedeck Bridge", and a value dial (e.g. *GIMP Value → Brush: Size*)
   shows GIMP's value instead of "–".

### Troubleshooting

- **Dials show "–":** GIMP isn't running, the extension isn't installed for this GIMP version, or GIMP wasn't restarted.
- **Buttons not reacting:** GIMP must be in front with the canvas focused: click into the image once. Don't hold Shift/Ctrl/Alt/Cmd while pressing.
- **New GIMP version (e.g. 3.2 → 3.4):** repeat step 2 in the new version.
- **Update:** install the newer `.lplug4`; if the release notes say the GIMP extension changed, remove it in
  *Edit → Manage Extensions*, restart GIMP and repeat step 2.
- **Uninstall:** Loupedeck software → plugin list → *Uninstall*; GIMP → *Edit → Manage Extensions* → remove "Loupedeck Bridge".

## Actions

- **GIMP Value** (turn = step, press = GIMP default, the dial shows the real value), via the bridge plug-in:
  - Brush: Size (±5 % per tick), Opacity, Hardness, Angle, Aspect Ratio, Spacing, Force
  - Colors: Foreground Hue, Saturation, Value
- **GIMP Value Step**: the same as +/−/reset buttons for devices without dials
- **GIMP Dial** (GIMP default shortcuts): Zoom (press = fit), Select Layer (press = top), Undo / Redo
- **GIMP Shortcut**: tools, selection, colors, undo/redo, cut/copy/paste, layers, zoom, save/export

## How it works

- **Values** go through a small GIMP 3 Python plug-in (`gimp-plugin/loupedeck-bridge`, installed as extension in step 2).
  GIMP starts it in the background like its own Script-Fu extension; it listens on `127.0.0.1:47862` and sets the
  values with GIMP's plug-in API, so the tool options update and the dial can show the real value.
- **Security:** on every start the GIMP plug-in writes a random token to a file only your user can read
  (macOS `~/Library/Application Support/loupedeck-gimp-bridge/token`, Windows `%LOCALAPPDATA%\loupedeck-gimp-bridge\token`).
  A connection must send it first, otherwise it is closed; so browsers and other users can't use the bridge.
  The protocol can only read and set the listed values (no files, no scripts); lines and connections are limited.
- **Everything else** uses GIMP's own default shortcuts, sent only while GIMP is the active app, with the active
  keyboard layout (QWERTZ, AZERTY, …).
- The plugin never changes GIMP's settings or shortcuts.

## Build

Needs the .NET 8 SDK and the LogiPluginTool dotnet tool (provides the net8 `PluginApi.dll`):

```
dotnet tool install --global LogiPluginTool --version 6.1.4.22672
dotnet build src                     # Debug: registers bin/Debug via a .link file for the Logi Plugin Service
dotnet build src -c Release
logiplugintool pack bin/Release Gimp_0_1.lplug4
```

The build also packs the GIMP extension to `bin/<Configuration>/gimp-plugin/io.github.nick11.loupedeck-bridge.gex`.

## License

[MIT](LICENSE). Third-party assets and their licenses are listed in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
