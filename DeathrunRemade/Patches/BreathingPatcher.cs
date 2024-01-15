using DeathrunRemade.Configuration;
using DeathrunRemade.Items;
using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Enums;
using HarmonyLib;
using UnityEngine;

namespace DeathrunRemade.Patches
{
    /// <summary>
    /// Poison the surface air and make it difficult to breathe in general. You know, just like real life.
    /// </summary>
    [HarmonyPatch]
    internal class BreathingPatcher
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

            return CanBreathe(Player.main, SaveData.Main.Config);
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

            __result = CanBreathe(__instance, SaveData.Main.Config);
        }

        /// <summary>
        /// Ensure the player loses oxygen while at the surface.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.GetBreathPeriod))]
        private static void SubtractOxygen(ref Player __instance, ref float __result)
        {
            if (!CanBreathe(__instance, SaveData.Main.Config))
                __result = 3f;
        }

        /// <summary>
        /// The "swim to surface" message is a bit weird when the surface is poisoned and doesn't make much sense
        /// at 300m down.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(HintSwimToSurface), nameof(HintSwimToSurface.OnLanguageChanged))]
        private static void SwimToSurfaceText(ref HintSwimToSurface __instance)
        {
            __instance.message = "Out of Air!";
        }
        
        /// <summary>
        /// Checks whether the player is able to breathe at their current location.
        /// </summary>
        private static bool CanBreathe(Player player, ConfigSave config)
        {
            // Special cases like player bases, vehicles or alien bases should always be breathable.
            if (player.IsInsidePoweredSubOrVehicle() || player.precursorOutOfWater)
                return true;
            
            // If at the surface, check for irradiated air.
            return IsAirBreathable(config);
        }

        /// <summary>
        /// Checks whether the player would be able to breathe at the surface.
        /// </summary>
        private static bool IsAirBreathable(ConfigSave config)
        {
            if (config.SurfaceAir == Difficulty3.Normal)
                return true;

            // If this doesn't pass the game is not yet done loading.
            if (Inventory.main == null || Inventory.main.equipment == null)
                return true;
            if (Inventory.main.equipment.GetCount(FilterChip.TechType) > 0)
                return true;
            
            // Special case: Aurora is breathable after fixing the generator.
            if (IsInBreathableAurora())
                return true;

            // Surface air without a filter is always unbreathable on high difficulties.
            if (config.SurfaceAir == Difficulty3.Deathrun)
                return false;
            return !RadiationPatcher.IsSurfaceIrradiated();
        }

        /// <summary>
        /// Checks whether the player is in a section of the Aurora with breathable air.
        /// </summary>
        private static bool IsInBreathableAurora()
        {
            if (CrashedShipAmbientSound.main == null || CrashedShipExploder.main == null || !CrashedShipExploder.main.IsExploded())
                return false;
            
            // Could split this up room by room but for now assume the entire ship is breathable after fixing the core.
            return CrashedShipAmbientSound.main.isPlayerInside && RadiationPatcher.IsRadiationFixed();
        }
    }
}