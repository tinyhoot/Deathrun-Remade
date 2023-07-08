using BepInEx;
using BepInEx.Configuration;
using DeathrunRemade.Objects.Enums;
using Nautilus.Handlers;
using HootLib.Configuration;
using UnityEngine;

namespace DeathrunRemade.Configuration
{
    internal class Config : HootConfig
    {
        // Section Keys. Intentionally static readonly to enable quick equality checks by reference.
        public static readonly string SectionChallenges = "Challenges";
        public static readonly string SectionCosts = "Costs";
        public static readonly string SectionEnvironment = "Environment";
        public static readonly string SectionExploRad = "Explosion_Radiation";
        public static readonly string SectionSurvival = "Survival";
        public static readonly string SectionUI = "UI";
        
        // Survival
        // Only temporarily, so I can actually see when something else doesn't work.
#pragma warning disable CS0649
        public ConfigEntryWrapper<Difficulty3> CrushDepth;
        public ConfigEntryWrapper<Difficulty4> DamageTaken;
        public ConfigEntryWrapper<Difficulty3> NitrogenBends;
        public ConfigEntryWrapper<bool> SpecialAirTanks;
        public ConfigEntryWrapper<Difficulty3> SurfaceAir;
        
        // Environment
        public ConfigEntryWrapper<Difficulty4> CreatureAggression;
        public ConfigEntryWrapper<DeathrunStarts> StartLocation;
        public ConfigEntryWrapper<bool> ToppleLifepod;
        public ConfigEntryWrapper<Murkiness> WaterMurkiness;

        // Explosion and Radiation
        public ConfigEntryWrapper<Difficulty3> ExplodeDepth;
        public ConfigEntryWrapper<Timer> ExplosionTime;
        public ConfigEntryWrapper<Difficulty4> RadiationDepth;
        public ConfigEntryWrapper<RadiationVisuals> RadiationFX;

        // Costs
        public ConfigEntryWrapper<Difficulty4> BatteryCapacity;
        public ConfigEntryWrapper<Difficulty3> BuilderCosts;
        public ConfigEntryWrapper<Difficulty4> PowerCosts;
        public ConfigEntryWrapper<Difficulty4> ScansRequired;
        public ConfigEntryWrapper<VehicleDifficulty> VehicleCosts;
        public ConfigEntryWrapper<Difficulty4> VehicleExitPowerLoss;
        
        // Challenges
        public ConfigEntryWrapper<Difficulty3> FarmingChallenge;
        public ConfigEntryWrapper<Difficulty3> FilterPumpChallenge;
        public ConfigEntryWrapper<DietPreference> FoodChallenge;
        public ConfigEntryWrapper<RelativeToExplosion> IslandFoodChallenge;
        public ConfigEntryWrapper<bool> PacifistChallenge;
        
        // UI
        public ConfigEntryWrapper<bool> ShowHighscores;
        public ConfigEntryWrapper<bool> ShowHighscoreTips;
        public ConfigEntryWrapper<Hints> ShowWarnings;

        public Config(ConfigFile configFile) : base(configFile) { }
        public Config(string path, BepInPlugin metadata) : base(path, metadata) { }

        protected override void RegisterOptions()
        {
            SurfaceAir = RegisterEntry(new ConfigEntryWrapper<Difficulty3>(
                configFile: ConfigFile,
                section: SectionSurvival,
                key: nameof(SurfaceAir),
                defaultValue: Difficulty3.Hard,
                description: ""
            ));
            
            BatteryCapacity = RegisterEntry(new ConfigEntryWrapper<Difficulty4>(
                configFile: ConfigFile,
                section: SectionCosts,
                key: nameof(BatteryCapacity),
                defaultValue: Difficulty4.Deathrun,
                description: ""
            ).WithDescription(
                "Battery Cost",
                ""
            ).WithChoiceOptionStringsOverride(new []
            {
                "Very normal",
                "A bit less normal",
                "Ah. Not normal",
                "TAKE ME BACK"
            }));
        }

        protected override void RegisterControllingOptions() { }

        public override void RegisterModOptions(string name, Transform separatorParent)
        {
            HootModOptions modOptions = new HootModOptions(name, this, separatorParent);
            modOptions.AddItem(SurfaceAir.ToModChoiceOption(modOptions));
            modOptions.AddItem(BatteryCapacity.ToModChoiceOption(modOptions));

            OptionsPanelHandler.RegisterModOptions(modOptions);
        }
    }
}