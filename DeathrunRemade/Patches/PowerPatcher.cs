using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using DeathrunRemade.Handlers;
using DeathrunRemade.Items;
using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Attributes;
using DeathrunRemade.Objects.Enums;
using HarmonyLib;
using HootLib.Objects.Exceptions;
using UnityEngine;

namespace DeathrunRemade.Patches
{
    [HarmonyPatch]
    [PatchCategory(ApplyPatch.Config)]
    internal class PowerPatcher
    {
        // Used to keep track of which vehicle is being exited by the player.
        private static Vehicle _ejectedVehicle;
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PowerSystem), nameof(PowerSystem.AddEnergy))]
        private static void AddBaseEnergy(IPowerInterface powerInterface, ref float amount)
        {
            amount = ModifyAddEnergy(amount, IsInRadiation(powerInterface));
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PowerSystem), nameof(PowerSystem.ConsumeEnergy))]
        private static void ConsumeBaseEnergy(IPowerInterface powerInterface, ref float amount)
        {
            amount = ModifyConsumeEnergy(amount, IsInRadiation(powerInterface));
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(EnergyMixin), nameof(EnergyMixin.AddEnergy))]
        private static void AddMixinEnergy(EnergyMixin __instance, ref float amount)
        {
            amount = ModifyAddEnergy(amount, IsInRadiation(__instance.transform));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EnergyMixin), nameof(EnergyMixin.ConsumeEnergy))]
        private static void ConsumeMixinEnergy(EnergyMixin __instance, ref float amount)
        {
            amount = ModifyConsumeEnergy(amount, IsInRadiation(__instance.transform));
            // Reduce power consumption of all tools. Outside of radiation, this *almost* returns it to vanilla values.
            if (__instance.GetComponent<Pickupable>())
                amount /= 2f;
        }

        /// <summary>
        /// Solar panels modify the PowerRelay directly and do not use the PowerSystem methods we patched above.
        /// Instead, patch the amount the panel recharges directly.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SolarPanel), nameof(SolarPanel.GetRechargeScalar))]
        private static void ModifySolarPanelRecharge(SolarPanel __instance, ref float __result)
        {
            __result = ModifyAddEnergy(__result, IsInRadiation(__instance.transform));
        }

        /// <summary>
        /// In vanilla, trying to craft something when you don't have the energy available to do so fails, but
        /// consumes the energy anyway. This gets egregious when combined with the increased power costs, so stop it
        /// from happening by cancelling the craft entirely.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CrafterLogic), nameof(CrafterLogic.ConsumeEnergy))]
        private static bool PreventFabricatorConsumption(PowerRelay powerRelay, float amount, ref bool __result)
        {
            // Don't do anything when NoCost cheat is active or game mode is creative.
            if (GameModeUtils.IsCheatActive(GameModeOption.NoCost) || (GameModeUtils.currentGameMode == GameModeOption.Creative))
                return true;
            
            amount = ModifyConsumeEnergy(amount, IsInRadiation(powerRelay));
            if (powerRelay.GetPower() < amount)
            {
                NotificationHandler.VanillaMessage("dr_notEnoughCraftPower", amount);
                __result = false;
                return false;
            }

            return true;
        }

        /// <summary>
        /// The scanner room has a passive power drain even when it is not actively running. This gets extremely
        /// punishing and honestly unfair when combined with the power cost increases. This transpiler removes the
        /// inactive power cost entirely.
        /// </summary>
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(MapRoomFunctionality), nameof(MapRoomFunctionality.UpdateScanning))]
        private static IEnumerable<CodeInstruction> RemovePassiveScannerDrain(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions);
            // Find the constant used for the passive drain.
            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_R4, 0.15f));
            // This is a likely target for other mods to patch too. Only try and change anything if we actually found
            // the constant. Replacing the constant with a zero makes the scanner room drain no power.
            if (matcher.IsValid)
                matcher.Set(OpCodes.Ldc_R4, 0f);
            
            return matcher.InstructionEnumeration();
        }

        /// <summary>
        /// The postfix is incapable of determining what vehicle was just exited, so set it up right here.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), nameof(Player.TryEject))]
        private static void ConsumeVehicleExitPowerPrefix(Player __instance)
        {
            _ejectedVehicle = __instance.GetVehicle();
        }
        
        /// <summary>
        /// Consume power when the player exits a Seamoth/Prawn at depth, reduced by decompression modules.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.TryEject))]
        private static void ConsumeVehicleExitPower(Player __instance)
        {
            // Don't do anything when NoCost cheat is active.
            if (GameModeUtils.IsCheatActive(GameModeOption.NoCost))
                return;
            
            Difficulty4 difficulty = SaveData.Main.Config.VehicleExitPowerLoss;
            // If the vehicle is a Cyclops or the config says no, stop.
            if (_ejectedVehicle == null || difficulty == Difficulty4.Normal)
                return;
            // No penalty for exiting in a moonpool.
            if (PrecursorMoonPoolTrigger.inMoonpool || __instance.precursorOutOfWater)
                return;
            // No penalty for very shallow (surface) exits.
            float depth = Ocean.GetDepthOf(_ejectedVehicle.transform);
            if (depth < 5f)
                return;
            // Never punish for exiting in any of the precursor locations.
            string biome = __instance.CalculateBiome();
            if (biome.StartsWith("Precursor", StringComparison.InvariantCultureIgnoreCase)
                || biome.StartsWith("Prison", StringComparison.InvariantCultureIgnoreCase))
                return;
            
            // Can only be seamoth or prawn at this point.
            float divisor = GetVehicleExitCostDiv(difficulty, _ejectedVehicle is SeaMoth);
            // Energy cost increases with depth and difficulty. At 100 depth, exiting Seamoth costs 10 power on Deathrun.
            float energyCost = depth / divisor;
            // Flat extra cost on top, absolutely crippling at the highest levels.
            energyCost += 100f / divisor;
            
            // Reduce power cost for decompression modules.
            int modules = _ejectedVehicle.modules.GetCount(DecompressionModule.s_TechType);
            // No reduction at zero, halved at 1, no cost at 2 and above.
            energyCost -= energyCost * (modules / 2f);
            if (energyCost < 1f)
                return;
            
            // Drain the energy.
            _ejectedVehicle.energyInterface.ConsumeEnergy(energyCost);
            NotificationHandler.VanillaMessage("dr_vehicleExitPowerLoss", _ejectedVehicle.subName.GetName(),
                Mathf.FloorToInt(energyCost), Mathf.FloorToInt(depth));

            // Ensure the player understands what just happened.
            if (_ejectedVehicle is SeaMoth)
                Tutorial.SeamothVehicleExitPowerLoss.Trigger();
            else
                Tutorial.ExosuitVehicleExitPowerLoss.Trigger();
        }

        /// <summary>
        /// Get the multiplier applied to any power consumption based on difficulty and whether the location is
        /// considered in radiation.
        /// Mostly exists so the in-game mod menu can access it and display the values dynamically.
        /// </summary>
        public static float GetPowerCostMult(Difficulty4 difficulty, bool radiation)
        {
            float mult;
            if (radiation)
                mult = difficulty switch
                {
                    Difficulty4.Hard => 3f,
                    Difficulty4.Deathrun => 5f,
                    Difficulty4.Kharaa => 6f,
                    _ => 1f
                };
            else
                mult = difficulty switch
                {
                    Difficulty4.Hard => 2f,
                    Difficulty4.Deathrun => 3f,
                    Difficulty4.Kharaa => 3.5f,
                    _ => 1f
                };
            return mult;
        }

        /// <summary>
        /// Get the divisor used for calculating the power cost of exiting a vehicle at depth.
        /// </summary>
        public static float GetVehicleExitCostDiv(Difficulty4 difficulty, bool isSeaMoth)
        {
            float difficultyMult = difficulty switch
            {
                Difficulty4.Hard => 20f,
                Difficulty4.Deathrun => 10f,
                Difficulty4.Kharaa => 2.5f,
                _ => throw new ConfigEntryException($"Invalid value for {difficulty.GetType()}: {difficulty.ToString()}")
            };
            float vehicleMult = isSeaMoth ? 1f : 2f;
            return difficultyMult * vehicleMult;
        }

        /// <summary>
        /// Check whether the given object is in any radiation.
        /// </summary>
        private static bool IsInRadiation(Transform transform)
        {
            if (transform == null)
                return false;
            return RadiationPatcher.IsInRadiation(transform, SaveData.Main.Config.RadiationDepth);
        }
        
        /// <inheritdoc cref="IsInRadiation(UnityEngine.Transform)"/>
        private static bool IsInRadiation(IPowerInterface powerInterface)
        {
            Transform transform = null;
            if (powerInterface is MonoBehaviour mono)
                transform = mono.transform;
            return IsInRadiation(transform);
        }
        
        /// <summary>
        /// When gaining power through any means, modify the amount that actually ends up getting added to the power
        /// grid based on difficulty and radiation.
        /// </summary>
        private static float ModifyAddEnergy(float energy, bool radiation)
        {
            float divisor = SaveData.Main.Config.PowerCosts switch
            {
                Difficulty4.Hard => 3f,
                Difficulty4.Deathrun => 5f,
                Difficulty4.Kharaa => 6f,
                _ => 1f
            };
            // In radiation, double all power costs.
            if (radiation)
                divisor *= 2;

            return energy / divisor;
        }
        
        /// <summary>
        /// When consuming power, modify the amount that actually ends up getting consumed from the power grid
        /// based on difficulty and radiation.
        /// </summary>
        private static float ModifyConsumeEnergy(float energy, bool radiation)
        {
            float mult = GetPowerCostMult(SaveData.Main.Config.PowerCosts, radiation);
            return energy * mult;
        }
    }
}