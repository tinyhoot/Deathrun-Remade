using System.Collections.Generic;
using System.Reflection.Emit;
using DeathrunRemade.Configuration;
using DeathrunRemade.Handlers;
using DeathrunRemade.Items;
using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Attributes;
using DeathrunRemade.Objects.Enums;
using HarmonyLib;
using UnityEngine;

namespace DeathrunRemade.Patches
{
    /// <summary>
    /// Poison the surface air and make it difficult to breathe in general. You know, just like real life.
    /// </summary>
    [HarmonyPatch]
    [PatchCategory(ApplyPatch.Config)]
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
            
            WarningHandler.ShowWarning(Warning.UnbreathableAir);
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
        /// For some reason all oxygen from other sources is ignored if the tool is currently drawn. Disabling this
        /// behaviour makes the air bladder work with air bubbles from the bubble plants or filtration pumps.
        /// </summary>
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(AirBladder), nameof(AirBladder.AddOxygen))]
        private static IEnumerable<CodeInstruction> EnableRefillWhenDrawn(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions);

            matcher.MatchForward(false,
                    new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(PlayerTool), nameof(PlayerTool.isDrawn))))
                .ThrowIfInvalid("Failed to find AddOxygen isDrawn call!")
                // Replace the call to the isDrawn property with a simple "false", which always allows oxygen to work.
                .SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_0))
                .SetInstructionAndAdvance(new CodeInstruction(OpCodes.Nop));

            return matcher.InstructionEnumeration();
        }

        /// <summary>
        /// In vanilla, air bladders refill instantly when at the surface. Delete this behaviour and insert a call
        /// to our own function to replace it.
        /// </summary>
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(AirBladder), nameof(AirBladder.UpdateInflateState))]
        private static IEnumerable<CodeInstruction> ReplaceRefill(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions);

            // Find the if statement where an empty bladder should get refilled when not underwater.
            matcher.MatchForward(false,
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(CodeInstruction.LoadField(typeof(AirBladder), nameof(AirBladder.inflate))),
                    new CodeMatch(OpCodes.Ldc_I4_0),
                    new CodeMatch(OpCodes.Callvirt),
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(CodeInstruction.LoadField(typeof(AirBladder), nameof(AirBladder.inflate))))
                .ThrowIfInvalid("Failed to find airbladder refill instructions!");
            
            // Delete the entire branch of this if statement, wiping the current behaviour for refilling.
            // Nop instructions are preferred over straight-up deleting to keep the length of the function the same
            // in case other mods use that in some way.
            while (matcher.Opcode != OpCodes.Ret)
            {
                matcher.SetInstruction(new CodeInstruction(OpCodes.Nop));
                matcher.Advance(1);
            }

            // Get back to the start of these Nop instructions and replace the first two with a call to our own
            // refilling logic.
            matcher
                .Start()
                .MatchForward(false,
                    new CodeMatch(OpCodes.Nop),
                    new CodeMatch(OpCodes.Nop),
                    new CodeMatch(OpCodes.Nop))
                .ThrowIfInvalid("Failed to find airbladder replaced nop instructions!")
                .SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
                .SetInstructionAndAdvance(CodeInstruction.Call(typeof(BreathingPatcher), nameof(RefillAirBladder)));

            return matcher.InstructionEnumeration();
        }

        private static void RefillAirBladder(AirBladder bladder)
        {
            // Refuse to fill the air bladder in a poisonous atmosphere.
            if (!CanBreathe(Player.main, SaveData.Main.Config))
                return;
            
            // Start playing the inflating sound.
            if (!bladder.inflate.playing)
                bladder.inflate.Play();
            
            // Mirrored from the original method.
            bladder.deflating = false;
            bladder.applyBuoyancy = false;
            
            // Instead of this, see the other air bladder transpiler above which enables OxygenManager.AddOxygen().
            // bladder.oxygen += 30f * Time.deltaTime;
            // if (bladder.oxygen > bladder.oxygenCapacity)
            //     bladder.oxygen = bladder.oxygenCapacity;
            bladder.animator.SetFloat(AirBladder.kAnimInflate, bladder.oxygen / bladder.oxygenCapacity);
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
            if (Inventory.main.equipment.GetCount(FilterChip.s_TechType) > 0)
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