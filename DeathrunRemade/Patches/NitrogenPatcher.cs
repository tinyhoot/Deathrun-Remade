using DeathrunRemade.Components;
using DeathrunRemade.Handlers;
using DeathrunRemade.Objects.Attributes;
using DeathrunRemade.Objects.Enums;
using HarmonyLib;
using UnityEngine;

namespace DeathrunRemade.Patches
{
    /// <summary>
    /// This patcher does not handle nitrogen by itself but it does deal with some important interactions and edge cases
    /// which cannot be handled by <see cref="Handlers.NitrogenHandler"/> without harmony patching.
    /// </summary>
    [HarmonyPatch]
    [PatchCategory(ApplyPatch.Config)]
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
            
            Player.main.GetComponent<NitrogenHandler>().RemoveNitrogen(__instance.oxygenPerSecond * Time.deltaTime);
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
            PauseNitrogen();
        }
        
        /// <summary>
        /// Restore nitrogen functionality after the elevator/teleport animation has ended.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(VFXPrecursorGunElevator), nameof(VFXPrecursorGunElevator.OnPlayerCinematicModeEnd))]
        [HarmonyPatch(typeof(Player), nameof(Player.CompleteTeleportation))]
        private static void RestoreNitrogenAfterCinematic()
        {
            EnableNitrogen();
        }

        /// <summary>
        /// Pause and reset nitrogen on teleporter use so the player doesn't die teleporting up from the prison.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PrecursorTeleporter), nameof(PrecursorTeleporter.BeginTeleportPlayer))]
        private static void PauseNitrogenOnTeleport(GameObject teleportObject)
        {
            // Run for players and players in vehicles, but not e.g. juvenile sea emperors.
            if (teleportObject.GetComponent<Player>() || (teleportObject.GetComponent<Vehicle>()
                                                          && Player.main.GetVehicle()))
                PauseNitrogen();
        }

        private static void PauseNitrogen()
        {
            Player player = Player.main;
            player.GetComponent<FastAscent>().enabled = false;
            var nitrogen = player.GetComponent<NitrogenHandler>();
            nitrogen.ResetNitrogen();
            nitrogen.enabled = false;
        }

        private static void EnableNitrogen()
        {
            Player player = Player.main;
            player.GetComponent<FastAscent>().enabled = true;
            player.GetComponent<NitrogenHandler>().enabled = true;
        }
    }
}