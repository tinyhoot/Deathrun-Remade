using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Enums;
using HarmonyLib;

namespace DeathrunRemade.Patches
{
    [HarmonyPatch]
    internal class FarmingChallengePatcher
    {
        /// <summary>
        /// Increase the time it takes for plants to grow.
        /// </summary>
        /// <param name="__result"></param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GrowingPlant), nameof(GrowingPlant.GetGrowthDuration))]
        private static void IncreaseGrowthDuration(ref float __result)
        {
            __result *= GetDurationMult(SaveData.Main.Config.FarmingChallenge);
        }

        public static float GetDurationMult(Difficulty3 difficulty)
        {
            return difficulty switch
            {
                Difficulty3.Hard => 3f,
                Difficulty3.Deathrun => 6f,
                _ => 1f
            };
        }
    }
}