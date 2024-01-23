using Nautilus.Assets;

namespace DeathrunRemade.Items
{
    internal abstract class DeathrunPrefabBase
    {
        // Do not assign a prefix in debug builds, which makes the items easier to spawn in and test but more at risk
        // of clashes with other mods.
#if DEBUG2
        public const string ClassIdPrefix = "";
#else
        public const string ClassIdPrefix = "deathrunremade_";
#endif
        
        protected PrefabInfo _prefabInfo;
        protected CustomPrefab _prefab;

        public virtual string ClassId => _prefabInfo.ClassID;

        public virtual CustomPrefab Prefab => _prefab;
        
        public virtual PrefabInfo PrefabInfo => _prefabInfo;

        public virtual TechType TechType => _prefabInfo.TechType;

        public virtual void Register()
        {
            _prefab.Register();
        }
    }
}