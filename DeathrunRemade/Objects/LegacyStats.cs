using System.Collections.Generic;
using System.IO;
using HootLib;

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
                id = -1,
                startPoint = Start,
                causeOfDeath = Cause,
                time = RunTime,
                depthReached = Deepest,
                deaths = Deaths,
                scoreBase = Score,
                victory = Victory,
                version = "Legacy"
            };
            return stats;
        }

        /// <summary>
        /// Try to find a legacy Deathrun stats file in a few likely locations.
        /// </summary>
        /// <returns>True if a file was found, false if not.</returns>
        public static bool TryFindLegacyStatsFile(out FileInfo legacyFile)
        {
            const string fileName = "/DeathRun_Stats.json";
            
            // First, try the modern BepInEx approach.
            legacyFile = new FileInfo(BepInEx.Paths.PluginPath + "/DeathRun" + fileName);
            if (legacyFile.Exists)
                return true;
            
            // Or try the ancient QMods way.
            string gameDirectory = new FileInfo(BepInEx.Paths.BepInExRootPath).Directory?.Parent?.FullName;
            legacyFile = new FileInfo(gameDirectory + "/QMods/DeathRun" + fileName);
            if (legacyFile.Exists)
                return true;
            
            // Or try to find it in this mod's folder - the user may have dropped it here specifically for this migration.
            legacyFile = new FileInfo(Hootils.GetModDirectory() + fileName);
            if (legacyFile.Exists)
                return true;
            
            // No luck! Reset and leave.
            legacyFile = null;
            return false;
        }

        /// <summary>
        /// Attempt to load a legacy Deathrun stats file from the old mod's folder on disk.
        /// </summary>
        /// <returns>A list of the old run data, or null if nothing was found.</returns>
        public static List<LegacyStats> LoadLegacyStats()
        {
            if (!TryFindLegacyStatsFile(out FileInfo legacyFile))
                return null;

            // TODO
            return null;
        }
    }
}