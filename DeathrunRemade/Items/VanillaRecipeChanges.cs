using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DeathrunRemade.Configuration;
using DeathrunRemade.Objects;
using HootLib;
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

        /// <summary>
        /// Get all recipe changes for a specific target difficulty.
        /// </summary>
        public List<SerialTechData> GetCraftData<TEnum>(TEnum targetDifficulty) where TEnum : Enum
        {
            string key = $"{targetDifficulty.GetType().Name}.{targetDifficulty}";
            return _recipeJson.TryGetValue(key, out List<SerialTechData> data) ? data : null;
        }
        
        /// <summary>
        /// Get all fragment scan number changes for a specific target difficulty.
        /// </summary>
        public List<SerialScanData> GetScanData<TEnum>(TEnum targetDifficulty) where TEnum : Enum
        {
            string key = $"{targetDifficulty}";
            return _fragmentJson.TryGetValue(key, out List<SerialScanData> data) ? data : null;
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
        /// Set up fragment scan changes based on the given config values.
        /// </summary>
        /// <param name="config">The config values to use.</param>
        public void RegisterFragmentChanges(ConfigSave config)
        {
            List<SerialScanData> changes = GetScanData(config.ScansRequired);
            foreach (var scanData in changes)
            {
                // We lose the race condition against Nautilus' PDAHandler, which only patches once on PDAScanner init.
                // Therefore, we need to patch things ourselves. This comes with an unfortunate side effect: Items
                // which are not unlocked but have their number of scans visible from the start (seaglide, constructor)
                // will appear to require their vanilla number of scans until the first fragment is scanned, at which
                // point it will update to the proper value.
                // I tried to get the label to update properly, but it just won't. Luckily this is not too bad.
                
                // PDAHandler.EditFragmentsToScan(scanData.techType, scanData.amount);
                if (PDAScanner.mapping.TryGetValue(scanData.techType, out PDAScanner.EntryData data))
                    data.totalFragments = scanData.amount;
                else
                    DeathrunInit._Log.Warn($"Failed to patch number of fragments for {scanData.techType}!");
            }
        }

        /// <summary>
        /// Set up recipe changes based on the given config values.
        /// </summary>
        /// <param name="config">The config values to use.</param>
        public void RegisterRecipeChanges(ConfigSave config)
        {
            List<SerialTechData> changes = GetCraftData(config.ToolCosts);
            changes.AddRange(GetCraftData(config.VehicleCosts));
            foreach (var craftData in changes)
            {
                CraftDataHandler.SetRecipeData(craftData.techType, craftData.ToTechData());
            }
        }
    }
}