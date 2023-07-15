using System;

namespace DeathrunRemade.Handlers
{
    /// <summary>
    /// A messaging class to prevent every single object needing to have its own patcher to hook into the same
    /// few common methods.
    /// </summary>
    internal static class GameEventHandler
    {
        public static event Action<uGUI_SceneHUD> OnHudUpdate;
        public static event Action<Player> OnPlayerAwake;

        public static void TriggerHudUpdate(uGUI_SceneHUD hud)
        {
            OnHudUpdate?.Invoke(hud);
        }
        
        public static void TriggerPlayerAwake(Player player)
        {
            OnPlayerAwake?.Invoke(player);
        }
    }
}