# Mods for The King's Bird

## Features

### TasBird

- Move and zoom the camera with the mouse.
- Speed up/slow down/pause the game, or step through one frame at a time.
- Shows debug data including position, velocity and internal timers.
- Draws deathzones, entity hitboxes and exact wall boundaries.
- TCP Server to communicate with external tools.

## Installation

1. Download BepInEx_x86 from the [BepInEx releases page](https://github.com/BepInEx/BepInEx/releases).
2. Extract it into the game folder so that `BepInEx/` is beside `TheKingsBird_Data/`.
3. Run the game once to let BepInEx set itself up.
4. Download `TasBird.dll` from the releases page and put it in `BepInEx/plugins/`.
5. (optional) Install the [ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager/releases)
   plugin to get a config menu bound to F1.

## Development

1. Install BepInEx as described above.
2. Copy `BepInEx/core/*.dll` and `TheKingsBird_Data/Managed/*.dll` into `Lib/`.
3. Set `Logging.Console.Enabled = true` in `BenInEx/config/BenInEx.cfg` to help with debugging.
4. (optional) Install the [ScriptEngine](https://github.com/BepInEx/BepInEx.Debug/releases) plugin to hot-load mods
   placed in `BepInEx/scripts/` by pressing F6.
