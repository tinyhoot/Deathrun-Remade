using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DeathrunRemade.Configuration;
using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Enums;
using HootLib;
using HootLib.Objects;
using Nautilus.Handlers;
using Newtonsoft.Json;

namespace DeathrunRemade.Items
{
    internal class VanillaRecipeChanges
    {
        private const string RecipeFileName = "RecipeChanges.json";
        private const string FragmentFileName = "ScanNumberChanges.json";
        private Dictionary<string, List<SerialScanData>> _fragmentJson;
        private Dictionary<string, List<SerialTechData>> _recipeJson;
        private NautilusShell<TechType, ITechData> _recipeCache;
        private NautilusShell<TechType, int> _scanNumberCache;

        public VanillaRecipeChanges()
        {
            // Set up caching so we can reset our changes at any point.
            _recipeCache = new NautilusShell<TechType, ITechData>(CraftDataHandler.SetRecipeData, CraftDataHandler.GetRecipeData);
            _scanNumberCache = new NautilusShell<TechType, int>(PDAHandler.EditFragmentsToScan, GetFragmentScanNumber);

            // Ensure we clean up after ourselves.
            DeathrunInit.OnReset += Reset;
        }

        /// <summary>
        /// Get all recipe changes for a specific value of a setting.
        /// </summary>
        public IEnumerable<SerialTechData> GetCraftData(string configKey, string configValue)
        {
            string key = $"{configKey}.{configValue}";
            return GetCraftData(key);
        }
        
        /// <summary>
        /// Get all recipe changes marked with the specified key.
        /// </summary>
        public IEnumerable<SerialTechData> GetCraftData(string key)
        {
            DeathrunInit._Log.Debug($"Grabbing craft data changes for '{key}'");
            return _recipeJson.TryGetValue(key, out List<SerialTechData> data) ? data : Enumerable.Empty<SerialTechData>();
        }
        
        /// <summary>
        /// Set up all changes related to batteries based on the given config values.
        /// </summary>
        public IEnumerable<SerialTechData> GetBatteryChanges(ConfigSave config)
        {
            if (config.BatteryCosts == Difficulty4.Normal)
                return Enumerable.Empty<SerialTechData>();

            // Remove the now-harder batteries from some early game tools.
            List<SerialTechData> changes = GetCraftData("RemoveBatteries").ToList();
            // Increase regular battery costs.
            changes.AddRange(GetCraftData(nameof(config.BatteryCosts), config.BatteryCosts.ToString()));
            return changes;
        }

        /// <summary>
        /// Get all fragment scan number changes for a specific target difficulty.
        /// </summary>
        public IEnumerable<SerialScanData> GetScanData<TEnum>(TEnum targetDifficulty) where TEnum : Enum
        {
            string key = $"{targetDifficulty}";
            return _fragmentJson.TryGetValue(key, out List<SerialScanData> data) ? data : Enumerable.Empty<SerialScanData>();
        }
        
        /// <summary>
        /// Load the files containing information on recipe changes from the disk.
        /// </summary>
        public async Task LoadFromDiskAsync()
        {
            using StreamReader reader = new StreamReader(Hootils.GetAssetHandle(RecipeFileName));
            string json = await reader.ReadToEndAsync();
            _recipeJson = JsonConvert.DeserializeObject<Dictionary<string, List<SerialTechData>>>(json);

            using StreamReader fragmentReader = new StreamReader(Hootils.GetAssetHandle(FragmentFileName));
            string fragmentJson = await fragmentReader.ReadToEndAsync();
            _fragmentJson = JsonConvert.DeserializeObject<Dictionary<string, List<SerialScanData>>>(fragmentJson);
        }

        /// <summary>
        /// Lock the battery blueprint to turn it into a mid-game item.
        /// </summary>
        public void LockBatteryBlueprint(Difficulty4 difficulty)
        {
            // No changes on normal difficulty.
            if (difficulty == Difficulty4.Normal)
                return;
            
            DeathrunInit._Log.Debug("Locking lithium battery recipe.");
            KnownTechHandler.RemoveDefaultUnlock(TechType.Battery);
            KnownTechHandler.SetAnalysisTechEntry(TechType.Lithium, new [] { TechType.Battery });
        }
        
        /// <summary>
        /// Set up fragment scan changes based on the given config values.
        /// </summary>
        /// <param name="config">The config values to use.</param>
        public void RegisterFragmentChanges(ConfigSave config)
        {
            IEnumerable<SerialScanData> changes = GetScanData(config.ScansRequired);
            foreach (var scanData in changes)
            {
                DeathrunInit._Log.Debug($"Setting fragments required for {scanData.techType}: {scanData.amount}");
                _scanNumberCache.SendChanges(scanData.techType, scanData.amount);
            }
        }

        /// <summary>
        /// Set up recipe changes based on the given config values.
        /// </summary>
        /// <param name="config">The config values to use.</param>
        public void RegisterRecipeChanges(ConfigSave config)
        {
            // The sequence of these changes is set up so that the later ones can overwrite changes made by earlier ones.
            // E.g. NoVehicleChallenge overwrites any cost settings for vehicles.
            List<SerialTechData> changes = GetBatteryChanges(config).ToList();
            changes.AddRange(GetCraftData(nameof(config.ToolCosts), config.ToolCosts.ToString()));
            changes.AddRange(GetCraftData(nameof(config.VehicleCosts), config.VehicleCosts.ToString()));
            changes.AddRange(GetCraftData(nameof(config.NoVehicleChallenge), config.NoVehicleChallenge.ToString()));
            
            foreach (var craftData in changes.Where(techData => techData != null))
            {
                DeathrunInit._Log.Debug($"Setting recipe for {craftData.techType}: {craftData.ingredients.ElementsToString()}");
                _recipeCache.SendChanges(craftData.techType, craftData.ToTechData());
            }
        }

        /// <summary>
        /// Undo all changes committed by this class.
        /// </summary>
        public void Reset()
        {
            DeathrunInit._Log.Debug("Undoing all recipe and fragment changes.");
            _recipeCache.UndoChanges();
            _scanNumberCache.UndoChanges();
            // Restore the regular battery as a starting item.
            KnownTechHandler.UnlockOnStart(TechType.Battery);
        }
        
        /// <summary>
        /// Small wrapper for easier <see cref="NautilusShell{TKey,TValue}"/> setup.
        /// </summary>
        private int GetFragmentScanNumber(TechType techType)
        {
            // At the time of writing, Nautilus only patches fragment edits once on PDAScanner.Initialize(). This is a
            // problem since the game is unable to provide its current data before that point and will just return null.
            // At the same time, delaying the edit past Initialize() and editing the mapping ourselves causes the in-game
            // number of fragments required to not update properly for items visible at start (e.g. seaglide).
            // So, set the value to something nonsensical so that a perfectly timed harmony patch can recognise and
            // update it later.
            return PDAScanner.GetEntryData(techType)?.totalFragments ?? -1;
        }

        /// <summary>
        /// Exists so that a harmony patch can update the values at the appropriate time. It's a mess.
        /// </summary>
        public NautilusShell<TechType, int> GetFragmentScanCache() => _scanNumberCache;
    }
}