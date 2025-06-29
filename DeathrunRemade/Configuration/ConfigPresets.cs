using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HootLib;
using HootLib.Configuration;
using Nautilus.Options;

namespace DeathrunRemade.Configuration
{
    internal static class ConfigPresets
    {
        private const string PresetsFolder = "Presets";
        public static readonly string CustomPresetId = "Custom";
        private static Dictionary<string, string> _presets = new();
        

        public static void LoadPresetFiles()
        {
            _presets.Add(CustomPresetId, null);
            
            string presetsPath = Path.Combine(Hootils.GetModDirectory(), "Assets", PresetsFolder);
            // If for some reason the user deleted the presets directory, respect that.
            if (!Directory.Exists(presetsPath))
            {
                DeathrunInit._Log.Info("No presets folder found - proceeding without config presets.");
                return;
            }

            foreach (var file in Directory.EnumerateFiles(presetsPath))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                // Skip vortex management files.
                if (fileName.StartsWith("__"))
                    continue;
                
                using var reader = new StreamReader(File.OpenRead(file));
                _presets.Add(fileName, reader.ReadToEnd());
            }
            
            DeathrunInit._Log.Info($"Loaded {_presets.Count - 1} config presets from disk.");
        }

        public static string[] GetPresetNames()
        {
            return _presets.Keys.ToArray();
        }

        public static ModChoiceOption<string> CreatePresetButton(ConfigEntryWrapper<string> entry, Action<string> callback)
        {
            var option = entry.ToModChoiceOption();
            option.OnChanged += (_, args) => OnPresetChanged(args, callback);
            return option;
        }

        private static void OnPresetChanged(ChoiceChangedEventArgs<string> args, Action<string> callback)
        {
            // The preset named "custom" is not an actual preset, but rather represents that values have been changed
            // beyond the preset by the player.
            if (args.Value == CustomPresetId)
                return;
            
            if (_presets.TryGetValue(args.Value, out string json))
            {
                callback(json);
            }
            else
            {
                DeathrunInit._Log.Warn($"User selected preset '{args.Value}', but there are no settings to switch to.");
            }
        }
    }
}