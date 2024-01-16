using System.Collections.Generic;
using DeathrunRemade.Items;
using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Enums;
using HarmonyLib;

namespace DeathrunRemade.Patches
{
    [HarmonyPatch]
    internal static class BatteryPatcher
    {
        /// <summary>
        /// Make sure vehicles with visible powercells show the model when a different cell-like battery is in the slot.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnergyMixin), nameof(EnergyMixin.NotifyHasBattery))]
        private static void EnsureVisiblePowerCell(ref EnergyMixin __instance, InventoryItem item)
        {
            // Additional null check because unity lifetime check shenanigans.
            if (item?.item == null)
                return;
            if (item.item.GetTechType().Equals(AcidPowerCell.TechType))
                __instance.batteryModels[0].model.SetActive(true);
        }

        /// <summary>
        /// Ensure that all tools and vehicles spawn without batteries and cells.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(EnergyMixin), nameof(EnergyMixin.OnCraftEnd))]
        private static void SpawnWithoutBattery(ref EnergyMixin __instance, TechType techType)
        {
            // Don't apply this patch on low difficulty levels.
            if (SaveData.Main.Config.BatteryCosts == Difficulty4.Normal)
                return;

            if (techType != TechType.MapRoomCamera)
            {
                __instance.defaultBattery = TechType.None;
                __instance.battery = null;
            }
        }
        
        /// <summary>
        /// Make sure custom powercells are recognised as valid by the charger.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PowerCellCharger), nameof(PowerCellCharger.Initialize))]
        private static void UpdatePowerCellCharger(ref PowerCellCharger __instance)
        {
            HashSet<TechType> compatibleTech = PowerCellCharger.compatibleTech;
            // It's a hashset, checking whether the techtype already exists is superfluous.
            compatibleTech.Add(AcidPowerCell.TechType);
        }

        /// <summary>
        /// Necessary to allow our custom batteries to be put into things with battery slots.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(EnergyMixin), nameof(EnergyMixin.Awake))]
        private static void UpdateValidBatteries(ref EnergyMixin __instance)
        {
            if (!__instance.allowBatteryReplacement)
                return;

            if (__instance.compatibleBatteries.Contains(TechType.Battery)
                || __instance.compatibleBatteries.Contains(TechType.PrecursorIonBattery))
            {
                __instance.compatibleBatteries.Add(AcidBattery.TechType);
            }

            if (__instance.compatibleBatteries.Contains(TechType.PowerCell)
                || __instance.compatibleBatteries.Contains(TechType.PrecursorIonPowerCell))
            {
                __instance.compatibleBatteries.Add(AcidPowerCell.TechType);
            }
        }
    }
}