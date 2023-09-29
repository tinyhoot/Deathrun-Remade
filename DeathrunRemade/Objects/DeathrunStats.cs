using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HootLib;
using Newtonsoft.Json;

namespace DeathrunRemade.Objects
{
    /// <summary>
    /// Represents stats across all runs, across the entire time this mod has been installed.
    /// </summary>
    [Serializable]
    internal class DeathrunStats
    {
        public const string FileName = "DeathrunStats.sav";
        
        public List<RunStats> bestRuns = new List<RunStats>();
        public bool hasImportedLegacyFile;
        public int totalRuns;
        public string version;

        /// <summary>
        /// Putting this into a function just to ensure it's the same for both saving and loading.
        /// </summary>
        public static JsonSerializerSettings GetSerializerSettings()
        {
            return new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore
            };
        }

        /// <summary>
        /// Deserialise run data from disk.
        /// </summary>
        /// <param name="fileName">The file path of the save file.</param>
        public static async Task<DeathrunStats> LoadAsync(string fileName = FileName)
        {
            fileName = Path.Combine(Hootils.GetModDirectory(), fileName);
            // If no file exists, this might be a first load. Get a fresh instance ready to go.
            if (!File.Exists(fileName))
            {
                DeathrunInit._Log.Info("No saved run stats found. Creating new file.");
                return new DeathrunStats();
            }
            
            var settings = GetSerializerSettings();
            try
            {
                StreamReader reader = new StreamReader(fileName);
                string json = await reader.ReadToEndAsync();
                return JsonConvert.DeserializeObject<DeathrunStats>(json, settings);
            }
            catch (Exception ex)
            {
                DeathrunInit._Log.InGameMessage("Failed to load run data from disk!", true);
                DeathrunInit._Log.Error($"{ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }
        
        /// <summary>
        /// Serialise this object to disk.
        /// </summary>
        /// <param name="fileName">The file path to save the file at.</param>
        public async Task SaveAsync(string fileName = FileName)
        {
            fileName = Path.Combine(Hootils.GetModDirectory(), fileName);
            version = DeathrunInit.VERSION;
            var settings = GetSerializerSettings();
            try
            {
                string json = JsonConvert.SerializeObject(this, settings);
                using StreamWriter writer = new StreamWriter(fileName);
                await writer.WriteAsync(json);
            }
            catch (Exception ex)
            {
                DeathrunInit._Log.InGameMessage("Failed to save run data to disk!", true);
                DeathrunInit._Log.Error($"{ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}