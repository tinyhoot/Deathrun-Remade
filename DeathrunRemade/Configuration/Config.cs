using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using DeathrunRemade.Components;
using DeathrunRemade.Handlers;
using DeathrunRemade.Items;
using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Enums;
using DeathrunRemade.Patches;
using HootLib;
using HootLib.Configuration;
using Nautilus.Handlers;
using UnityEngine;
using UnityEngine.UI;

namespace DeathrunRemade.Configuration
{
    internal class Config : HootConfig
    {
        // Section Keys. Intentionally static readonly to enable quick equality checks by reference.
        public static readonly string SectionChallenges = "Challenges";
        public static readonly string SectionCosts = "Costs";
        public static readonly string SectionEnvironment = "Environment";
        public static readonly string SectionSurvival = "Survival";
        public static readonly string SectionUI = "UI";

        // Survival
        public ConfigEntryWrapper<Difficulty3> PersonalCrushDepth;
        public ConfigEntryWrapper<DamageDifficulty> DamageTaken;
        public ConfigEntryWrapper<Difficulty3> NitrogenBends;
        public ConfigEntryWrapper<bool> SpecialAirTanks;
        public ConfigEntryWrapper<Difficulty3> SurfaceAir;
        public ConfigEntryWrapper<string> StartLocation;
        public ConfigEntryWrapper<bool> SinkLifepod;
        public ConfigEntryWrapper<bool> ToppleLifepod;

        // Environment
        public ConfigEntryWrapper<Difficulty4> CreatureAggression;
        public ConfigEntryWrapper<Murkiness> WaterMurkiness;
        public ConfigEntryWrapper<Difficulty3> ExplosionDepth;
        public ConfigEntryWrapper<Timer> ExplosionTime;
        public ConfigEntryWrapper<Difficulty4> RadiationDepth;
        public ConfigEntryWrapper<RadiationVisuals> RadiationFX;

        // Costs
        public ConfigEntryWrapper<Difficulty4> ToolCosts;
        public ConfigEntryWrapper<Difficulty4> VehicleCosts;
        public ConfigEntryWrapper<Difficulty4> ScansRequired;
        public ConfigEntryWrapper<Difficulty4> BatteryCosts;
        public ConfigEntryWrapper<Difficulty4> BatteryCapacity;
        public ConfigEntryWrapper<Difficulty4> PowerCosts;
        public ConfigEntryWrapper<Difficulty4> VehicleExitPowerLoss;

        // Challenges
        public ConfigEntryWrapper<Difficulty3> FarmingChallenge;
        public ConfigEntryWrapper<DietPreference> FoodChallenge;
        public ConfigEntryWrapper<RelativeToExplosion> IslandFoodChallenge;
        public ConfigEntryWrapper<Difficulty3> FilterPumpChallenge;
        public ConfigEntryWrapper<bool> NoVehicleChallenge;
        public ConfigEntryWrapper<bool> PacifistChallenge;

        // UI
        public ConfigEntryWrapper<bool> ShowTutorials;
        public ConfigEntryWrapper<Hints> ShowWarnings;
        public ConfigEntryWrapper<float> ExplosionWindowPosX;
        public ConfigEntryWrapper<float> ExplosionWindowPosY;
        public ConfigEntryWrapper<bool> MoveSunbeamWindow;

        internal readonly List<StartLocation> _startLocations;
        private Color _disabledOptionTint = new Color(1f, 0.4f, 0.4f, 0.15f);

        public Config(string path, BepInPlugin metadata) : base(path, metadata)
        {
            // Read the CSV file containing all possible random starts from disk.
            using CsvParser parser = new CsvParser(Hootils.GetAssetHandle("DeathrunStarts.csv"));
            _startLocations = parser.ParseAllLines<StartLocation>().ToList();
            // Add two extra options which aren't explicit spawn locations. They'll need special handling later on.
            _startLocations.Insert(0, new StartLocation("Vanilla", 0, 0, 0));
            _startLocations.Insert(1, new StartLocation("Random", 0, 0, 0));
            
            Setup();
        }

        protected override void RegisterOptions()
        {
            RegisterSurvivalOptions();
            RegisterEnvironmentOptions();
            RegisterCostOptions();
            RegisterChallengeOptions();
            RegisterUIOptions();
        }

        private void RegisterSurvivalOptions()
        {
            PersonalCrushDepth = RegisterEntry(
                section: SectionSurvival,
                key: nameof(PersonalCrushDepth),
                defaultValue: Difficulty3.Deathrun,
                description: $"You have a personal crush depth of {CrushDepthHandler.SuitlessCrushDepth:F0}m and need "
                             + $"to craft advanced suits to survive greater depths. "
                             + $"On Deathrun, your suit will require more upgrades."
            ).WithChoiceOptionStringsOverride(
                "Normal (Inactive)",
                "Hard (Reinforced Suit)",
                "Deathrun (Adv. Reinforced Suits)"
            ).WithDescription("Personal Crush Depth");
            DamageTaken = RegisterEntry(
                section: SectionSurvival,
                key: nameof(DamageTaken),
                defaultValue: DamageDifficulty.Deathrun,
                description: "Increase damage taken from all sources and decrease respawn health."
            ).WithChoiceOptionStringsOverride(
                "Normal (No changes)",
                $"Love Taps ({DamageTakenPatcher.GetDamageMult(DamageDifficulty.LoveTaps).Item1}x - {DamageTakenPatcher.GetDamageMult(DamageDifficulty.LoveTaps).Item2}x)",
                $"Hard ({DamageTakenPatcher.GetDamageMult(DamageDifficulty.Hard).Item1}x - {DamageTakenPatcher.GetDamageMult(DamageDifficulty.Hard).Item2}x)",
                $"Deathrun ({DamageTakenPatcher.GetDamageMult(DamageDifficulty.Deathrun).Item1}x - {DamageTakenPatcher.GetDamageMult(DamageDifficulty.Deathrun).Item2}x)",
                $"Kharaa ({DamageTakenPatcher.GetDamageMult(DamageDifficulty.Kharaa).Item1}x - {DamageTakenPatcher.GetDamageMult(DamageDifficulty.Kharaa).Item2}x)"
            ).WithDescription("Damage Taken");
            NitrogenBends = RegisterEntry(
                section: SectionSurvival,
                key: nameof(NitrogenBends),
                defaultValue: Difficulty3.Deathrun,
                description: "You get decompression sickness and take damage from surfacing too quickly. Ascend slowly "
                             + "and take care to let your safe diving depth adjust."
            ).WithChoiceOptionStringsOverride(
                "Normal (Inactive)",
                "Hard (Slower, lenient)",
                "Deathrun (Strict)"
            ).WithDescription("Nitrogen and the Bends");
            SurfaceAir = RegisterEntry(
                section: SectionSurvival,
                key: nameof(SurfaceAir),
                defaultValue: Difficulty3.Hard,
                description: "The surface air is unbreathable without a filter pump or an integrated filter chip."
            ).WithChoiceOptionStringsOverride(
                "Normal (Inactive)",
                "Hard (While Aurora not fixed)",
                "Deathrun (Always)"
            ).WithDescription("Surface Air Poisoning");
            SpecialAirTanks = RegisterEntry(
                section: SectionSurvival,
                key: nameof(SpecialAirTanks),
                defaultValue: true,
                description: "Add new special air tanks which regenerate oxygen under the right conditions, like "
                             + "sunlight or heat. This setting will not affect your score."
            ).WithDescription("Enable Special Air Tanks");
            StartLocation = RegisterEntry(
                section: SectionSurvival,
                key: nameof(StartLocation),
                defaultValue: "Random",
                description: "The spawn point of the lifepod. These are all hand-picked locations.",
                acceptableValues: new AcceptableValueList<string>(_startLocations.Select(l => l.Name).ToArray())
            );
            SinkLifepod = RegisterEntry(
                section: SectionSurvival,
                key: nameof(SinkLifepod),
                defaultValue: true,
                description: "Make the lifepod sink to the bottom of the ocean."
            ).WithDescription("Sink Lifepod");
            ToppleLifepod = RegisterEntry(
                section: SectionSurvival,
                key: nameof(ToppleLifepod),
                defaultValue: true,
                description: "Tilt the lifepod once it is done sinking. This setting will not affect your score."
            ).WithDescription("Topple Lifepod");
        }

        private void RegisterEnvironmentOptions()
        {
            CreatureAggression = RegisterEntry(
                section: SectionEnvironment,
                key: nameof(CreatureAggression),
                defaultValue: Difficulty4.Deathrun,
                description: "Creatures become more aggressive after 20 and 40 minutes and receive buffs to their vision. \n"
                             + "Normal: No changes. \n"
                             + "Hard: Creatures see you from further away. \n"
                             + "Deathrun: Creatures can sense you even when you're behind them. \n"
                             + "Kharaa: Creatures can sense you even through terrain."
            ).WithDescription("Creature Aggression");
            WaterMurkiness = RegisterEntry(
                section: SectionEnvironment,
                key: nameof(WaterMurkiness),
                defaultValue: Murkiness.Normal,
                description: "Increase the murkiness of the water, making it darker and more difficult to see."
            );
            ExplosionDepth = RegisterEntry(
                section: SectionEnvironment,
                key: nameof(ExplosionDepth),
                defaultValue: Difficulty3.Deathrun,
                description: "Controls how deep the Aurora's explosion reaches.\n"
                             + "Normal: No changes\n"
                             + $"Hard: About {ExplosionPatcher.GetExplosionDepth(Difficulty3.Hard)}m\n"
                             + $"Deathrun: About {ExplosionPatcher.GetExplosionDepth(Difficulty3.Deathrun)}m"
            ).WithDescription(
                "Explosion Depth",
                "Aurora explosion reaches below the surface. Strength varies with depth, and is further reduced "
                + "if you're somewhere inside."
            ).WithChoiceOptionStringsOverride(
                "Normal (Inactive)",
                $"Hard ({ExplosionPatcher.GetExplosionDepth(Difficulty3.Hard)}m)",
                $"Deathrun ({ExplosionPatcher.GetExplosionDepth(Difficulty3.Deathrun)}m)"
            );
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
            ).WithChoiceOptionStringsOverride(
                "Vanilla (46-80min)",
                $"Short ({ExplosionPatcher.GetExplosionTime(Timer.Short)}min)",
                $"Medium ({ExplosionPatcher.GetExplosionTime(Timer.Medium)}min)",
                $"Long ({ExplosionPatcher.GetExplosionTime(Timer.Long)}min)"
            );
            RadiationDepth = RegisterEntry(
                section: SectionEnvironment,
                key: nameof(RadiationDepth),
                defaultValue: Difficulty4.Deathrun,
                description: "Radiation affects the water even when not near the Aurora up to a certain depth."
            ).WithChoiceOptionStringsOverride(
                "Normal (No changes)",
                $"Hard ({RadiationPatcher.GetMaxRadiationDepth(Difficulty4.Hard)}m)",
                $"Deathrun ({RadiationPatcher.GetMaxRadiationDepth(Difficulty4.Deathrun)}m)",
                $"Kharaa ({RadiationPatcher.GetMaxRadiationDepth(Difficulty4.Kharaa)}m)"
            ).WithDescription("Radiation");
            RadiationFX = RegisterEntry(
                section: SectionEnvironment,
                key: nameof(RadiationFX),
                defaultValue: RadiationVisuals.Chernobyl,
                description: "Determines what kind of radiation visuals to display while immune to radiation. On the "
                             + "highest setting, the effects get worse as you approach the Aurora and make it "
                             + "difficult to see while inside."
            ).WithChoiceOptionStringsOverride(
                "Normal (No visuals)",
                "Mild Reminder",
                "Chernobyl (Strong Interference)"
            ).WithDescription("Radiation FX if immune");
        }

        private void RegisterCostOptions()
        {
            ToolCosts = RegisterEntry(
                section: SectionCosts,
                key: nameof(ToolCosts),
                defaultValue: Difficulty4.Deathrun,
                description: "Harder recipes for important tools and buildings like habitat builder and reactors."
            ).WithDescription("Tool and Building Costs");
            VehicleCosts = RegisterEntry(
                section: SectionCosts,
                key: nameof(VehicleCosts),
                defaultValue: Difficulty4.Deathrun,
                description: "Harder recipes for vehicles and vehicle modules."
            ).WithChoiceOptionStringsOverride(
                "Normal (No changes)",
                "Hard (No changes to modules)",
                "Deathrun",
                "Kharaa"
            ).WithDescription("Vehicle Costs");
            ScansRequired = RegisterEntry(
                section: SectionCosts,
                key: nameof(ScansRequired),
                defaultValue: Difficulty4.Deathrun,
                description: "Increase the number of fragment scans required for almost all fragments in the game."
            ).WithDescription("Required Fragment Scans");
            BatteryCosts = RegisterEntry(
                section: SectionCosts,
                key: nameof(BatteryCosts),
                defaultValue: Difficulty4.Deathrun,
                description: "Batteries cost more with higher difficulties. Adds non-rechargeable copper batteries. "
                             + "Tools do not automatically contain batteries."
            ).WithDescription("Battery Costs");
            BatteryCapacity = RegisterEntry(
                section: SectionCosts,
                key: nameof(BatteryCapacity),
                defaultValue: Difficulty4.Deathrun,
                description: "Copper batteries hold less power with higher difficulties."
            ).WithChoiceOptionStringsOverride(
                $"Normal ({AcidBattery.GetCapacityForDifficulty(Difficulty4.Normal)} power)",
                $"Hard ({AcidBattery.GetCapacityForDifficulty(Difficulty4.Hard)} power)",
                $"Deathrun ({AcidBattery.GetCapacityForDifficulty(Difficulty4.Deathrun)} power)",
                $"Kharaa ({AcidBattery.GetCapacityForDifficulty(Difficulty4.Kharaa)} power)"
            ).WithDescription("Battery Capacity");
            PowerCosts = RegisterEntry(
                section: SectionCosts,
                key: nameof(PowerCosts),
                defaultValue: Difficulty4.Deathrun,
                description: "Increase all power costs and even more so while irradiated. Recharge speeds are also slower."
            ).WithChoiceOptionStringsOverride(
                "Normal (No changes)",
                $"Hard ({PowerPatcher.GetPowerCostMult(Difficulty4.Hard, false)}x, "
                + $"{PowerPatcher.GetPowerCostMult(Difficulty4.Hard, true)}x while irradiated)",
                $"Deathrun ({PowerPatcher.GetPowerCostMult(Difficulty4.Deathrun, false)}x, "
                + $"{PowerPatcher.GetPowerCostMult(Difficulty4.Deathrun, true)}x while irradiated)",
                $"Kharaa ({PowerPatcher.GetPowerCostMult(Difficulty4.Kharaa, false)}x, "
                + $"{PowerPatcher.GetPowerCostMult(Difficulty4.Kharaa, true)}x while irradiated)"
            ).WithDescription("Power Costs");
            VehicleExitPowerLoss = RegisterEntry(
                section: SectionCosts,
                key: nameof(VehicleExitPowerLoss),
                defaultValue: Difficulty4.Deathrun,
                description: "Vehicles lose power when you exit them outside of a docking bay like the moonpool or "
                             + "cyclops. The power loss depends on your current depth and can be reduced with "
                             + "Decompression Modules."
            ).WithChoiceOptionStringsOverride(
                "Normal (No changes)",
                $"Hard (Depth / {PowerPatcher.GetVehicleExitCostDiv(Difficulty4.Hard, true)})",
                $"Deathrun (Depth / {PowerPatcher.GetVehicleExitCostDiv(Difficulty4.Deathrun, true)})",
                $"Kharaa (Depth / {PowerPatcher.GetVehicleExitCostDiv(Difficulty4.Kharaa, true)})"
            ).WithDescription("Power Loss on Vehicle Exit");
        }

        private void RegisterChallengeOptions()
        {
            FoodChallenge = RegisterEntry(
                section: SectionChallenges,
                key: nameof(FoodChallenge),
                defaultValue: DietPreference.Omnivore,
                description: "Restricts your diet to only certain types of food. \n"
                             + "Omnivore: You can eat everything. \n"
                             + "Radical Pescatarian: You can only eat fish. \n"
                             + "Vegetarian: You can only eat plants and nutrient blocks. \n"
                             + "Vegan: You can only eat plants."
            ).WithDescription("Challenge: Diet Restrictions");
            FarmingChallenge = RegisterEntry(
                section: SectionChallenges,
                key: nameof(FarmingChallenge),
                defaultValue: Difficulty3.Normal,
                description: "Plants grow more slowly, making it more difficult to farm them."
            ).WithChoiceOptionStringsOverride(
                "Normal (No changes)",
                $"Hard (1/{FarmingChallengePatcher.GetDurationMult(Difficulty3.Hard)}x speed)",
                $"Deathrun (1/{FarmingChallengePatcher.GetDurationMult(Difficulty3.Deathrun)}x speed)"
            ).WithDescription("Challenge: Farming");
            IslandFoodChallenge = RegisterEntry(
                section: SectionChallenges,
                key: nameof(IslandFoodChallenge),
                defaultValue: RelativeToExplosion.Always,
                description: "Food from the floating island is inedible when irradiated."
            ).WithChoiceOptionStringsOverride(
                "Never edible",
                "Edible after fixing reactor",
                "Edible before and after radiation",
                "Always edible"
            ).WithDescription("Challenge: Island Food");
            FilterPumpChallenge = RegisterEntry(
                section: SectionChallenges,
                key: nameof(FilterPumpChallenge),
                defaultValue: Difficulty3.Normal,
                description: "The filter pump will not work in heavily irradiated areas. \n"
                             + "Hard: Will not work while inside the Aurora. \n"
                             + "Deathrun: Will not work inside the Aurora's Radiation Zone (not including the extra "
                             + "depth-based radiation)"
            ).WithChoiceOptionStringsOverride(
                "Normal (No changes)",
                "Hard (Not inside Aurora)",
                "Deathrun (Not near Aurora)"
            ).WithDescription("Challenge: Filter Pump");
            NoVehicleChallenge = RegisterEntry(
                section: SectionChallenges,
                key: nameof(NoVehicleChallenge),
                defaultValue: false,
                description: "You cannot build vehicles until you've acquired Hatching Enzymes."
            ).WithDescription("Challenge: No Vehicles");
            PacifistChallenge = RegisterEntry(
                section: SectionChallenges,
                key: nameof(PacifistChallenge),
                defaultValue: false,
                description: "You cannot hurt animals; your knife does zero damage."
            ).WithDescription("Challenge: Pacifist");
        }

        private void RegisterUIOptions()
        {
            ShowTutorials = RegisterEntry(
                section: SectionUI,
                key: nameof(ShowTutorials),
                defaultValue: true,
                description: "Show tutorial messages when something unique to Deathrun happens for the first time."
            ).WithDescription("Show Tutorials");
            ShowWarnings = RegisterEntry(
                section: SectionUI,
                key: nameof(ShowWarnings),
                defaultValue: Hints.Always,
                description: "Show warnings when you are about to do something harmful, like ascending very quickly."
            ).WithDescription("Show Warnings");
            ExplosionWindowPosX = RegisterEntry(
                section: SectionUI,
                key: nameof(ExplosionWindowPosX),
                defaultValue: 0.03f,
                description: "Sets the distance of any Deathrun countdown windows from the left edge of the screen."
            ).WithDescription("Countdown Windows Horiz. Position");
            ExplosionWindowPosY = RegisterEntry(
                section: SectionUI,
                key: nameof(ExplosionWindowPosY),
                defaultValue: 0.2f,
                description: "Sets the distance of any Deathrun countdown windows from the top edge of the screen."
            ).WithDescription("Countdown Windows Vert. Position");
            MoveSunbeamWindow = RegisterEntry(
                section: SectionUI,
                key: nameof(MoveSunbeamWindow),
                defaultValue: true,
                description: "If enabled, sets the window of the sunbeam arrival countdown to the same position as any "
                             + "Deathrun countdown windows. It overlaps with stickied blueprints in the default position."
            ).WithDescription("Also move Sunbeam window?");
        }

        protected override void RegisterControllingOptions() { }

        public override void RegisterModOptions(string name, Transform persistentParent = null)
        {
            HootModOptions modOptions = new HootModOptions(name, this);
            modOptions.OnAddOptionToMenu += OnAddOptionToModMenu;
            
            modOptions.AddText("Choose carefully. Any options you set here lock in and <color=#FF0000FF>cannot</color>"
                               + " be changed during an ongoing game.");
            // Add a preview of the score multiplier, which updates whenever the user changes an option.
            var scorePreview = new ScoreMultPreviewText(this, "Your current settings grant you a score multiplier "
                                                              + "of <b>{0}</b>");
            modOptions.AddDecorator(scorePreview);
            
            modOptions.AddSeparator(persistentParent);
            modOptions.AddItem(PersonalCrushDepth.ToModChoiceOption());
            modOptions.AddItem(DamageTaken.ToModChoiceOption());
            modOptions.AddItem(NitrogenBends.ToModChoiceOption());
            modOptions.AddItem(SurfaceAir.ToModChoiceOption());
            modOptions.AddItem(SpecialAirTanks.ToModToggleOption());
            modOptions.AddItem(StartLocation.ToModChoiceOption());
            modOptions.AddItem(SinkLifepod.ToModToggleOption());
            modOptions.AddItem(ToppleLifepod.ToModToggleOption());
            
            modOptions.AddSeparator(persistentParent);
            modOptions.AddItem(CreatureAggression.ToModChoiceOption());
            modOptions.AddItem(WaterMurkiness.ToModChoiceOption());
            modOptions.AddItem(ExplosionDepth.ToModChoiceOption());
            modOptions.AddItem(ExplosionTime.ToModChoiceOption());
            modOptions.AddItem(RadiationDepth.ToModChoiceOption());
            modOptions.AddItem(RadiationFX.ToModChoiceOption());
            
            modOptions.AddSeparator(persistentParent);
            modOptions.AddItem(ToolCosts.ToModChoiceOption());
            modOptions.AddItem(VehicleCosts.ToModChoiceOption());
            modOptions.AddItem(ScansRequired.ToModChoiceOption());
            modOptions.AddItem(BatteryCosts.ToModChoiceOption());
            modOptions.AddItem(BatteryCapacity.ToModChoiceOption());
            modOptions.AddItem(PowerCosts.ToModChoiceOption());
            modOptions.AddItem(VehicleExitPowerLoss.ToModChoiceOption());
            
            modOptions.AddSeparator(persistentParent);
            modOptions.AddItem(FoodChallenge.ToModChoiceOption());
            modOptions.AddItem(FarmingChallenge.ToModChoiceOption());
            modOptions.AddItem(IslandFoodChallenge.ToModChoiceOption());
            modOptions.AddItem(FilterPumpChallenge.ToModChoiceOption());
            modOptions.AddItem(NoVehicleChallenge.ToModToggleOption());
            modOptions.AddItem(PacifistChallenge.ToModToggleOption());
            
            modOptions.AddSeparator(persistentParent);
            modOptions.AddText("These options can always be changed and will take effect immediately, even during a "
                               + "run. They do not affect your score.");
            modOptions.AddItem(ShowTutorials.ToModToggleOption());
            modOptions.AddItem(ShowWarnings.ToModChoiceOption());
            var windowPosHSlider = ExplosionWindowPosX.ToModSliderOption(0f, 1f, stepSize: 0.01f);
            var windowPosVSlider = ExplosionWindowPosY.ToModSliderOption(0f, 1f, stepSize: 0.01f);
            // Update all countdown windows whenever these position settings are changed.
            windowPosHSlider.OnChanged += ExplosionCountdown.OnUpdateSettingsX;
            windowPosVSlider.OnChanged += ExplosionCountdown.OnUpdateSettingsY;
            windowPosHSlider.OnChanged += (obj, args) => CountdownPatcher.MoveSunbeamCountdownWindow();
            windowPosVSlider.OnChanged += (obj, args) => CountdownPatcher.MoveSunbeamCountdownWindow();
            modOptions.AddItem(windowPosHSlider);
            modOptions.AddItem(windowPosVSlider);
            modOptions.AddItem(MoveSunbeamWindow.ToModToggleOption());

            OptionsPanelHandler.RegisterModOptions(modOptions);
        }

        private void OnAddOptionToModMenu(AddOptionToMenuEventArgs args)
        {
            if (args.IsMainMenu)
                return;
            
            ConfigEntryWrapperBase entry = GetEntryById(args.ID);
            // Don't do anything to UI options, those are always editable.
            if (entry.GetSection() == SectionUI)
                return;
            
            DeathrunInit._Log.Debug($"Locking down menu option '{args.ID}'.");
            // If the mod options menu is brought up in-game let the user look at, but not modify, the settings.
            HootModOptions.DisableOption(args.GameObject, _disabledOptionTint);
            // Additionally, change the value to the one that has been locked in for the current save.
            if (!SaveData.Main.Config.TryGetSavedValue(entry.GetKey(), out object value, out Type type))
            {
                DeathrunInit._Log.Warn($"Tried to adjust displayed value of in-game option '{args.ID}', but "
                                       + $"failed to find saved value!");
                return;
            }
            
            ChangeDisplayedOptionValue(args.GameObject, value, type);
        }

        private void ChangeDisplayedOptionValue(GameObject option, object value, Type type)
        {
            // Check for each of the different types of option this mod uses. Only one of these exists for each option.
            Toggle toggle = option.GetComponentInChildren<Toggle>();
            if (toggle != null)
            {
                toggle.SetIsOnWithoutNotify((bool)value);
                return;
            }
            
            uGUI_Choice choice = option.GetComponentInChildren<uGUI_Choice>();
            if (choice != null)
            {
                // Stop these changes from reaching the config file. Has no impact beyond this menu since all objects
                // are destroyed once it closes and the next rebuild will have the listeners on it again.
                choice.onValueChanged.RemoveAllListeners();
                
                // Try to set the value for enum-based choices.
                if (type.IsEnum || typeof(int).IsAssignableFrom(type))
                {
                    choice.value = (int)value;
                }
                else
                {
                    // Handle string-based choices.
                    DeathrunInit._Log.Debug("Using fallback choice override.");
                    choice.SetOptions(new[] { value.ToString() });
                    choice.value = 0;
                }

                return;
            }

            uGUI_SnappingSlider slider = option.GetComponentInChildren<uGUI_SnappingSlider>();
            if (slider != null)
            {
                slider.SetValueWithoutNotify((float)value);
                return;
            }
            DeathrunInit._Log.Warn($"Tried to adjust displayed value of in-game option {option.name} but failed "
                                   + $"to find option type.");
        }
    }
}