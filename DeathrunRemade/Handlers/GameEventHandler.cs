using System;
using DeathrunRemade.Objects.Attributes;
using DeathrunRemade.Objects.Enums;
using HarmonyLib;
using UnityEngine.SceneManagement;

namespace DeathrunRemade.Handlers
{
    /// <summary>
    /// A messaging class to prevent every single object needing to have its own patcher to hook into the same
    /// few common methods.
    /// </summary>
    [HarmonyPatch]
    [PatchCategory(ApplyPatch.Always)]
    internal static class GameEventHandler
    {
        /// <summary>
        /// Invoked whenever the main HUD updates the visibility of its components. This includes things like oxygen
        /// and health bars or the depth meter.
        /// </summary>
        public static event Action<uGUI_SceneHUD> OnHudUpdate;
        
        /// <summary>
        /// Invoked whenever the main menu is loaded, both from startup and by returning from play.
        /// </summary>
        public static event Action OnMainMenuLoaded;

        /// <summary>
        /// Invoked whenever the PDA is opened/closed. The parameter is true if the PDA is currently open.
        /// </summary>
        public static event Action<bool> OnPdaStateChanged;
        
        /// <summary>
        /// Invoked as a postfix to Player.Awake().
        /// </summary>
        public static event Action<Player> OnPlayerAwake;

        /// <summary>
        /// Invoked whenever the player dies.
        /// Using <see cref="Player.playerDeathEvent"/> instead would be nice but for some reason that is called really
        /// inconsistently.
        /// </summary>
        public static event Action<Player, DamageType> OnPlayerDeath;

        /// <summary>
        /// Invoked as soon as the rocket is launched, i.e. just as the ending cinematic begins.
        /// </summary>
        public static event Action<Player> OnPlayerVictory;

        /// <summary>
        /// Register all events for the handler to propagate to the rest of the mod.
        /// </summary>
        public static void RegisterEvents()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        /// <summary>
        /// Get notified on any scene change.
        /// </summary>
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "XMenu")
                return;
            OnMainMenuLoaded?.Invoke();
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(uGUI_SceneHUD), nameof(uGUI_SceneHUD.UpdateElements))]
        private static void TriggerHudUpdate(uGUI_SceneHUD __instance)
        {
            OnHudUpdate?.Invoke(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(uGUI_PDA), nameof(uGUI_PDA.SetCanvasVisible))]
        private static void TriggerPdaStateChanged(bool visible)
        {
            OnPdaStateChanged?.Invoke(visible);
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
        private static void TriggerPlayerAwake(Player __instance)
        {
            OnPlayerAwake?.Invoke(__instance);
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.OnKill))]
        private static void TriggerPlayerDeath(Player __instance, DamageType damageType)
        {
            OnPlayerDeath?.Invoke(__instance, damageType);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LaunchRocket), nameof(LaunchRocket.SetLaunchStarted))]
        private static void TriggerPlayerVictory()
        {
            OnPlayerVictory?.Invoke(Player.main);
        }
    }
}