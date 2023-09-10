using System;
using System.IO;
using System.Linq;
using DeathrunRemade.Items;
using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Enums;
using UnityEngine;
using ILogHandler = HootLib.Interfaces.ILogHandler;

namespace DeathrunRemade.Configuration
{
    /// <summary>
    /// Make sense of the myriad of config options through easily consumable functions.
    /// </summary>
    internal static class ConfigUtils
    {
        private static ConfigSave _config => SaveData.Main.Config;
        private static ILogHandler _log => DeathrunInit._Log;

        /// <summary>
        /// Checks whether the player is able to breathe at their current location.
        /// </summary>
        public static bool CanBreathe(Player player)
        {
            // Special cases like player bases, vehicles or alien bases should always be breathable.
            if (player.IsInsidePoweredSubOrVehicle() || player.precursorOutOfWater)
                return true;
            
            // If at the surface, check for irradiated air.
            return IsAirBreathable();
        }

        /// <summary>
        /// Get the damage dealt by decompression sickness.
        /// </summary>
        public static int GetBendsDamage(Difficulty3 value)
        {
            return value switch
            {
                Difficulty3.Normal => 10,
                Difficulty3.Hard => 10,
                Difficulty3.Deathrun => 20,
                _ => throw new InvalidDataException()
            };
        }

        /// <summary>
        /// Get the crush depth of the player based on equipped suits.
        /// </summary>
        public static float GetPersonalCrushDepth()
        {
            TechType suit = Inventory.main.equipment.GetTechTypeInSlot("Body");
            bool deathrun = _config.CrushDepth == Difficulty3.Deathrun;
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
        /// Get the spawn point of the escape pod.
        /// </summary>
        /// <returns></returns>
        public static Vector3 GetStartPoint(out string name)
        {
            string setting = _config.StartLocation;

            // This will throw an exception if the setting name has been altered for some reason, but that's intended.
            StartLocation location = DeathrunInit._Config._startLocations.First(l => l.Name == setting);
            if (setting == "Random")
                location = DeathrunInit._Config._startLocations.Where(l => l.Name != "Random").ToList().GetRandom();

            name = location.Name;
            if (location.Name == "Vanilla")
                return default;
            return new Vector3(location.X, location.Y, location.Z);
        }
        
        /// <summary>
        /// Checks whether the player would be able to breathe at the surface.
        /// </summary>
        public static bool IsAirBreathable()
        {
            if (_config.SurfaceAir == Difficulty3.Normal)
                return true;

            // If this doesn't pass the game is not yet done loading.
            if (Inventory.main is null || Inventory.main.equipment is null)
                return true;
            if (Inventory.main.equipment.GetCount(FilterChip.TechType) > 0)
                return true;

            // Surface air without a filter is always unbreathable on high difficulties.
            if (_config.SurfaceAir == Difficulty3.Deathrun)
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

        /// <summary>
        /// Check whether the given warning should be shown.
        /// </summary>
        /// <param name="warning">The type of warning.</param>
        /// <param name="intervalSeconds">The interval between warnings, in seconds.</param>
        public static bool ShouldShowWarning(Warning warning, float intervalSeconds)
        {
            Hints setting = _config.ShowWarnings;
            if (setting == Hints.Never)
                return false;

            // Don't show anything until the save has finished loading.
            if (SaveData.Main is null)
                return false;
            
            WarningSave save = SaveData.Main.Warnings;
            float lastShown = warning switch
            {
                Warning.AscentSpeed => save.lastAscentWarningTime,
                Warning.Decompression => save.lastDecompressionWarningTime,
                Warning.DecompressionDamage => save.lastDecoDamageWarningTime,
                Warning.UnbreathableAir => save.lastBreathWarningTime,
                _ => throw new InvalidDataException()
            };

            // Always space out warnings by at least this much.
            const float delay = 3f;
            float delta = Time.time - lastShown;
            switch (setting)
            {
                case Hints.Introductory:
                    return lastShown == 0;
                case Hints.Occasional:
                    return delta > intervalSeconds && delta > delay;
                case Hints.Always:
                    return delta > delay;
            }
            
            // Things shouldn't be able to get down here, but if it happens, uh, scream?
            _log.Warn($"Unexpected outcome in {typeof(ConfigUtils).FullName}.{nameof(ShouldShowWarning)}: "
                      + $"Did not return proper value!");
            return false;
        }
    }
}