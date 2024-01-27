using System;
using DeathrunRemade.Objects.Attributes;
using DeathrunRemade.Objects.Enums;
using HarmonyLib;
using UnityEngine.SceneManagement;
using UWE;

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
        /// Invoked after the player has pressed the button to either start or load a game and the game transitions
        /// to the loading screen.
        /// </summary>
        public static event Action OnMainMenuPressPlay;
        
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
        /// Invoked when the loading screen for is done and the player gains control.
        /// </summary>
        public static event Action<Player> OnPlayerGainControl;

        /// <summary>
        /// Invoked as soon as the rocket is launched, i.e. just as the ending cinematic begins.
        /// </summary>
        public static event Action<Player> OnPlayerVictory;

        /// <summary>
        /// Invoked when the loading screen for a previously saved game is done and the player gains control.
        /// </summary>
        public static event Action OnSavedGameLoaded;

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
        [HarmonyPatch(typeof(FreezeTime), nameof(FreezeTime.End))]
        private static void TriggerSaveGameLoaded(FreezeTime.Id id)
        {
            if (id == FreezeTime.Id.WaitScreen && Player.main != null)
            {
                OnPlayerGainControl?.Invoke(Player.main);
                if (!EscapePod.main.isNewBorn)
                    OnSavedGameLoaded?.Invoke();
            }
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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MainSceneLoading), nameof(MainSceneLoading.Launch))]
        private static void TriggerPressPlay()
        {
            OnMainMenuPressPlay?.Invoke();
        }
    }
}