using System;
using BepInEx;
using SubnauticaCommons;
using SubnauticaCommons.Configuration;

namespace DeathrunRemade
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("com.snmodding.nautilus", "1.0")]
    internal class DeathrunInit : BaseUnityPlugin
    {
        public const string GUID = "com.github.tinyhoot.DeathrunRemade";
        public const string NAME = "Deathrun Remade";
        public const string VERSION = "0.1";

        internal HootConfig _Config;
        internal HootLogger _Log;
        
        private void Awake()
        {
            Logger.LogDebug("AAAHHHHH");
            _Log = new HootLogger(NAME);
            _Log.Debug("Hey wow I'm alive!");
        }
    }
}