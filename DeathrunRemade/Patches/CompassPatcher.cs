using DeathrunRemade.Items;
using HarmonyLib;

namespace DeathrunRemade.Patches
{
    [HarmonyPatch]
    internal class CompassPatcher
    {
        /// <summary>
        /// Ensure that FilterChip is also counted as a compass.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Equipment), nameof(Equipment.GetCount))]
        private static void CountFilterChip(ref Equipment __instance, TechType techType, ref int __result)
        {
            if (!techType.Equals(TechType.Compass) || __result > 0)
                return;
            
            TechType filterChip = ItemInfo.GetTechTypeForItem(nameof(FilterChip));
            __result = __instance.equippedCount.GetOrDefault(filterChip, 0);
        }
    }
}