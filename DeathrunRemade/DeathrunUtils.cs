using System;
using UnityEngine;

namespace DeathrunRemade
{
    /// <summary>
    /// General utilities which just didn't feel like they fit anywhere else.
    /// </summary>
    public static class DeathrunUtils
    {
        /// <summary>
        /// Convert a float representing a number of seconds to in-game days.
        /// </summary>
        public static float TimeToGameDays(double time)
        {
            return (float)(time / DayNightCycle.kDayLengthSeconds);
        }
        
        /// <summary>
        /// Get a detailed name for the player's current biome, like individual hallways within the Aurora.
        /// </summary>
        public static string GetDetailedPlayerBiome()
        {
            if (AtmosphereDirector.main != null)
            {
                string biomeOverride = AtmosphereDirector.main.GetBiomeOverride();
                if (!string.IsNullOrEmpty(biomeOverride))
                    return biomeOverride;
            }

            if (LargeWorld.main != null && Player.main != null)
            {
                string biome = LargeWorld.main.GetBiome(Player.main.transform.position);
                if (string.IsNullOrEmpty(biome))
                    return "<none>";
                else
                    return biome;
            }
            
            return "<unkown>";
        }

        /// <summary>
        /// If something goes terribly during wrong e.g. during load, inform the user.
        /// </summary>
        public static void FatalError(Exception exception)
        {
            DeathrunInit._Log.InGameMessage($"{DeathrunInit.NAME} has encountered a fatal error and will not function "
                                            + $"properly. Please report this error with your LogOutput.log on NexusMods, "
                                            + $"GitHub, or the Subnautica Modding Discord.", true);
            DeathrunInit._Log.Fatal($"{exception.GetType()}: {exception.Message}\n"
                                    + $"{exception.StackTrace}");
        }
        
        /// <summary>
        /// Check whether the player is inside any kind of player-made base, escape pod or vehicle.
        /// </summary>
        public static bool IsInsideSubOrVehicle(this Player player)
        {
            return player.IsInSub() || player.GetVehicle() != null || player.currentEscapePod != null;
        }

        /// <summary>
        /// Check whether the player is inside any kind of player-made base, escape pod or vehicle
        /// and whether that place is also powered.
        /// </summary>
        public static bool IsInsidePoweredSubOrVehicle(this Player player)
        {
            if (player.currentEscapePod)
                return true;
            if (player.IsInSub())
                return player.currentSub.powerRelay != null && player.currentSub.powerRelay.IsPowered();
            return player.IsInsidePoweredVehicle();
        }

        /// <summary>
        /// Check whether the escape pod has already been repaired. This way is more reliable than checking for damaged
        /// effects since those don't start until after the cutscene ends.
        /// </summary>
        public static bool IsPodRepaired(EscapePod pod)
        {
            return pod.liveMixin.IsFullHealth();
        }

        /// <summary>
        /// Set the position of a countdown window.
        /// </summary>
        /// <param name="countdownWindow">The transform of the controlling object of the countdown window. For the
        /// Sunbeam this is the transform with the <see cref="uGUI_SunbeamCountdown"/> component.</param>
        /// <param name="xpos">How far from the left edge of the screen the window should be placed,
        /// measured as a percentage of the total screen width.</param>
        /// <param name="ypos">How far down from the top of the screen the window should be placed,
        /// measured as a percentage of the total screen height.</param>
        public static void SetCountdownWindowPosition(Transform countdownWindow, float xpos, float ypos)
        {
            // Get the transform which holds the actual visible window itself.
            Transform contentHolder = countdownWindow.GetChild(0);
            // Find the rectangle of the entire screen.
            Rect screenRect = countdownWindow.GetComponent<RectTransform>().rect;
            RectTransform contentRect = contentHolder.GetComponent<RectTransform>();
            // Set the positional centre of the countdown window to its upper left corner.
            contentRect.pivot = new Vector2(0f, 1f);
            // Calculate the position based on the given percentages. The coordinate centre is the middle of the screen
            // so it takes a little extra math to get the correct offset.
            float x = -(screenRect.width / 2f) + ((screenRect.width - contentRect.rect.width) * xpos);
            float y = (screenRect.height / 2f) - ((screenRect.height - contentRect.rect.height) * ypos);
            Vector3 position = new Vector3(x, y, contentHolder.localPosition.z);
            contentHolder.transform.localPosition = position;
        }
    }
}