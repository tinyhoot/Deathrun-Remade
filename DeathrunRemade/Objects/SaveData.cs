using System;
using System.Collections.Generic;
using DeathrunRemade.Configuration;
using Nautilus.Json;

namespace DeathrunRemade.Objects
{
    /// <summary>
    /// The cache that keeps track of everything happening in an ongoing game. It enables the mod to load properly
    /// after saving and quitting.
    /// </summary>
    internal class SaveData : SaveDataCache
    {
        [NonSerialized]
        public static SaveData Main;
        [NonSerialized]
        public bool Ready;
        
        /// <summary>
        /// Invoked when the save cache has finished initialising and/or loading data from disk.
        /// </summary>
        public static event Action<SaveData> OnSaveDataLoaded;

        public ConfigSave Config;
        public EscapePodSave EscapePod;
        public NitrogenSave Nitrogen;
        public RunStats Stats;
        public TutorialSave Tutorials;
        public WarningSave Warnings;
        
        public override void Load(bool createFileIfNotExist = true)
        {
            base.Load(createFileIfNotExist);
            // The save data loads on game start. If the config data has not been set already, lock it in.
            if (!Config.WasInitialised)
            {
                Config = new ConfigSave(DeathrunInit._Config);
                DeathrunInit._RunHandler.StartNewRun(this);
            }
            else
            {
                DeathrunInit._Log.Info($"Loading existing run with id {Stats.id}");
            }

            DeathrunInit.OnReset += OnReset;
            // Once the file has completed loading/creation, notify everything waiting on it.
            Ready = true;
            DeathrunInit._Log.Debug("Save data is ready.");
            OnSaveDataLoaded?.Invoke(this);
        }

        /// <summary>
        /// Work around an issue in Nautilus/Newtonsoft(?) where the JSON fails to populate fields that already have
        /// values in them. By resetting these we ensure the save games of successive games are loaded properly.
        /// </summary>
        public void OnReset()
        {
            DeathrunInit._Log.Debug("Purging save data from memory.");
            Config = default;
            EscapePod = default;
            Nitrogen = default;
            Stats = default;
            Tutorials = default;
            Warnings = default;
            DeathrunInit.OnReset -= OnReset;
        }
    }

    [Serializable]
    internal struct EscapePodSave
    {
        public bool isAnchored;
        public bool isToppled;
    }

    [Serializable]
    internal struct NitrogenSave
    {
        public float nitrogen;
        public float safeDepth;
    }

    [Serializable]
    internal struct TutorialSave
    {
        public HashSet<string> completedTutorials;
    }

    [Serializable]
    internal class WarningSave
    {
        // These do get assigned and used but by reflection rather than directly.
#pragma warning disable CS0649
        public double lastAscentWarningTime;
        public double lastBreathWarningTime;
        public double lastCrushDepthWarningTime;
        public double lastCrushDamageWarningTime;
        public double lastDecompressionWarningTime;
        public double lastDecoDamageWarningTime;
#pragma warning restore CS0649
    }
}