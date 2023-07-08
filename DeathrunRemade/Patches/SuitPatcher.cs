using DeathrunRemade.Configuration;
using DeathrunRemade.Items;
using HarmonyLib;

namespace DeathrunRemade.Patches
{
    [HarmonyPatch]
    internal class SuitPatcher
    {
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
            __instance.temperatureDamage.minDamageTemperature = ConfigUtils.GetPersonalTemperatureLimit();
        }
    }
}