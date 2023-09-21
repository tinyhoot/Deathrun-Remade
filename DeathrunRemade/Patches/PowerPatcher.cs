using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Enums;
using HarmonyLib;
using UnityEngine;

namespace DeathrunRemade.Patches
{
    [HarmonyPatch]
    internal class PowerPatcher
    {
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
            amount = ModifyConsumeEnergy(amount, IsInRadiation(powerRelay));
            if (powerRelay.GetPower() < amount)
            {
                DeathrunInit._Log.InGameMessage($"Not enough power (need {amount})");
                __result = false;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check whether the given object is in any radiation.
        /// </summary>
        private static bool IsInRadiation(Transform transform)
        {
            if (transform is null)
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
            float mult;
            // Slightly higher multiplier in radiation.
            if (radiation)
                mult = SaveData.Main.Config.PowerCosts switch
                {
                    Difficulty4.Hard => 3f,
                    Difficulty4.Deathrun => 5f,
                    Difficulty4.Kharaa => 5f,
                    _ => 1f
                };
            else
                mult = SaveData.Main.Config.PowerCosts switch
                {
                    Difficulty4.Hard => 2f,
                    Difficulty4.Deathrun => 3f,
                    Difficulty4.Kharaa => 3f,
                    _ => 1f
                };

            return energy * mult;
        }
    }
}