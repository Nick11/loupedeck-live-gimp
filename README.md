# GIMP Controls for Loupedeck (unofficial)

Plugin for the Logi Actions SDK (Loupedeck software / Logi Options+) to control GIMP 3 with the Loupedeck Live:
brush and color values on dials that show GIMP's real value, plus GIMP's shortcuts on buttons.

> This is an unofficial community plugin, not affiliated with or endorsed by the GIMP project or Logitech.
> It was generated with AI.

## Installation

**Requires:** Loupedeck Live (or another Loupedeck / MX Creative device) · Loupedeck software ≥ 6.3 or Logi Options+ ·
GIMP ≥ 3.0 with Python (macOS: included; Windows: keep the installer's "Python" option on) · macOS or Windows 11

### 1. Loupedeck plugin

1. Double-click `Gimp_<version>.lplug4` (GitHub *Releases*). On first install the Loupedeck Live gets a starter GIMP
   profile (brush size, opacity, hardness); add more actions in the Loupedeck software.
2. **macOS only:** System Settings → Privacy & Security → *Accessibility* → enable **LogiPluginService** (needed for the shortcut buttons).

### 2. GIMP plug-in (separate step, once per GIMP version)

The value dials need the small bridge plug-in `loupedeck-bridge.zip` (GitHub *Releases*). It is a regular GIMP plug-in,
installed the way the [GIMP manual](https://docs.gimp.org/en/gimp-scripting.html) describes; GIMP's settings and
shortcuts stay untouched.

1. Unzip `loupedeck-bridge.zip`. You get a folder `loupedeck-bridge` with `loupedeck-bridge.py` inside.
2. Open GIMP's plug-in folder for your user. GIMP shows it under *Edit → Preferences → Folders → Plug-ins*; the
   `<version>` part is your GIMP's major.minor version (e.g. `3.0`, `3.2`):
   - **Windows:** paste `%APPDATA%\GIMP\<version>\plug-ins` into the Explorer address bar.
   - **macOS:** Finder → *Go → Go to Folder…* (⇧⌘G) → `~/Library/Application Support/GIMP/<version>/plug-ins`.

   If the `plug-ins` folder doesn't exist yet, start and quit GIMP once, or create it.
3. Copy the folder `loupedeck-bridge` into it, so you have `plug-ins/loupedeck-bridge/loupedeck-bridge.py`.
4. Restart GIMP. A value dial (e.g. *GIMP Value → Brush: Size*) now shows GIMP's value instead of "–".

### Troubleshooting

- **Dials show "–":** GIMP isn't running, the plug-in folder is missing, not in the folder of this GIMP version or not named
  `loupedeck-bridge`, or GIMP wasn't restarted. On Windows, GIMP must be installed with Python (installer default).
- **Buttons not reacting:** GIMP must be in front with the canvas focused: click into the image once. Don't hold Shift/Ctrl/Alt/Cmd while pressing.
- **New GIMP version (e.g. 3.2 → 3.4):** GIMP uses a new settings folder per version; repeat step 2 there.
- **Update:** install the newer `.lplug4`; if the release notes say the GIMP plug-in changed, replace the folder (step 2)
  and restart GIMP.
- **Uninstall:** Loupedeck software → plugin list → *Uninstall*; delete the folder `plug-ins/loupedeck-bridge`.

## Actions

- **GIMP Value** (turn = step, press = GIMP default, the dial shows the real value), via the bridge plug-in:
  - Brush: Size (±5 % per tick), Opacity, Hardness, Angle, Aspect Ratio, Spacing, Force
  - Colors: Foreground Hue, Saturation, Value
- **GIMP Value Step**: the same as +/−/reset buttons for devices without dials
- **GIMP Shortcut Dial** (GIMP default shortcuts): Zoom (press = fit), Select Layer (press = top), Undo / Redo
- **GIMP Shortcut**: tools, selection, colors, undo/redo, cut/copy/paste, layers, zoom, save/export

## How it works

- **Values** go through a small GIMP 3 Python plug-in (`gimp-plugin/loupedeck-bridge`, installed in step 2).
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
logiplugintool pack bin/Release Gimp_<version>.lplug4
```

- The build also zips the GIMP plug-in to `bin/loupedeck-bridge.zip`. A release contains both files.
- Build releases on macOS or Linux (the zip must keep the script's executable bit) from a clean tree (`rm -rf bin src/obj`).
- `-p:PluginApiDir=<folder>` overrides where `PluginApi.dll` is taken from.
- Development: reload with `open loupedeck:plugin/Gimp/reload` (Windows: `start …`); the log is
  `Logs/plugin_logs/Gimp.log` in the Logi Plugin Service data folder.
- `src/package/profiles/DefaultProfile30.lp5` is the starter profile for the Loupedeck Live: build it in the Loupedeck
  software and export it, don't edit it by hand. `assets/PluginIcon.svg` is the source of `metadata/Icon256x256.png`.

## License

[MIT](LICENSE). Third-party assets and their licenses are listed in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
