using DeathrunRemade.Items;
using DeathrunRemade.Objects.Attributes;
using DeathrunRemade.Objects.Enums;
using HarmonyLib;

namespace DeathrunRemade.Patches
{
    [HarmonyPatch]
    [PatchCategory(ApplyPatch.Always)]
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
            __result = equipment.GetCount(ReinforcedFiltrationSuit.s_TechType) > 0
                       || equipment.GetCount(ReinforcedSuitMk2.s_TechType) > 0
                       || equipment.GetCount(ReinforcedSuitMk3.s_TechType) > 0;
        }

        /// <summary>
        /// Ensure that temperature is updated properly for custom suits.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.UpdateReinforcedSuit))]
        private static void UpdateSuitValues(ref Player __instance)
        {
            TechType suit = Inventory.main.equipment.GetTechTypeInSlot("Body");

            // Only change things if this is a suit added by our mod.
            if (!SuitBase.TryGetTemperatureLimit(suit, out float limit))
                return;
            if (__instance.HasReinforcedGloves())
                limit += 6f;
            
            // Other mods might add extra equipment that raises the limit even higher. Do not overwrite that,
            // but ensure a floor.
            if (__instance.temperatureDamage.minDamageTemperature < limit)
                __instance.temperatureDamage.minDamageTemperature = limit;
        }
    }
}