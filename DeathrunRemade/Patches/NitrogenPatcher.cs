using DeathrunRemade.Monos;
using HarmonyLib;
using UnityEngine;

namespace DeathrunRemade.Patches
{
    [HarmonyPatch]
    internal class NitrogenPatcher
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
        private static void test()
        {
            NitrogenBar.Create<NitrogenBar>("NitrogenBar", -45, out GameObject gameObject);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(uGUI_SceneHUD), nameof(uGUI_SceneHUD.UpdateElements))]
        private static void TestTwo(uGUI_SceneHUD __instance)
        {
            if (NitrogenBar.Main)
                NitrogenBar.Main.HarmonyUpdateHudElements(__instance);
        }
    }
}