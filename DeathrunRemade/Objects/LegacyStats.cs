using System.Collections.Generic;
using DeathrunRemade.Objects.Enums;

namespace DeathrunRemade.Objects
{
    /// <summary>
    /// For loading legacy stats from an original Deathrun Stats.json file.
    /// </summary>
    internal struct LegacyStats
    {
        // Seriously we do not need warnings about variables that never get assigned, that's kind of the whole point here.
#pragma warning disable CS0649
        public int ID;
        public string Start;
        public string Cause;
        public float RunTime;
        public float Deepest;
        public int Deaths;
        public int Score;
        public int BestVehicle;
        public int VehicleFlags;
        public int DeathRunSettingCount;
        public int DeathRunSettingBonus;
        public bool Victory;
        public string Version;
#pragma warning restore CS0649

        /// <summary>
        /// Convert a legacy run to the modern format.
        /// </summary>
        public RunStats ToModernStats()
        {
            RunStats stats = new RunStats
            {
                // Give legacy stats a negative id to distinguish them.
                id = -ID,
                startPoint = Start,
                causeOfDeath = Cause,
                time = RunTime,
                depthReached = Deepest,
                deaths = Deaths,
                scoreBase = Score,
                scoreMult = 1f,
                achievements = (RunAchievements)VehicleFlags,
                legacySettingsCount = DeathRunSettingCount,
                victory = Victory,
                version = "Legacy",
                isLegacy = true,
                gameMode = GameModeOption.Survival,
            };
            return stats;
        }
    }

    internal struct LegacyStatsFile
    {
#pragma warning disable CS0649
        public bool VeryFirstTime;
        public int RunCounter;
        public int RecentIndex;
        public List<LegacyStats> HighScores;
        public string Version;
#pragma warning restore CS0649
    }
}