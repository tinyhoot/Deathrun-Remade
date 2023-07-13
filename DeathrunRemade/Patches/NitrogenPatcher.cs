using DeathrunRemade.Monos;
using HarmonyLib;

namespace DeathrunRemade.Patches
{
    [HarmonyPatch]
    internal class NitrogenPatcher
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
        private static void test()
        {
            NitrogenBar.Create();
        }
    }
}