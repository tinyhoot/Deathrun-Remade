using System;
using System.Reflection;
using DeathrunRemade.Objects.Enums;
using HarmonyLib;
using HootLib.Configuration;
using HootLib.Objects.Exceptions;
using Newtonsoft.Json;

namespace DeathrunRemade.Configuration
{
    /// <summary>
    /// The config class is complex and full of references to things that change at runtime. This struct preserves only
    /// the relevant setting values for consistency during a run.
    /// </summary>
    [Serializable]
    internal readonly struct ConfigSave
    {
        // JsonProperty everywhere is necessary to allow deserialising to read-only fields.
        // Survival
        [JsonProperty] public readonly Difficulty3 PersonalCrushDepth;
        [JsonProperty] public readonly DamageDifficulty DamageTaken;
        [JsonProperty] public readonly Difficulty3 NitrogenBends;
        [JsonProperty] public readonly bool SpecialAirTanks;
        [JsonProperty] public readonly Difficulty3 SurfaceAir;
        [JsonProperty] public readonly string StartLocation;
        [JsonProperty] public readonly bool SinkLifepod;
        [JsonProperty] public readonly bool ToppleLifepod;
        
        // Environment
        [JsonProperty] public readonly Difficulty4 CreatureAggression;
        [JsonProperty] public readonly Murkiness WaterMurkiness;
        [JsonProperty] public readonly Difficulty3 ExplosionDepth;
        [JsonProperty] public readonly Timer ExplosionTime;
        [JsonProperty] public readonly Difficulty4 RadiationDepth;
        [JsonProperty] public readonly RadiationVisuals RadiationFX;
        
        // Costs
        [JsonProperty] public readonly Difficulty4 BatteryCapacity;
        [JsonProperty] public readonly Difficulty4 BatteryCosts;
        [JsonProperty] public readonly Difficulty4 ToolCosts;
        [JsonProperty] public readonly Difficulty4 PowerCosts;
        [JsonProperty] public readonly Difficulty4 ScansRequired;
        [JsonProperty] public readonly Difficulty4 VehicleCosts;
        [JsonProperty] public readonly Difficulty4 VehicleExitPowerLoss;
        
        // Challenges
        [JsonProperty] public readonly Difficulty3 FarmingChallenge;
        [JsonProperty] public readonly Difficulty3 FilterPumpChallenge;
        [JsonProperty] public readonly DietPreference FoodChallenge;
        [JsonProperty] public readonly RelativeToExplosion IslandFoodChallenge;
        [JsonProperty] public readonly bool NoVehicleChallenge;
        [JsonProperty] public readonly bool PacifistChallenge;
        
        // This field defaults to false in any instance that was made using the default constructor rather than
        // serialisation from a config file.
        [JsonProperty] public readonly bool WasInitialised;

        public ConfigSave(Config config)
        {
            PersonalCrushDepth = config.PersonalCrushDepth.Value;
            DamageTaken = config.DamageTaken.Value;
            NitrogenBends = config.NitrogenBends.Value;
            SpecialAirTanks = config.SpecialAirTanks.Value;
            SurfaceAir = config.SurfaceAir.Value;
            StartLocation = config.StartLocation.Value;
            SinkLifepod = config.SinkLifepod.Value;
            ToppleLifepod = config.ToppleLifepod.Value;

            CreatureAggression = config.CreatureAggression.Value;
            WaterMurkiness = config.WaterMurkiness.Value;
            ExplosionDepth = config.ExplosionDepth.Value;
            ExplosionTime = config.ExplosionTime.Value;
            RadiationDepth = config.RadiationDepth.Value;
            RadiationFX = config.RadiationFX.Value;

            BatteryCapacity = config.BatteryCapacity.Value;
            BatteryCosts = config.BatteryCosts.Value;
            ToolCosts = config.ToolCosts.Value;
            PowerCosts = config.PowerCosts.Value;
            ScansRequired = config.ScansRequired.Value;
            VehicleCosts = config.VehicleCosts.Value;
            VehicleExitPowerLoss = config.VehicleExitPowerLoss.Value;

            FarmingChallenge = config.FarmingChallenge.Value;
            FilterPumpChallenge = config.FilterPumpChallenge.Value;
            FoodChallenge = config.FoodChallenge.Value;
            IslandFoodChallenge = config.IslandFoodChallenge.Value;
            NoVehicleChallenge = config.NoVehicleChallenge.Value;
            PacifistChallenge = config.PacifistChallenge.Value;
            
            WasInitialised = true;

            Validate(config);
        }

        /// <summary>
        /// Ensure that no discrepancy between the config fields and the saved fields can exist by checking for the
        /// same field names with reflection.
        /// </summary>
        /// <exception cref="ConfigEntryException">Thrown if the config and this save data do not match up.</exception>
        private void Validate(Config config)
        {
            var configFields = AccessTools.GetDeclaredFields(typeof(Config));
            foreach (FieldInfo configField in configFields)
            {
                // Ignore all fields that aren't actually config values.
                if (!configField.FieldType.IsGenericType || !configField.FieldType.GetGenericTypeDefinition().IsAssignableFrom(typeof(ConfigEntryWrapper<>)))
                    continue;
                // Ignore all UI fields as those are meant to be editable at runtime and thus do not have to be serialised.
                if (((ConfigEntryWrapperBase)configField.GetValue(config)).GetSection() == Config.SectionUI)
                    continue;
                
                // Find the field in this struct of the same name.
                FieldInfo saveField = AccessTools.Field(typeof(ConfigSave), configField.Name);
                if (saveField is null || configField.FieldType.GetGenericArguments()[0] != saveField.FieldType)
                    throw new ConfigEntryException($"Failed to find field in SaveData corresponding to {configField.Name}: "
                                                   + $"Types were '{configField.FieldType.GetGenericArguments()[0]}' and '{saveField?.FieldType}'");
            }
        }

        /// <summary>
        /// Try to get a value from this saved config.
        /// </summary>
        /// <param name="key">The option's key, corresponding to the field name in this class.</param>
        /// <param name="value">The value of the requested option.</param>
        /// <param name="type">The typing of the requested option.</param>
        /// <returns>True if an option with the given key exists, false if it does not.</returns>
        public bool TryGetSavedValue(string key, out object value, out Type type)
        {
            FieldInfo entry = AccessTools.GetDeclaredFields(typeof(ConfigSave)).Find(info => info.Name == key);
            if (entry is null)
            {
                value = null;
                type = null;
                return false;
            }

            value = entry.GetValue(this);
            type = entry.FieldType;
            return true;
        }
    }
}