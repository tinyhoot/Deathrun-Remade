using System;

namespace DeathrunRemade.Objects
{
    /// <summary>
    /// Represents the stats stemming from a single run.
    /// </summary>
    [Serializable]
    internal struct RunStats
    {
        public string causeOfDeath;
        public int deaths;
        public float depthReached;
        public float scoreBase;
        public float scoreMult;
        public string startPoint;
        public float time;
        public bool victory;
        
        // Technical information that isn't really about what the player did.
        public int id;
        public string version;
        public string gameMode;
    }
}