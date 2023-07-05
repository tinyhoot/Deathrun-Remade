using Nautilus.Assets;

namespace DeathrunRemade.Items
{
    public abstract class DeathrunPrefabBase
    {
        protected PrefabInfo _prefabInfo;
        protected CustomPrefab _prefab;

        public virtual string GetClassId() => _prefabInfo.ClassID;

        public virtual CustomPrefab GetPrefab() => _prefab;
        
        public virtual PrefabInfo GetPrefabInfo() => _prefabInfo;

        public virtual TechType GetTechType() => _prefabInfo.TechType;
    }
}