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

        public ConfigSave Config;
        public EscapePodSave EscapePod;
        public NitrogenSave Nitrogen;
        public RunStats Stats;
        public TutorialSave Tutorials;
        public WarningSave Warnings;

        public void Init()
        {
            // Loading a different save repopulates the fields in this class, it does *not* create a new instance.
            // Therefore, register an instanced method for this.
            OnFinishedLoading += OnSaveDataLoaded;
        }

        private void OnSaveDataLoaded(object obj, JsonFileEventArgs args)
        {
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
            
            DeathrunInit._Log.Debug("Save data is ready.");
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