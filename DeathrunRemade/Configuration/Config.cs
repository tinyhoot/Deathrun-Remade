using BepInEx;
using BepInEx.Configuration;
using DeathrunRemade.Objects.Enums;
using Nautilus.Handlers;
using SubnauticaCommons.Configuration;
using UnityEngine;

namespace DeathrunRemade.Configuration
{
    internal class Config : HootConfig
    {
        public ConfigEntryWrapper<int> DamageTaken;
        public ConfigEntryWrapper<Difficulty> BatteryCapacity;

        public Config(ConfigFile configFile) : base(configFile) { }
        public Config(string path, BepInPlugin metadata) : base(path, metadata) { }

        protected override void RegisterOptions()
        {
            BatteryCapacity = RegisterEntry(new ConfigEntryWrapper<Difficulty>(
                configFile: ConfigFile,
                section: "Costs",
                key: nameof(BatteryCapacity),
                defaultValue: Difficulty.Deathrun,
                description: ""
                //acceptableValues: new AcceptableValueList<string>()
            ).WithDescription(
                "Battery Cost",
                ""
                ));
            DamageTaken = RegisterEntry(new ConfigEntryWrapper<int>(
                configFile: ConfigFile,
                section: "TestSection",
                key: "Test",
                defaultValue: 0,
                description: "Hello."
            ));
        }

        protected override void RegisterControllingOptions() { }

        public override void RegisterModOptions(string name, Transform separatorParent)
        {
            HootModOptions modOptions = new HootModOptions(name, this, separatorParent);
            modOptions.AddItem(DamageTaken.ToModSliderOption(0f, 10f));
            modOptions.AddItem(BatteryCapacity.ToModChoiceOption(modOptions, null));

            OptionsPanelHandler.RegisterModOptions(modOptions);
        }
    }
}