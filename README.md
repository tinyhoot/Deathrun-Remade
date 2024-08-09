# Deathrun Remade

![GitHub License](https://img.shields.io/github/license/tinyhoot/Deathrun-Remade)
![wakatime](https://wakatime.com/badge/user/d7c60741-27ca-486e-a1d0-e23b93d91114/project/d4482193-e257-4c33-86fd-9b49e09c932c.svg)

A complete rewrite of the original [DeathRun by Cattlesquat](https://github.com/Cattlesquat/subnautica) for [Nautilus](https://github.com/SubnauticaModding/Nautilus)
and the latest version of Subnautica.

## Installation

- Install [BepInEx](https://www.nexusmods.com/subnautica/mods/1108).
- Install [Nautilus](https://www.nexusmods.com/subnautica/mods/1262).
- Unzip this mod into your `Subnautica/BepInEx` directory.
- Enjoy!

## Compatibility

Due to the many, many issues caused by infighting between Nautilus and SMLHelper, Deathrun Remade will refuse to load if
SMLHelper is present.

## Features

Deathrun Remade is a roguelike difficulty mod that aims to make the game challenging for anyone who felt that the
vanilla experience was too easy. It introduces new mechanics like decompression sickness, expands on overlooked vanilla
items like the floating air pump, and enhances the threat posed by most vanilla mechanics. This remake retains all the features of
the original while occasionally expanding on it with extra options or difficulty levels.

Deathrun is hard. Your first few runs most likely will not get far, and that is okay! The point is not to win, but to
see just how far you can make it, and then come up with plans to make it even further! The mod features an in-depth 
scoring system that rewards you for surviving longer,
achieving more, and challenging yourself with harder difficulty settings.

Almost every part of Deathrun is configurable. Jump right in with the carefully balanced default settings or take control
of *your* experience with detailed config options. Be careful though: once you start a run, you can no longer change its
difficulty settings!

If you want a spoiler-free experience, stop reading now and jump right in! Otherwise, feel free to read through the
list of features below.

### Poisonous Atmosphere

It would be awfully convenient if you happened to crash on an alien planet with a breathable atmosphere. Even more awful
that you didn't! With Deathrun, the atmosphere is poisonous and must first be filtered through a Floating Air Pump to
be breathable.

### Nitrogen and 'the Bends'

Deathrun adds a new mechanic in decompression sickness, otherwise known as the Bends. At the top of your screen, you'll
find a new depth meter displaying your current *Safe Depth*. Your safe depth will even out at around 3/4 of your current
depth over time. If you ascend too quickly, you'll exceed your current safe depth and start taking serious damage from
the Bends. Even if you're well below your current safe depth coming up too quickly will cause problems for you, so be sure
to take regular breaks.

Pay attention to item tooltips and databank entries. Some equipment and wildlife can help you overcome this challenge.

### Failing Lifepod

The lifepod's flotation systems fail, and it sinks to the ocean floor with you where it falls over at an awkward angle.
You may be able to return it to its upright position if you repair its secondary systems, but the pod will stay sunk!

### Lethal Quantum Detonation

Deathrun's first hour is a race against time. The detonation of the Aurora's drive core *will* kill you if you do not
manage to take shelter somewhere deep and inside in time.

### Radioactive Fallout

You thought you're safe after the explosion? The entire surface and much of the upper levels of the ocean will turn
radioactive, making a radiation suit that much more crucial. Repair the leaks and hope the radiation dissipates in time.

### Personal Crush Depth

Just like your precious Seamoth you now have a crush depth. Dive too deep and the ocean will squeeze the life out of you.
Deep wildlife may hold the answer to finding ways to survive the depths below.

### Aggressive Wildlife

Creatures on 4546B have become much more aggressive, and it's getting worse still! They will gradually ramp up their
aggression levels over the first 40 minutes of play, chasing you for longer, attacking you more often, and spotting you
from further away. It is inadvisable to remain in one place for long.

### Increased Damage

Think twice before you try to wrangle a reaper, as it won't go quite as easy on you anymore! Damage from most sources in
the game has been increased. Core mechanics like the Bends will not one-shot you — but if you get munched
on by a leviathan, you're on your own!

## Differences to the Original

If you were familiar with the legacy version of Deathrun, here's what's changed:
- A completely custom implementation of nitrogen without relying on the game. Some of the math and speeds is different
  as a result.
- Damage from the Aurora explosion is dealt at the time the shockwave hits you; the shockwave has a physical force and
  damages everything else (bases, fish, ...) too.
- Improved UI and in-game mod menu.
- Save data is now per save slot. Deathrun no longer touches your vanilla saves. This also improves compatibility with
  autosave mods.
- Much better compatibility with other mods due to less intrusive patching.
- The mod can be translated simply by adding a localisation file to the `DeathrunRemade/Assets/Localization` folder.
- Pacifist challenge covers many more ways to damage things, not just the knife.
- Photosynthetic tanks always work in sunlight, including the surface.
- Re-introduces a patch that lets you deploy filterpumps from the water surface.
- Scoring system is reworked. Legacy runs can be imported, but will show different scores.
- The scanner room does not passively drain power.

## API

This mod features an [API](https://github.com/tinyhoot/Deathrun-Remade/blob/main/DeathrunRemade/DeathrunAPI.cs) for other
mods to interact with. If you are a mod author and would like to interface with Deathrun in a way that the API currently
does not cover, feel free to submit an issue or pull request.

## For Modders: Building the Project

- Clone this repository including the submodule using `git clone --recurse-submodules`
- NuGet should automatically download all dependencies for you. If it does not, perform a NuGet restore.
- Create a `GameDirectory.targets` file in the same folder as the freshly cloned `DeathrunRemade.csproj` and add the path to your 
  local Subnautica install directory to it. An example file can be found [here](https://github.com/tinyhoot/HootLib-Subnautica/blob/main/HootLib/Example_GameDirectory.targets).
- Building the project will leave you with a `DeathrunRemade.dll` in the default build directory and automatically 
  copy all necessary mod files to your `Subnautica/BepInEx/plugins` directory for quick testing.

## For Modders: Initialisation Timeline

Deathrun Remade uses a *lot* of non-traditional timing for when it registers its changes. This is necessary in order to
keep the mod from breaking on return to main menu and enables it to be as modular as it is. This overview is intended to
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
  - Event for clicking Play is invoked; The `Reset` event fires.
    - All config-specific harmony patches are unpatched to get a clean slate for the setup in the next steps.
    - All prefabs registered with Nautilus are deregistered (but TechTypes persist).
    - Any run-specific changes previously sent to Nautilus (such as recipes) are undone.
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

Many thanks go to
- MrPurple6411 for his contributions to the API and the kick to get Deathrun working across save games
- Cattlesquat for his incredible work with the original version
- The veritable squadron of testers in the Subnautica Modding Discord I've inflicted untold horrors on in the form of terrible bugs

- ["Anticlockwise Rotation Icon"](https://game-icons.net/1x1/delapouite/anticlockwise-rotation.html) by Delapouite under [CC-BY-3.0](https://creativecommons.org/licenses/by/3.0/)
- ["Cancel Icon"](https://game-icons.net/1x1/sbed/cancel.html) by sbed under [CC-BY-3.0](https://creativecommons.org/licenses/by/3.0/)
- ["Trash Can Icon"](https://game-icons.net/1x1/delapouite/trash-can.html) by Delapouite under [CC-BY-3.0](https://creativecommons.org/licenses/by/3.0/)
