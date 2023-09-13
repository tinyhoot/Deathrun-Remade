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
        private const string FileName = "RecipeChanges.json";
        private Dictionary<string, List<SerialTechData>> _json;

        /// <summary>
        /// Get all recipe changes for a specific target difficulty.
        /// </summary>
        public List<SerialTechData> GetCraftData<TEnum>(TEnum targetDifficulty) where TEnum : Enum
        {
            string key = $"{targetDifficulty.GetType().Name}.{targetDifficulty}";
            return _json.TryGetValue(key, out List<SerialTechData> data) ? data : null;
        }
        
        /// <summary>
        /// Load the file containing information on recipe changes from the disk.
        /// </summary>
        public async Task LoadFromDiskAsync()
        {
            using StreamReader reader = new StreamReader(Hootils.GetAssetHandle(FileName));
            string json = await reader.ReadToEndAsync();
            _json = JsonConvert.DeserializeObject<Dictionary<string, List<SerialTechData>>>(json);
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