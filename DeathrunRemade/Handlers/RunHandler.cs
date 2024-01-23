using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeathrunRemade.Components;
using DeathrunRemade.Objects;
using HootLib;
using Newtonsoft.Json;
using UnityEngine;
using ILogHandler = HootLib.Interfaces.ILogHandler;

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
        private string _deathCauseOverride;

        public RunHandler(ILogHandler log)
        {
            _log = log;
            ScoreHandler = new ScoreHandler(_log);
            GameEventHandler.OnPlayerDeath += OnDeath;
            GameEventHandler.OnPlayerVictory += OnVictory;
            DeathrunStats.LoadAsync().ContinueWith(task => Initialise(task.Result));
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
        }

        /// <inheritdoc cref="DeleteRun(int, float)"/>
        public void DeleteRun(RunStats run)
        {
            DeleteRun(run.id, run.scoreTotal);
        }

        /// <summary>
        /// Delete a run from the saved statistics.
        /// </summary>
        public void DeleteRun(int id, float scoreTotal)
        {
            ModStats.bestRuns = ModStats.bestRuns
                .Where(stats => stats.id != id && !Mathf.Approximately(stats.scoreTotal, scoreTotal))
                .ToList();
            _ = ModStats.SaveAsync();
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
        /// Figure out the cause of death from the type of damage that killed the player or an override set by a
        /// helper patch to capture cases the damage system does not cover.
        /// This works by some damage types <em>always</em> having a cause of death attached, so that causes are either
        /// never outdated or not applicable.
        /// </summary>
        private string GetCauseOfDeath(DamageType damageType)
        {
            if (!string.IsNullOrEmpty(_deathCauseOverride)
                && (damageType == DamageType.Normal || damageType == DamageType.Starve))
                return _deathCauseOverride;

            string cause = damageType switch
            {
                DamageType.Collide => "Heavy Impact",
                DamageType.Electrical => "Electrocution",
                DamageType.Explosive => "Explosion",
                DamageType.Heat => "Unbearable Heat",
                _ => damageType.ToString()
            };

            return cause;
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
            save.Stats.causeOfDeath = GetCauseOfDeath(damageType);
            AddAndSaveRun(save.Stats);

            // Get the language lines and let the game insert stats variables for us.
            NotificationHandler.VanillaMessage("dr_run_death1",
                Mathf.Floor(DeathrunUtils.TimeToGameDays(save.Stats.time)), save.Stats.deaths);
            NotificationHandler.VanillaMessage("dr_run_death2", save.Stats.causeOfDeath);
        }

        private void OnVictory(Player player)
        {
            SaveData save = SaveData.Main;
            save.Stats.causeOfDeath = "Victory";
            save.Stats.victory = true;
            AddAndSaveRun(save.Stats);
            
            int deaths = save.Stats.deaths;
            string key = deaths > 0 ? "dr_run_victory" : "dr_run_deathlessVictory";
            NotificationHandler.VanillaMessage(key, Mathf.Floor(DeathrunUtils.TimeToGameDays(save.Stats.time)), deaths);
        }
        
        /// <summary>
        /// Set an override for the cause of death for when the damage type alone just isn't enough.
        /// </summary>
        public void SetCauseOfDeathOverride(string cause)
        {
            _deathCauseOverride = cause;
            DeathrunInit._Log.Debug($"Setting death cause override: {_deathCauseOverride}");
        }

        /// <summary>
        /// Set an override for the cause of death for when the damage type alone just isn't enough.
        /// </summary>
        /// <param name="techType">The <see cref="TechType"/> of the thing or attacker causing the death.</param>
        public void SetCauseOfDeathOverride(TechType techType)
        {
            _deathCauseOverride = techType == TechType.None ? "Unknown Creature" : Language.main.Get(techType.AsString());
            DeathrunInit._Log.Debug($"Setting death cause override: {_deathCauseOverride}");
        }

        /// <summary>
        /// Set an override for the cause of death for when the damage type alone just isn't enough.
        /// </summary>
        /// <param name="gameObject">The attacker causing the death.</param>
        public void SetCauseOfDeathOverride(GameObject gameObject)
        {
            TechType techType = CraftData.GetTechType(gameObject);
            SetCauseOfDeathOverride(techType);
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