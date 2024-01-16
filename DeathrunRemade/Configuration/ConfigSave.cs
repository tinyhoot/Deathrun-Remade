using System;
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
    [Serializable]
    internal readonly struct ConfigSave
    {
        // Survival
        public readonly Difficulty3 PersonalCrushDepth;
        public readonly DamageDifficulty DamageTaken;
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
        public readonly Difficulty4 BatteryCosts;
        public readonly Difficulty4 ToolCosts;
        public readonly Difficulty4 PowerCosts;
        public readonly Difficulty4 ScansRequired;
        public readonly Difficulty4 VehicleCosts;
        public readonly Difficulty4 VehicleExitPowerLoss;
        
        // Challenges
        public readonly Difficulty3 FarmingChallenge;
        public readonly Difficulty3 FilterPumpChallenge;
        public readonly DietPreference FoodChallenge;
        public readonly RelativeToExplosion IslandFoodChallenge;
        public readonly bool NoVehicleChallenge;
        public readonly bool PacifistChallenge;
        
        // UI
        public readonly bool ShowTutorials;
        public readonly Hints ShowWarnings;
        
        // This field defaults to false in any instance that was made using the default constructor rather than
        // serialisation from a config file.
        public readonly bool WasInitialised;

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
            
            ShowTutorials = config.ShowTutorials.Value;
            ShowWarnings = config.ShowWarnings.Value;
            
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
                // Ignore all UI fields as those do not have to be serialised.
                if (((ConfigEntryWrapperBase)configField.GetValue(config)).GetSection().Equals(Config.SectionUI))
                    continue;
                
                // Find the field in this struct of the same name.
                FieldInfo saveField = AccessTools.Field(typeof(ConfigSave), configField.Name);
                if (saveField is null || configField.FieldType.GetGenericArguments()[0] != saveField.FieldType)
                    throw new ConfigEntryException($"Failed to find field in SaveData corresponding to {configField.Name}: "
                                                   + $"Types were '{configField.FieldType.GetGenericArguments()[0]}' and '{saveField?.FieldType}'");
            }
        }
    }
}