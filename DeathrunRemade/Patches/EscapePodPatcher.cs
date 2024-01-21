using System.Linq;
using DeathrunRemade.Components;
using DeathrunRemade.Configuration;
using DeathrunRemade.Handlers;
using DeathrunRemade.Objects;
using HarmonyLib;
using UnityEngine;

namespace DeathrunRemade.Patches
{
    [HarmonyPatch]
    internal class EscapePodPatcher
    {
        /// <summary>
        /// Make sure the component responsible for sinking the lifepod is added to its gameobject as soon as possible.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EscapePod), nameof(EscapePod.Awake))]
        public static void PatchAwake(EscapePod __instance)
        {
            __instance.gameObject.EnsureComponent<EscapePodSinker>();
        }

        /// <summary>
        /// If this is a new game start sinking the pod once the intro is either over or it has been skipped.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EscapePod), nameof(EscapePod.StopIntroCinematic))]
        private static void SinkNewbornPod(EscapePod __instance)
        {
            __instance.gameObject.GetComponent<EscapePodSinker>().SinkPod();
        }

        /// <summary>
        /// Override the spawn location of the lifepod at the start of the game.
        /// </summary>
        /// <param name="__result">The spawnpoint chosen by the game.</param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(RandomStart), nameof(RandomStart.GetRandomStartPoint))]
        private static void OverrideStart(ref Vector3 __result)
        {
            Vector3 start = GetStartPoint(SaveData.Main.Config, out string name);
            SaveData.Main.Stats.startPoint = name;
            // Show the intro sequence.
            NotificationHandler.Main.AddMessage(NotificationHandler.Centre, $"DEATHRUN\nStart: {name}")
                .SetDuration(10f, 2f);
            
            // If the setting was on Vanilla, do not override anything.
            if (start == default)
                return;
            
            DeathrunInit._Log.Debug($"Replacing spawn point with {start}");
            __result = start;
        }

        /// <summary>
        /// Get the spawn point of the escape pod.
        /// </summary>
        /// <returns></returns>
        public static Vector3 GetStartPoint(ConfigSave config, out string name)
        {
            string setting = config.StartLocation;
            // If the save data has not yet initialised, fall back to the actual config.
            setting ??= DeathrunInit._Config.StartLocation.Value;

            // This will throw an exception if the setting name has been altered for some reason, but that's intended.
            StartLocation location = DeathrunInit._Config._startLocations.First(l => l.Name == setting);
            if (setting == "Random")
                location = DeathrunInit._Config._startLocations.Where(l => l.Name != "Random").ToList().GetRandom();

            name = location.Name;
            if (location.Name == "Vanilla")
                return default;
            return new Vector3(location.X, location.Y, location.Z);
        }
    }
}