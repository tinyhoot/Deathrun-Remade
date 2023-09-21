using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Enums;
using HarmonyLib;
using UnityEngine;

namespace DeathrunRemade.Patches
{
    [HarmonyPatch]
    internal class WaterMurkPatcher
    {
        /// <summary>
        /// Increase the murkiness of the water based on config options, decreasing visibility.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(WaterscapeVolume.Settings), nameof(WaterscapeVolume.Settings.GetExtinctionAndScatteringCoefficients))]
        private static void MakeWaterMurky(ref Vector4 __result)
        {
            float murkMult = SaveData.Main.Config.WaterMurkiness switch
            {
                Murkiness.Clear => 0.5f,
                Murkiness.Dark => 1.5f,
                Murkiness.Darker => 2f,
                Murkiness.Darkest => 5f,
                _ => 1f
            };
            __result *= murkMult;
        }
    }
}