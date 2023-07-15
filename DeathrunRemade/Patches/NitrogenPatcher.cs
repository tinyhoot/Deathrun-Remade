using DeathrunRemade.Components;
using HarmonyLib;
using HootLib.Components;
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
            HootHudBar.Create<NitrogenBar>("NitrogenBar", -45, out GameObject _);
            SafeDepthHud.Create(out GameObject _);
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