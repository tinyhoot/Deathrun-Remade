using System;
using HarmonyLib;

namespace DeathrunRemade.Handlers
{
    /// <summary>
    /// A messaging class to prevent every single object needing to have its own patcher to hook into the same
    /// few common methods.
    /// </summary>
    [HarmonyPatch]
    internal static class GameEventHandler
    {
        public static event Action<uGUI_SceneHUD> OnHudUpdate;
        public static event Action<Player> OnPlayerAwake;

        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(uGUI_SceneHUD), nameof(uGUI_SceneHUD.UpdateElements))]
        private static void TriggerHudUpdate(uGUI_SceneHUD __instance)
        {
            OnHudUpdate?.Invoke(__instance);
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
        private static void TriggerPlayerAwake(Player __instance)
        {
            OnPlayerAwake?.Invoke(__instance);
        }
    }
}