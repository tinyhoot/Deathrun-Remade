using DeathrunRemade.Items;
using DeathrunRemade.Objects.Attributes;
using DeathrunRemade.Objects.Enums;
using HarmonyLib;

namespace DeathrunRemade.Patches
{
    [HarmonyPatch]
    [PatchCategory(ApplyPatch.Always)]
    internal class FilterChipPatcher
    {
        /// <summary>
        /// Ensure that FilterChip is also counted as a compass.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Equipment), nameof(Equipment.GetCount))]
        private static void CountFilterChip(ref Equipment __instance, TechType techType, ref int __result)
        {
            if (!techType.Equals(TechType.Compass) || __result > 0 || FilterChip.s_TechType == TechType.None)
                return;
            
            __result = __instance.equippedCount.GetOrDefault(FilterChip.s_TechType, 0);
        }
    }
}