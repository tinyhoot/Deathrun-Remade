using System.Collections.Generic;
using System.Text;
using DeathrunRemade.Items;
using DeathrunRemade.Objects.Enums;
using HarmonyLib;
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
        public static void AddTooltipsToBatteries(StringBuilder sb, TechType techType, GameObject obj)
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
        public static void EnsureVisiblePowerCell(ref EnergyMixin __instance, InventoryItem item)
        {
            // Additional null check because unity lifetime check shenanigans.
            if (item?.item is null)
                return;
            if (item.item.GetTechType() == ItemInfo.GetTechTypeForItem(nameof(AcidPowerCell)))
                __instance.batteryModels[0].model.SetActive(true);
        }

        /// <summary>
        /// Ensure that all tools and vehicles spawn without batteries and cells.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(EnergyMixin), nameof(EnergyMixin.OnCraftEnd))]
        public static void SpawnWithoutBattery(EnergyMixin __instance, TechType techType)
        {
            // Don't apply this patch on low difficulty levels.
            if (DeathrunInit._Config.BatteryCapacity.Value.Equals(Difficulty4.Normal))
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
        public static void UpdatePowerCellCharger(ref PowerCellCharger __instance)
        {
            HashSet<TechType> compatibleTech = PowerCellCharger.compatibleTech;
            TechType powerCell = ItemInfo.GetTechTypeForItem(nameof(AcidPowerCell));
            // It's a hashset, checking whether the techtype already exists is superfluous.
            compatibleTech.Add(powerCell);
        }

        /// <summary>
        /// Necessary to allow our custom batteries to be put into things with battery slots.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(EnergyMixin), nameof(EnergyMixin.Awake))]
        public static void UpdateValidBatteries(ref EnergyMixin __instance)
        {
            if (!__instance.allowBatteryReplacement)
                return;

            if (__instance.compatibleBatteries.Contains(TechType.Battery)
                || __instance.compatibleBatteries.Contains(TechType.PrecursorIonBattery))
            {
                __instance.compatibleBatteries.Add(ItemInfo.GetTechTypeForItem(nameof(AcidBattery)));
            }

            if (__instance.compatibleBatteries.Contains(TechType.PowerCell)
                || __instance.compatibleBatteries.Contains(TechType.PrecursorIonPowerCell))
            {
                __instance.compatibleBatteries.Add(ItemInfo.GetTechTypeForItem(nameof(AcidPowerCell)));
            }
        }
    }
}