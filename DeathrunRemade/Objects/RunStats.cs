using System;

namespace DeathrunRemade.Objects
{
    [Serializable]
    internal struct RunStats
    {
        public string startPoint;
        public float time;
        public string causeOfDeath;
        public int deaths;
        public float scoreMult;
        public float score;
    }
}