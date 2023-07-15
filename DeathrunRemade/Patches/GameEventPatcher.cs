using DeathrunRemade.Handlers;
using HarmonyLib;

namespace DeathrunRemade.Patches
{
    [HarmonyPatch]
    internal static class GameEventPatcher
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(uGUI_SceneHUD), nameof(uGUI_SceneHUD.UpdateElements))]
        private static void OnHudUpdate(uGUI_SceneHUD __instance)
        {
            GameEventHandler.TriggerHudUpdate(__instance);
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
        public static void OnPlayerAwake(Player __instance)
        {
            GameEventHandler.TriggerPlayerAwake(__instance);
        }
    }
}