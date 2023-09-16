using DeathrunRemade.Configuration;
using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Enums;
using UnityEngine;

namespace DeathrunRemade.Handlers
{
    internal static class CrushDepthHandler
    {
        /// <summary>
        /// Do the math and check whether the player needs to take crush damage.
        ///
        /// This method is tied to the player taking breaths, so it runs every three seconds or so.
        /// </summary>
        public static void CrushPlayer(Player player)
        {
            // Only do this if the player is exposed to the elements.
            if (!player.IsUnderwater() || player.currentWaterPark != null)
                return;
            
            float crushDepth = ConfigUtils.GetPersonalCrushDepth();
            float diff = player.GetDepth() - crushDepth;
            // Not below the crush depth, do nothing.
            if (diff <= 0)
                return;

            // Show a warning before dealing damage.
            if (ConfigUtils.ShouldShowWarning(Warning.CrushDepth, 30f))
            {
                DeathrunInit._Log.InGameMessage("Personal crush depth exceeded. Return to safe depth!");
                SaveData.Main.Warnings.lastCrushDepthWarningTime = Time.time;
                return;
            }
            
            // Fifty-fifty on whether you take damage this time.
            if (UnityEngine.Random.value < 0.5f)
                return;
            
            // At 50 depth, ^2 (4dmg). At 250 depth, ^6 (64dmg).
            // Together with the separate global damage multiplier, this gets quite punishing.
            float damageExp = 1f + Mathf.Clamp(diff / 50f, 1f, 5f);
            player.GetComponent<LiveMixin>().TakeDamage(Mathf.Pow(2f, damageExp), type: DamageType.Pressure);
            DeathrunInit._Log.InGameMessage("The pressure is crushing you!");
        }
    }
}