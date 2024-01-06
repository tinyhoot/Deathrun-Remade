using DeathrunRemade.Handlers;
using HarmonyLib;
using UnityEngine;

namespace DeathrunRemade.Patches
{
    /// <summary>
    /// This patcher does not handle nitrogen by itself but it does deal with some important interactions and edge cases
    /// which cannot be handled by <see cref="Handlers.NitrogenHandler"/> without harmony patching.
    /// </summary>
    [HarmonyPatch]
    internal static class NitrogenPatcher
    {
        /// <summary>
        /// Dissipate nitrogen while the player is within range of an active oxygen pipe.
        /// This is an update-like function which seems to run roughly ten times per second.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(OxygenArea), nameof(OxygenArea.OnTriggerStay))]
        private static void DecreaseNitrogenAtPipe(OxygenArea __instance, Collider other)
        {
            // Ensure this does not run when a random fish swims past.
            if (other.gameObject.FindAncestor<Player>() == null)
                return;
            
            NitrogenHandler.Main.RemoveNitrogen(__instance.oxygenPerSecond * Time.deltaTime);
        }
        
        /// <summary>
        /// Reset safe depth and pause nitrogen functionality when an elevator is used so that the player does not
        /// get the bends.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(VFXPrecursorGunElevator), nameof(VFXPrecursorGunElevator.OnGunElevatorAscendStart))]
        [HarmonyPatch(typeof(VFXPrecursorGunElevator), nameof(VFXPrecursorGunElevator.OnGunElevatorDecendStart))]
        private static void PauseNitrogenOnElevatorUse()
        {
            NitrogenHandler.Main.ResetNitrogen();
            NitrogenHandler.Main.enabled = false;
        }
        
        /// <summary>
        /// Restore nitrogen functionality after the elevator animation has ended.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(VFXPrecursorGunElevator), nameof(VFXPrecursorGunElevator.OnPlayerCinematicModeEnd))]
        private static void RestoreNitrogenAfterElevatorUse()
        {
            NitrogenHandler.Main.enabled = true;
        }
        
        /// <summary>
        /// Reset safe depth when a teleporter is used so that the player does not get the bends.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PrecursorTeleporter), nameof(PrecursorTeleporter.OnActivateTeleporter))]
        private static void ResetNitrogenOnTeleporterUse()
        {
            NitrogenHandler.Main.ResetNitrogen();
        }
    }
}