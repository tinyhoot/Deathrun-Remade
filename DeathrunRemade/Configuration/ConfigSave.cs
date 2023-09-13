using System.Reflection;
using DeathrunRemade.Objects.Enums;
using HarmonyLib;
using HootLib.Configuration;
using HootLib.Objects.Exceptions;

namespace DeathrunRemade.Configuration
{
    /// <summary>
    /// The config class is complex and full of references to things that change at runtime. This struct preserves only
    /// the relevant setting values for consistency during a run.
    /// </summary>
    internal readonly struct ConfigSave
    {
        // Everything in here *does* get assigned, but by reflection rather than explicitly.
#pragma warning disable CS0649
        // Survival
        public readonly Difficulty3 PersonalCrushDepth;
        public readonly Difficulty4 DamageTaken;
        public readonly Difficulty3 NitrogenBends;
        public readonly bool SpecialAirTanks;
        public readonly Difficulty3 SurfaceAir;
        public readonly string StartLocation;
        public readonly bool SinkLifepod;
        public readonly bool ToppleLifepod;
        
        // Environment
        public readonly Difficulty4 CreatureAggression;
        public readonly Murkiness WaterMurkiness;
        public readonly Difficulty3 ExplosionDepth;
        public readonly Timer ExplosionTime;
        public readonly Difficulty4 RadiationDepth;
        public readonly RadiationVisuals RadiationFX;
        
        // Costs
        public readonly Difficulty4 BatteryCapacity;
        public readonly Difficulty4 ToolCosts;
        public readonly Difficulty4 PowerCosts;
        public readonly Difficulty4 ScansRequired;
        public readonly VehicleDifficulty VehicleCosts;
        public readonly Difficulty4 VehicleExitPowerLoss;
        
        // Challenges
        public readonly Difficulty3 FarmingChallenge;
        public readonly Difficulty3 FilterPumpChallenge;
        public readonly DietPreference FoodChallenge;
        public readonly RelativeToExplosion IslandFoodChallenge;
        public readonly bool PacifistChallenge;
        
        // UI
        public readonly bool ShowHighscores;
        public readonly bool ShowHighscoreTips;
        public readonly Hints ShowWarnings;
        
        // This field defaults to false in any instance that was made using the constructor rather than serialisation.
        public readonly bool WasInitialised;
        
#pragma warning restore CS0649
        
        /// <summary>
        /// Create a new save data instance from an existing config.
        /// </summary>
        public static ConfigSave SerializeConfig(Config config)
        {
            ConfigSave save = new ConfigSave();
            // Use reflection to copy each field from the config to the field in the save data of the same name.
            // This also gets around the readonly restriction. Ideally all of this would be a constructor but that
            // does not allow for setting fields by reflection (i.e. arbitrary names).
            var fields = AccessTools.GetDeclaredFields(typeof(Config));
            foreach (FieldInfo field in fields)
            {
                // Ignore all fields that aren't actually config values.
                if (!field.FieldType.IsGenericType || !field.FieldType.GetGenericTypeDefinition().IsAssignableFrom(typeof(ConfigEntryWrapper<>)))
                    continue;

                // Find the field in this struct of the same name.
                FieldInfo saveField = AccessTools.Field(typeof(ConfigSave), field.Name);
                if (saveField is null || field.FieldType.GetGenericArguments()[0] != saveField.FieldType)
                    throw new ConfigEntryException($"Failed to find field in SaveData corresponding to {field.Name}: "
                                                   + $"Types were '{field.FieldType.GetGenericArguments()[0]}' and '{saveField?.FieldType}'");

                object wrapper = field.GetValue(config);
                if (wrapper is null)
                {
                    DeathrunInit._Log.Debug($"Ignoring field {field.Name}, was not set in config.");
                    continue;
                }
                
                // Copy the value.
                object value = AccessTools.Property(wrapper.GetType(), nameof(ConfigEntryWrapper<bool>.Value))?.GetValue(wrapper);
                saveField.SetValue(save, value);
                DeathrunInit._Log.Debug($"Successfully set field {saveField.Name}");
            }

            AccessTools.Field(typeof(ConfigSave), nameof(WasInitialised)).SetValue(save, true);
            DeathrunInit._Log.Debug($"Config serialization complete!");
            return save;
        }
    }
}