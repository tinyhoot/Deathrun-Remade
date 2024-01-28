# Deathrun Remade

A complete rewrite of the original [DeathRun by Cattlesquat](https://github.com/Cattlesquat/subnautica) for [Nautilus](https://github.com/SubnauticaModding/Nautilus)
and the latest version of Subnautica.

## Progress

The rewrite is in its playtesting stage and nearing completion.

## Building the Project

- Clone this repository including the submodule using `git clone --recurse-submodules`
- NuGet should automatically download all dependencies for you. If it does not, perform a NuGet restore.
- Create a `GameDirectory.targets` file in the same folder as the freshly cloned `DeathrunRemade.csproj` and add the path to your 
  local Subnautica install directory to it. An example file can be found [here](https://github.com/tinyhoot/HootLib-Subnautica/blob/main/HootLib/Example_GameDirectory.targets).
- Building the project will leave you with a `DeathrunRemade.dll` in the default build directory and automatically 
  copy all necessary mod files to your `Subnautica/BepInEx/plugins` directory for quick testing.

## For Modders: Initialisation Timeline

Deathrun Remade uses a *lot* of non-traditional timing for when it registers its changes. This overview is intended to
help other modders interact with Deathrun, be it through the API, Nautilus, or Harmony patches.

- Plugin `Awake()`
  - Custom items have their TechTypes registered with Nautilus.
  - Persistent data which does not change depending on settings is registered with Nautilus, such as PDA encyclopedia entries.
  - Systems underlying the API are initialised.
  - Harmony patches that are always necessary are applied.
- Plugin `Start()`
  - The API is ready and safe to use.
- Main Menu loads.
- User presses Play and starts/loads a game.
  - Event for clicking Play is invoked.
    - All config-specific harmony patches are unpatched to get a clean slate for the setup in the next steps.
    - All prefabs registered with Nautilus are deregistered (but TechTypes persist).
  - `SaveDataCache` loads.
    - The config locks in and can no longer be changed.
    - Config-specific harmony patches are applied.
    - Changes to recipes, fragments, etc. are registered with Nautilus.
    - Prefabs for custom items are registered.
  - Player `Awake()`
    - MonoBehaviours are added to `Player.main`.
    - UI elements are instantiated and set up.
  - Main Scene loads; systems like `PDAScanner` awake.
    - Caches like the fragment scan number cache are updated with vanilla state *before* Nautilus applies its modifications.
  - Most of Nautilus' patchers activate.
  - EscapePod `Awake()`
    - MonoBehaviours specific to the lifepod are attached to it.
  - The 'DEATHRUN' info on which start was chosen appears on the loading screen.
  - The world loads.
- The loading screen finishes and the player gains control over their character.
  - Vanilla GUI elements are copied and repurposed.
  - Radiation events are registered.
- On quit, repeat from Main Menu load.

## Credits

- ["Anticlockwise Rotation Icon"](https://game-icons.net/1x1/delapouite/anticlockwise-rotation.html) by Delapouite under [CC-BY-3.0](https://creativecommons.org/licenses/by/3.0/)
- ["Cancel Icon"](https://game-icons.net/1x1/sbed/cancel.html) by sbed under [CC-BY-3.0](https://creativecommons.org/licenses/by/3.0/)
- ["Trash Can Icon"](https://game-icons.net/1x1/delapouite/trash-can.html) by Delapouite under [CC-BY-3.0](https://creativecommons.org/licenses/by/3.0/)