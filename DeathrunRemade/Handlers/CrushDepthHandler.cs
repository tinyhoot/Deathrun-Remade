using System.Collections.Generic;
using System.Linq;
using DeathrunRemade.Configuration;
using DeathrunRemade.Objects;
using UnityEngine;

namespace DeathrunRemade.Handlers
{
    internal static class CrushDepthHandler
    {
        public const float InfiniteCrushDepth = 10000f;
        public const float SuitlessCrushDepth = 200f;

        private static readonly Dictionary<TechType, float[]> SuitCrushDepths = new Dictionary<TechType, float[]>
        {
            { TechType.RadiationSuit, new[] { 500f, 500f } },
            { TechType.ReinforcedDiveSuit, new []{ InfiniteCrushDepth, 800f } },
            { TechType.WaterFiltrationSuit, new []{ 1300f, 800f } },
        };

        public static readonly Utils.MonitoredValue<int> CompassDepthClassOverride = new Utils.MonitoredValue<int>();

        /// <summary>
        /// Update the depth class to be displayed by the compass (regular vs danger).
        /// </summary>
        public static void UpdateCompassDepthClass(Player player)
        {
            // There is a different depth class system for vehicles, do not bother when the player is piloting one.
            if (SaveData.Main is null || Inventory.main == null || player.GetVehicle() != null)
                return;
            
            // The depth is always safe inside non-flooded bases.
            // IsLeaking() as opposed to IsUnderwater() allows for a red compass even while the player is still in
            // knee-deep water and not yet taking damage, which works well as a warning system.
            if (player.IsInBase() && !player.GetCurrentSub().IsLeaking())
            {
                CompassDepthClassOverride.Update((int)Ocean.DepthClass.Safe);
                return;
            }
            
            TechType suit = Inventory.main.equipment.GetTechTypeInSlot("Body");
            int crushDepth = Mathf.FloorToInt(GetCrushDepth(suit, SaveData.Main.Config));
            Ocean.DepthClass depthClass = player.GetDepth() >= crushDepth ? Ocean.DepthClass.Crush : Ocean.DepthClass.Safe;
            CompassDepthClassOverride.Update((int)depthClass);
        }

        /// <summary>
        /// Add a custom suit with its own crush depth which differs depending on difficulty. The crush depth values are
        /// associated with difficulty in ascending order.
        /// </summary>
        public static void AddSuitCrushDepth(TechType suit, IEnumerable<float> crushDepth)
        {
            SuitCrushDepths[suit] = crushDepth.ToArray();
        }

        /// <summary>
        /// Attempt to get the existing crush depth values of a suit.
        /// </summary>
        public static bool TryGetSuitCrushDepth(TechType suit, out float[] crushDepths)
        {
            return SuitCrushDepths.TryGetValue(suit, out crushDepths);
        }

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
            
            TechType suit = Inventory.main.equipment.GetTechTypeInSlot("Body");
            float crushDepth = GetCrushDepth(suit, SaveData.Main.Config);
            float diff = player.GetDepth() - crushDepth;
            // Not below the crush depth, do nothing.
            if (diff <= 0)
                return;

            // Show a warning before dealing damage.
            if (WarningHandler.ShowWarning(Warning.CrushDepth))
                return;
            
            // Small chance to not take damage this time.
            if (UnityEngine.Random.value < 0.3f)
                return;
            
            // At 8 depth, ^2 (4dmg). At 40 depth, ^6 (64dmg).
            // Together with the separate global damage multiplier, this gets quite punishing.
            float damageExp = 1f + Mathf.Clamp(diff / 8f, 1f, 5f);
            player.GetComponent<LiveMixin>().TakeDamage(Mathf.Pow(2f, damageExp), type: DamageType.Pressure);
            DeathrunInit._Log.InGameMessage("The pressure is crushing you!");
        }
        
        /// <summary>
        /// Get the crush depth of the provided suit based on config values.
        /// </summary>
        public static float GetCrushDepth(TechType suit, ConfigSave config)
        {
            // Difficulty turned into an index, ignoring NORMAL and starting with HARD.
            int difficulty = (int)config.PersonalCrushDepth - 1;
            // If there is no entry for this techtype always use the minimum default value.
            float[] depths = SuitCrushDepths.GetOrDefault(suit, new[] { SuitlessCrushDepth });
            // Ensure that no mess-up happened in adding custom suit values anywhere.
            if (depths.Length == 0)
            {
                DeathrunInit._Log.Warn($"Tried to get crush depth values for '{suit}' but the custom values are "
                                       + $"an empty array!");
                return SuitlessCrushDepth;
            }
            // Attempt to find a difficulty-specific depth for this suit. If none exists, take the one for the highest
            // defined difficulty level.
            return depths.Length > difficulty ? depths[difficulty] : depths[depths.Length - 1];
        }
    }
}