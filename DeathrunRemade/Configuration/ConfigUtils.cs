using System.IO;
using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Enums;
using UnityEngine;
using ILogHandler = HootLib.Interfaces.ILogHandler;

namespace DeathrunRemade.Configuration
{
    /// <summary>
    /// Make sense of the myriad of config options through easily consumable functions.
    /// </summary>
    internal static class ConfigUtils
    {
        private static ConfigSave _config => SaveData.Main.Config;
        private static ILogHandler _log => DeathrunInit._Log;

        /// <summary>
        /// Check whether the given warning should be shown.
        /// </summary>
        /// <param name="warning">The type of warning.</param>
        /// <param name="intervalSeconds">The interval between warnings, in seconds.</param>
        public static bool ShouldShowWarning(Warning warning, float intervalSeconds)
        {
            Hints setting = _config.ShowWarnings;
            if (setting == Hints.Never)
                return false;

            // Don't show anything until the save has finished loading.
            if (SaveData.Main is null)
                return false;
            
            WarningSave save = SaveData.Main.Warnings;
            float lastShown = warning switch
            {
                Warning.AscentSpeed => save.lastAscentWarningTime,
                Warning.CrushDepth => save.lastCrushDepthWarningTime,
                Warning.Decompression => save.lastDecompressionWarningTime,
                Warning.DecompressionDamage => save.lastDecoDamageWarningTime,
                Warning.UnbreathableAir => save.lastBreathWarningTime,
                _ => throw new InvalidDataException()
            };

            // Always space out warnings by at least this much.
            const float delay = 3f;
            float delta = Time.time - lastShown;
            switch (setting)
            {
                case Hints.Introductory:
                    return lastShown == 0;
                case Hints.Occasional:
                    return delta > intervalSeconds && delta > delay;
                case Hints.Always:
                    return delta > delay;
            }
            
            // Things shouldn't be able to get down here, but if it happens, uh, scream?
            _log.Warn($"Unexpected outcome in {typeof(ConfigUtils).FullName}.{nameof(ShouldShowWarning)}: "
                      + $"Did not return proper value!");
            return false;
        }
    }
}