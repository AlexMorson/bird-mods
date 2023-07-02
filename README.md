# Mods for The King's Bird

## Features

### Bugfixes

- Fix the camera sometimes jumping wildly.
- Fix the quality settings not being saved.

### TasBird

- Move and zoom the camera with the mouse.
- Speed up/slow down/pause the game, or step through one frame at a time.
- Shows debug data including position, velocity and internal timers.
- Draws deathzones, entity hitboxes and exact wall boundaries.
- TCP Server to communicate with external tools.
- Input display.

## Installation

1. Download BepInEx\_x86 from the [BepInEx releases page](https://github.com/BepInEx/BepInEx/releases). (Note: `x86`, not `x64`!)
2. Extract the zip into the game folder so that `BepInEx/`, `doorstop_config.ini` and `winhttp.dll` are in the same place as `TheKingsBird.exe`.
3. Run the game once to let BepInEx set itself up. This will create `BepInEx/LogOutput.txt` if successful.
4. Download the desired mod's `dll` from the [releases page](https://github.com/AlexMorson/bird-mods/releases) and put it in `BepInEx/plugins/`.
5. (optional) Install the [ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager/releases) plugin to get a config menu bound to F1.

## Development

1. Install BepInEx as described above.
2. Clone the `bird-mods` repository.
3. Copy `BepInEx/core/*.dll` and `TheKingsBird_Data/Managed/*.dll` into `bird-mods/Lib/`.
4. Download [NStrip](https://github.com/BepInEx/NStrip/releases) and run `NStrip.exe -n -o -p Lib/Assembly-CSharp.dll` to make all types, methods, properties and fields public.
5. Set `Logging.Console.Enabled = true` in `BepInEx/config/BepInEx.cfg` to help with debugging.
6. Install the [ScriptEngine](https://github.com/BepInEx/BepInEx.Debug/releases) plugin to hot-load mods placed in `BepInEx/scripts/` by pressing F6. This avoids having to restart the game to test changes.
