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