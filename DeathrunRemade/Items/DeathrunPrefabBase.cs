using Nautilus.Assets;

namespace DeathrunRemade.Items
{
    public abstract class DeathrunPrefabBase
    {
        protected PrefabInfo _prefabInfo;
        protected CustomPrefab _prefab;

        public virtual string ClassId => _prefabInfo.ClassID;

        public virtual CustomPrefab Prefab => _prefab;
        
        public virtual PrefabInfo PrefabInfo => _prefabInfo;

        public virtual TechType TechType => _prefabInfo.TechType;
    }
}