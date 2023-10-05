using DeathrunRemade.Items;
using HarmonyLib;
using UnityEngine;

namespace DeathrunRemade.Patches
{
    [HarmonyPatch]
    internal class SuitPatcher
    {
        public const float MinTemperatureLimit = 49f;
        
        /// <summary>
        /// Ensure that some of the special suits are also recognised as reinforced suits.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.HasReinforcedSuit))]
        private static void RecogniseUpgradedSuits(ref bool __result)
        {
            if (__result)
                return;

            var equipment = Inventory.main.equipment;
            __result = equipment.GetCount(Suit.ReinforcedFiltration) > 0
                       || equipment.GetCount(Suit.ReinforcedMk2) > 0
                       || equipment.GetCount(Suit.ReinforcedMk3) > 0;
        }

        /// <summary>
        /// Ensure that temperature is updated properly for custom suits.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.UpdateReinforcedSuit))]
        private static void UpdateSuitValues(ref Player __instance)
        {
            TechType suit = Inventory.main.equipment.GetTechTypeInSlot("Body");
            float limit = GetTemperatureLimit(suit);
            if (__instance.HasReinforcedGloves())
                limit += 6f;
            __instance.temperatureDamage.minDamageTemperature = limit;
        }
        
        /// <summary>
        /// Get the temperature limit of a suit.
        /// </summary>
        public static float GetTemperatureLimit(TechType suit)
        {
            if (suit == TechType.None)
                return MinTemperatureLimit;
            
            float tempLimit = MinTemperatureLimit;
            if (suit.Equals(TechType.ReinforcedDiveSuit))
                tempLimit += 15f;
            // Also check for temperature from custom suits.
            tempLimit = Mathf.Max(tempLimit, Suit.GetTemperatureLimit(suit));
            return tempLimit;
        }
    }
}