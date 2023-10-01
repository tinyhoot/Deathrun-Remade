using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeathrunRemade.Components;
using DeathrunRemade.Objects;
using HootLib;
using HootLib.Interfaces;
using Newtonsoft.Json;

namespace DeathrunRemade.Handlers
{
    /// <summary>
    /// Responsible for dealing with the logistics of runs and the overall Deathrun save file. Adds new run data to
    /// highscores when the player dies.
    /// </summary>
    internal class RunHandler
    {
        private const string LegacyFileName = "/DeathRun_Stats.json";
        
        public DeathrunStats ModStats;
        public ScoreHandler ScoreHandler;
        private ILogHandler _log;

        public RunHandler(ILogHandler log)
        {
            _log = log;
            ScoreHandler = new ScoreHandler(_log);
            GameEventHandler.OnPlayerDeath += OnDeath;
            GameEventHandler.OnPlayerVictory += OnVictory;
            DeathrunStats.LoadAsync(Hootils.GetModDirectory() + DeathrunStats.FileName)
                .ContinueWith(task => Initialise(task.Result));
        }

        /// <summary>
        /// Set up as soon as we have finished loading existing data from disk.
        /// </summary>
        private void Initialise(DeathrunStats stats)
        {
            ModStats = stats;
            // Only if we've never imported a legacy file before, try to do so.
            if (!ModStats.hasImportedLegacyFile)
            {
                _log.Info("Trying to import legacy Deathrun stats from mod folder...");
                ImportLegacyRuns();
            }
            else
            {
                _log.Info("Skipping legacy run importing, already did so previously.");
            }
        }

        /// <summary>
        /// Add a newly completed run.
        /// </summary>
        public void AddCompletedRun(RunStats run)
        {
            run.version = DeathrunInit.VERSION;
            ScoreHandler.UpdateScore(ref run);
            ModStats.bestRuns.Add(run);
        }

        /// <summary>
        /// Add a completed run and immediately save the changes to disk. Prevents save scumming on death.
        /// </summary>
        /// <param name="run"></param>
        public void AddAndSaveRun(RunStats run)
        {
            AddCompletedRun(run);
            // Save immediately to prevent save scumming.
            _ = ModStats.SaveAsync();
            SaveData.Main.Save();
            // Display the new run.
            DeathrunInit._RunStatsWindow.AddRun(run);
        }

        /// <summary>
        /// Start a new run and set up everything for it to be tracked properly.
        /// </summary>
        public void StartNewRun(SaveData save)
        {
            int id = GetNewRunID();
            _log.Info($"Starting new run with id {id}");
            // The stats themselves do not need to be initialised because they are contained in a struct set up in
            // tandem with the save file. The struct defaults to values recognised as the player having done nothing.
            RunStatsTracker.InitStats(ref save.Stats, save.Config, id);
        }
        
        /// <summary>
        /// Get a unique id for a new run.
        /// </summary>
        private int GetNewRunID()
        {
            // Guard against a race condition which shouldn't really ever occur, but just in case.
            if (ModStats is null)
                return -1;
            return ModStats.totalRuns++;
        }
        
        private void OnDeath(Player player, DamageType damageType)
        {
            SaveData save = SaveData.Main;
            save.Stats.deaths++;
            AddAndSaveRun(save.Stats);

            int deaths = save.Stats.deaths;
            string plural = deaths == 1 ? "" : "s";
            _log.InGameMessage($"Survived for {DeathrunUtils.TimeToGameDays(save.Stats.time):F0} days in {deaths} death{plural}.");
            _log.InGameMessage($"Died to {save.Stats.causeOfDeath}.");
        }

        private void OnVictory(Player player)
        {
            SaveData save = SaveData.Main;
            save.Stats.causeOfDeath = "Victory";
            save.Stats.victory = true;
            AddAndSaveRun(save.Stats);
            
            int deaths = save.Stats.deaths;
            string plural = deaths == 1 ? "" : "s";
            string message = deaths > 0 ? $"Victory in {deaths} death{plural} and" : "Flawless victory in";
            _log.InGameMessage($"{message} {DeathrunUtils.TimeToGameDays(save.Stats.time):F0} days!");
        }

        private void ImportLegacyRuns()
        {
            var legacyStats = TryLoadLegacyStats();
            if (legacyStats?.Count > 0)
            {
                foreach (LegacyStats legacy in TryLoadLegacyStats() ?? Enumerable.Empty<LegacyStats>())
                {
                    var modern = legacy.ToModernStats();
                    AddCompletedRun(modern);
                }
                ModStats.hasImportedLegacyFile = true;
                _ = ModStats.SaveAsync();
            }
        }
        
        /// <summary>
        /// Try to find a legacy Deathrun stats file in a few likely locations.
        /// </summary>
        /// <returns>True if a file was found, false if not.</returns>
        private bool TryFindLegacyStatsFile(out FileInfo legacyFile)
        {
            // First, try the modern BepInEx approach.
            legacyFile = new FileInfo(BepInEx.Paths.PluginPath + "/DeathRun" + LegacyFileName);
            if (legacyFile.Exists)
                return true;
            
            // Or try the ancient QMods way.
            string gameDirectory = new FileInfo(BepInEx.Paths.BepInExRootPath).Directory?.Parent?.FullName;
            legacyFile = new FileInfo(gameDirectory + "/QMods/DeathRun" + LegacyFileName);
            if (legacyFile.Exists)
                return true;
            
            // Or try to find it in this mod's folder - the user may have dropped it here specifically for this migration.
            legacyFile = new FileInfo(Hootils.GetModDirectory() + LegacyFileName);
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
        public List<LegacyStats> TryLoadLegacyStats()
        {
            if (!TryFindLegacyStatsFile(out FileInfo legacyFile))
                return null;

            using StreamReader reader = new StreamReader(legacyFile.FullName);
            string json = reader.ReadToEnd();
            var statsFile = JsonConvert.DeserializeObject<LegacyStatsFile>(json, DeathrunStats.GetSerializerSettings());
            return statsFile.HighScores;
        }
    }
}