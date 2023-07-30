using System;
using Nautilus.Json;

namespace DeathrunRemade.Objects
{
    internal class SaveData : SaveDataCache
    {
        [NonSerialized]
        public static SaveData Main;
        [NonSerialized]
        public bool Ready;
        
        public NitrogenSave Nitrogen;
        public WarningSave Warnings;
    }

    [Serializable]
    internal struct NitrogenSave
    {
        public float nitrogen;
        public float safeDepth;
    }

    [Serializable]
    internal struct WarningSave
    {
        public float lastAscentWarningTime;
        public float lastBreathWarningTime;
        public float lastDecompressionWarningTime;
        public float lastDecoDamageWarningTime;
    }
}