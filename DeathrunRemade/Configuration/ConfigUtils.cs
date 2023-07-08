using System;
using DeathrunRemade.Items;
using DeathrunRemade.Objects.Enums;

namespace DeathrunRemade.Configuration
{
    /// <summary>
    /// Make sense of the myriad of config options through easily consumable functions.
    /// </summary>
    internal static class ConfigUtils
    {
        private static Config _config => DeathrunInit._Config;

        /// <summary>
        /// Checks whether the player is able to breathe at their current location.
        /// </summary>
        public static bool CanBreathe(Player player)
        {
            // Special cases like player bases, vehicles or alien bases should always be breathable.
            if (player.IsInside() || player.IsInSub())
                return true;
            
            // If at the surface, check for irradiated air.
            return IsAirBreathable();
        }

        /// <summary>
        /// Get the crush depth of the player based on equipped suits.
        /// </summary>
        public static float GetPersonalCrushDepth()
        {
            TechType suit = Inventory.main.equipment.GetTechTypeInSlot("Body");
            bool deathrun = _config.CrushDepth.Value == Difficulty3.Deathrun;
            float depth = suit switch
            {
                TechType.RadiationSuit => 500f,
                TechType.ReinforcedDiveSuit => deathrun ? 800f : Constants.InfiniteCrushDepth,
                TechType.WaterFiltrationSuit => deathrun ? 800f : 1300f,
                _ => 200f,
            };
            // If the player wasn't wearing any of the vanilla suits, check for custom ones.
            if (depth <= 201f)
                depth = Suit.GetCrushDepth(suit, deathrun);
            return depth;
        }

        /// <summary>
        /// Get the temperature limit of the player based on their equipment.
        /// </summary>
        public static float GetPersonalTemperatureLimit()
        {
            TechType suit = Inventory.main.equipment.GetTechTypeInSlot("Body");
            float tempLimit = Constants.MinTemperatureLimit;
            if (suit.Equals(TechType.ReinforcedDiveSuit))
                tempLimit += 15f;
            // Also check for temperature from custom suits.
            tempLimit = Math.Max(tempLimit, Suit.GetTemperatureLimit(suit));
            if (Player.main.HasReinforcedGloves())
                tempLimit += 6f;
            return tempLimit;
        }
        
        /// <summary>
        /// Checks whether the player would be able to breathe at the surface.
        /// </summary>
        public static bool IsAirBreathable()
        {
            if (_config.SurfaceAir.Value == Difficulty3.Normal)
                return true;

            // If this doesn't pass the game is not yet done loading.
            if (Inventory.main is null || Inventory.main.equipment is null)
                return true;
            if (Inventory.main.equipment.GetCount(ItemInfo.GetTechTypeForItem(nameof(FilterChip))) > 0)
                return true;

            // Surface air without a filter is always unbreathable on high difficulties.
            if (_config.SurfaceAir.Value == Difficulty3.Deathrun)
                return false;
            return !IsSurfaceIrradiated();
        }

        /// <summary>
        /// Check whether the surface as a whole is irradiated.
        /// </summary>
        public static bool IsSurfaceIrradiated()
        {
            // If these do not exist the game is probably still loading.
            if (CrashedShipExploder.main is null || LeakingRadiation.main is null)
                return false;
            if (!CrashedShipExploder.main.IsExploded())
                return false;
            // Surface is decontaminated once leaks are fixed and radiation has completely dissipated.
            return LeakingRadiation.main.radiationFixed && LeakingRadiation.main.currentRadius < 5f;
        }
    }
}