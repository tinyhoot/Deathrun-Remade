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
    }

    [Serializable]
    internal struct NitrogenSave
    {
        public float nitrogen;
        public float safeDepth;
    }
}