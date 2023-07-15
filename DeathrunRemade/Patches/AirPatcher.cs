using DeathrunRemade.Configuration;
using HarmonyLib;
using UnityEngine;

namespace DeathrunRemade.Patches
{
    /// <summary>
    /// Poison the surface air and make it difficult to breathe in general. You know, just like real life.
    /// </summary>
    [HarmonyPatch]
    internal class AirPatcher
    {
        /// <summary>
        /// Cancel adding oxygen at the surface if the air is not breathable.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(OxygenManager), nameof(OxygenManager.AddOxygenAtSurface))]
        private static bool CancelOxygenAtSurface(ref OxygenManager __instance)
        {
            // Only for the player.
            if (!Player.main.oxygenMgr == __instance)
                return true;

            return ConfigUtils.CanBreathe(Player.main);
        }

        /// <summary>
        /// If the air is not breathable, notify the player and play a choking sound.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(WaterAmbience), nameof(WaterAmbience.PlayReachSurfaceSound))]
        private static bool NotifySurfacePoisoned(ref WaterAmbience __instance)
        {
            if (Player.main.CanBreathe())
                return true;
            // Don't notify when inside a powered-down vehicle or an alien base.
            if (Player.main.IsInsideSubOrVehicle() || Player.main.motorMode == Player.MotorMode.Walk)
                return true;

            // TODO: More detailed warnings depending on config.
            DeathrunInit._Log.InGameMessage("The surface air is unbreathable!");
            PlayerDamageSounds sounds = Player.main.GetComponent<PlayerDamageSounds>();
            if (sounds != null)
                sounds.painSmoke.Play();
            __instance.timeReachSurfaceSoundPlayed = Time.time;
            
            return false;
        }

        /// <summary>
        /// Ensure the player loses oxygen while at the surface.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.CanBreathe))]
        private static void DoubleCheckCanBreathe(ref Player __instance, ref bool __result)
        {
            if (!__result)
                return;

            __result = ConfigUtils.CanBreathe(__instance);
        }

        /// <summary>
        /// Ensure the player loses oxygen while at the surface.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.GetBreathPeriod))]
        private static void SubtractOxygen(ref Player __instance, ref float __result)
        {
            if (!ConfigUtils.CanBreathe(__instance))
                __result = 3f;
        }

        /// <summary>
        /// The "swim to surface" message is a bit weird when the surface is poisoned and doesn't make much sense
        /// at 300m down.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(uGUI_PopupMessage), nameof(uGUI_PopupMessage.SetText))]
        private static void SwimToSurfaceText(ref string message)
        {
            if (Language.main.Get("SwimToSurface").Equals(message))
            {
                if (!ConfigUtils.IsAirBreathable() || Player.main.GetDepth() > 100)
                    message = "Out of Air!";
            }
        }
    }
}