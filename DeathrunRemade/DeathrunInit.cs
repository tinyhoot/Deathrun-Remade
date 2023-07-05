using System.Collections.Generic;
using System.IO;
using BepInEx;
using DeathrunRemade.Configuration;
using DeathrunRemade.Interfaces;
using DeathrunRemade.Items;
using SubnauticaCommons;
using SubnauticaCommons.Interfaces;

namespace DeathrunRemade
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("com.snmodding.nautilus", "1.0")]
    internal class DeathrunInit : BaseUnityPlugin
    {
        public const string GUID = "com.github.tinyhoot.DeathrunRemade";
        public const string NAME = "Deathrun Remade";
        public const string VERSION = "0.1";

        internal static Config _Config;
        internal static ILogHandler _Log;

        internal static Dictionary<string, IDeathrunPrefab> customItems;
        
        private void Awake()
        {
            _Log = new HootLogger(NAME);
            _Log.Info($"{NAME} v{VERSION} starting up.");
            
            // Registering config.
            _Config = new Config(Path.Combine(Paths.ConfigPath, Hootils.GetConfigFileName(NAME)),
                Info.Metadata);
            _Config.RegisterModOptions(NAME, transform);
            
            RegisterItems();
            
            _Log.Info("Finished loading.");
        }

        private void RegisterItems()
        {
            customItems = new Dictionary<string, IDeathrunPrefab>
            {
                { nameof(AcidBattery), new AcidBattery() },
            };
        }
    }
}