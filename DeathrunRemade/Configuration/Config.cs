using BepInEx;
using BepInEx.Configuration;
using DeathrunRemade.Objects.Enums;
using DeathrunRemade.Patches;
using HootLib.Configuration;
using Nautilus.Handlers;
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
        public ConfigEntryWrapper<Difficulty3> ExplosionDepth;
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
            // Survival
            CrushDepth = RegisterEntry(
                section: SectionSurvival,
                key: nameof(CrushDepth),
                defaultValue: Difficulty3.Deathrun,
                description: ""
            );
            NitrogenBends = RegisterEntry(
                section: SectionSurvival,
                key: nameof(NitrogenBends),
                defaultValue: Difficulty3.Deathrun,
                description: ""
            );
            SurfaceAir = RegisterEntry(
                section: SectionSurvival,
                key: nameof(SurfaceAir),
                defaultValue: Difficulty3.Hard,
                description: ""
            );

            // Environment
            ExplosionDepth = RegisterEntry(
                section: SectionEnvironment,
                key: nameof(ExplosionDepth),
                defaultValue: Difficulty3.Deathrun,
                description: "Controls how deep the Aurora's explosion reaches.\n"
                             + "Normal: No changes\n"
                             + $"Hard: About {ExplosionPatcher.GetExplosionDepth(Difficulty3.Hard)}\n"
                             + $"Deathrun: About {ExplosionPatcher.GetExplosionDepth(Difficulty3.Deathrun)}m"
            ).WithDescription(
                "Explosion Depth",
                "Aurora explosion reaches below the surface. Strength varies with depth, and is further reduced "
                + "if you're somewhere inside."
            ).WithChoiceOptionStringsOverride(new []
            {
                "Normal (No changes)",
                $"Hard ({ExplosionPatcher.GetExplosionDepth(Difficulty3.Hard)}m)",
                $"Deathrun ({ExplosionPatcher.GetExplosionDepth(Difficulty3.Deathrun)}m)"
            });
            ExplosionTime = RegisterEntry(
                section: SectionEnvironment,
                key: nameof(ExplosionTime),
                defaultValue: Timer.Short,
                description: "How long it takes for the Aurora to explode.\n"
                             + "Vanilla: Just like usual, randomly in the range of 2-4 days (46-80 minutes)"
                             + $"Short: {ExplosionPatcher.GetExplosionTime(Timer.Short)}min\n"
                             + $"Medium: {ExplosionPatcher.GetExplosionTime(Timer.Medium)}min\n"
                             + $"Long: {ExplosionPatcher.GetExplosionTime(Timer.Long)}min"
            ).WithDescription(
                "Time to Explosion",
                "How long it takes for the Aurora to explode."
            ).WithChoiceOptionStringsOverride(new[]
            {
                "Vanilla (46-80min)",
                $"Short ({ExplosionPatcher.GetExplosionTime(Timer.Short)}min)",
                $"Medium ({ExplosionPatcher.GetExplosionTime(Timer.Medium)}min)",
                $"Long ({ExplosionPatcher.GetExplosionTime(Timer.Long)}min)",
            });
            
            // Costs
            BatteryCapacity = RegisterEntry(
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
            });

            // UI
            ShowWarnings = RegisterEntry(
                section: SectionUI,
                key: nameof(ShowWarnings),
                defaultValue: Hints.Always,
                description: ""
            );
        }

        protected override void RegisterControllingOptions() { }

        public override void RegisterModOptions(string name, Transform separatorParent = null)
        {
            HootModOptions modOptions = new HootModOptions(name, this, separatorParent);
            modOptions.AddItem(CrushDepth.ToModChoiceOption(modOptions));
            modOptions.AddItem(ExplosionDepth.ToModChoiceOption(modOptions));
            modOptions.AddItem(ExplosionTime.ToModChoiceOption(modOptions));
            modOptions.AddItem(SurfaceAir.ToModChoiceOption(modOptions));
            modOptions.AddItem(BatteryCapacity.ToModChoiceOption(modOptions));
            
            modOptions.AddSeparatorBeforeOption(ExplosionDepth.GetId());

            OptionsPanelHandler.RegisterModOptions(modOptions);
        }
    }
}