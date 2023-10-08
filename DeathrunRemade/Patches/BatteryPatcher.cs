using System.Collections.Generic;
using System.Text;
using DeathrunRemade.Items;
using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Enums;
using HarmonyLib;
using Nautilus.Handlers;
using UnityEngine;

namespace DeathrunRemade.Patches
{
    [HarmonyPatch]
    internal static class BatteryPatcher
    {
        /// <summary>
        /// Subnautica usually does not show descriptive tooltips for batteries, which is bad since this mod introduces
        /// more differences between them. This patch re-adds the tooltip text.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(TooltipFactory), nameof(TooltipFactory.ItemCommons))]
        private static void AddTooltipsToBatteries(StringBuilder sb, TechType techType, GameObject obj)
        {
            IBattery battery = obj.GetComponent<IBattery>();
            if (battery != null)
                TooltipFactory.WriteDescription(sb,
                    Language.main.Get(TooltipFactory.techTypeTooltipStrings.Get(techType)));
        }

        /// <summary>
        /// Make sure vehicles with visible powercells show the model when a different cell-like battery is in the slot.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EnergyMixin), nameof(EnergyMixin.NotifyHasBattery))]
        private static void EnsureVisiblePowerCell(ref EnergyMixin __instance, InventoryItem item)
        {
            // Additional null check because unity lifetime check shenanigans.
            if (item?.item is null)
                return;
            if (item.item.GetTechType().Equals(AcidPowerCell.TechType))
                __instance.batteryModels[0].model.SetActive(true);
        }

        /// <summary>
        /// Remove the recipe for batteries from the recipes the player knows from the beginning of the game.
        /// </summary>
        /// <param name="data"></param>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(KnownTech), nameof(KnownTech.Initialize))]
        private static void LockBatteryBlueprint(PDAData data)
        {
            if (SaveData.Main is null || SaveData.Main.Config.BatteryCapacity == Difficulty4.Normal)
                return;
            // Lock the vanilla battery recipe behind lithium.
            data.defaultTech.Remove(TechType.Battery);
            KnownTechHandler.SetAnalysisTechEntry(TechType.Lithium, new [] { TechType.Battery });
        }

        /// <summary>
        /// Ensure that all tools and vehicles spawn without batteries and cells.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(EnergyMixin), nameof(EnergyMixin.OnCraftEnd))]
        private static void SpawnWithoutBattery(ref EnergyMixin __instance, TechType techType)
        {
            // Don't apply this patch on low difficulty levels.
            if (SaveData.Main.Config.BatteryCapacity.Equals(Difficulty4.Normal))
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