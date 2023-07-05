using BepInEx;
using BepInEx.Configuration;
using Nautilus.Handlers;
using SubnauticaCommons.Configuration;
using UnityEngine;

namespace DeathrunRemade.Configuration
{
    internal class Config : HootConfig
    {
        public ConfigEntryWrapper<int> DamageTaken;

        public Config(ConfigFile configFile) : base(configFile) { }
        public Config(string path, BepInPlugin metadata) : base(path, metadata) { }

        protected override void RegisterOptions()
        {
            DamageTaken = RegisterEntry(new ConfigEntryWrapper<int>(
                configFile: ConfigFile,
                section: "TestSection",
                key: "Test",
                defaultValue: 0,
                description: "Hello."
            ));
        }

        protected override void RegisterControllingOptions()
        {
            
        }

        public override void RegisterModOptions(string name, Transform separatorParent)
        {
            HootModOptions modOptions = new HootModOptions(name, this, separatorParent);
            modOptions.AddItem(DamageTaken.ToModSliderOption(0f, 10f));

            OptionsPanelHandler.RegisterModOptions(modOptions);
        }
    }
}