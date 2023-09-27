using System;
using DeathrunRemade.Configuration;
using DeathrunRemade.Objects.Enums;

namespace DeathrunRemade.Objects
{
    /// <summary>
    /// Represents the stats stemming from a single run.
    /// </summary>
    [Serializable]
    internal struct RunStats
    {
        public RunAchievements achievements;
        public string causeOfDeath;
        public int deaths;
        public float depthReached;
        public float scoreBase;
        public float scoreMult;
        public float scoreBonus;
        public float scoreTotal;
        public string startPoint;
        public double time;
        public bool victory;
        
        // Technical information that isn't really about what the player did.
        public int id;
        public bool isLegacy;
        public int legacySettingsCount;
        public string version;
        public GameModeOption gameMode;
        public ConfigSave config;
    }
}