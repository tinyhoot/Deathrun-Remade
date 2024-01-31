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
        /// Deal damage but prevent one-shots by reducing lethal damage if the LiveMixin's current health is above the
        /// threshold.
        /// </summary>
        public static void TakeOneShotProtectedDamage(LiveMixin liveMixin, float damage, DamageType type, float threshold = 10f)
        {
            if (liveMixin.health > threshold)
                damage = Mathf.Min(damage, liveMixin.health - (threshold / 2f));
            liveMixin.TakeDamage(damage, type: type);
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
            RectTransform contentRect = contentHolder.GetComponent<RectTransform>();
            SetRelativeScreenPositionInbounds(contentRect, new Vector2(xpos, ypos));
        }

        /// <summary>
        /// Set a UI transform's position on the screen relative to the total screen size, while respecting the
        /// transform's pivot and size properties to ensure it does not partially leave the screen.
        /// </summary>
        /// <param name="rect">The transform to move.</param>
        /// <param name="targetPos">How far from the top left corner of the screen the window should be placed,
        /// measured as a fraction of the total screen width/height. Accepts values from 0 to 1.</param>
        public static void SetRelativeScreenPositionInbounds(RectTransform rect, Vector2 targetPos)
        {
            Vector2 size = rect.rect.size;
            Vector2 availableSpace = new Vector2(1920f, 1080f) - size;

            Vector2 absolutePos = availableSpace * targetPos;
            Vector2 centreOffset = availableSpace / 2f;
            
            // Pivot is already in the inverted system, do not convert it.
            Vector2 convertedPos = (absolutePos - centreOffset) * new Vector2(1f, -1f);
            Vector2 pivotOffset = size * (rect.pivot - new Vector2(0.5f, 0.5f));
            rect.localPosition = (convertedPos + pivotOffset).WithZ(rect.localPosition.z);
        }

        /// <summary>
        /// Set a UI transform's position on the screen relative to the total screen size.
        /// </summary>
        /// <param name="transform">The transform to move.</param>
        /// <param name="x">How far from the left edge of the screen the window should be placed,
        /// measured as a fraction of the total screen width.</param>
        /// <param name="y">How far down from the top of the screen the window should be placed,
        /// measured as a fraction of the total screen height.</param>
        public static void SetRelativeScreenPosition(Transform transform, float x, float y)
        {
            // The base resolution of all UI is 1920x1080. For other resolutions, unity keeps the coordinate system
            // intact and scales the result appropriately.
            Vector2 absolute = new Vector2(x * 1920f, y * -1080f);
            Vector2 offset = new Vector2(1920f / 2f, -1080f / 2f);
            transform.localPosition = (absolute - offset).WithZ(transform.localPosition.z);
        }
    }
}