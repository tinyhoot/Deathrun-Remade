using Nautilus.Assets;

namespace DeathrunRemade.Items
{
    internal abstract class DeathrunPrefabBase
    {
        public const string ClassIdPrefix = "deathrunremade_";
        
        protected PrefabInfo _prefabInfo;
        protected CustomPrefab _prefab;

        public virtual string ClassId => _prefabInfo.ClassID;

        public virtual CustomPrefab Prefab => _prefab;
        
        public virtual PrefabInfo PrefabInfo => _prefabInfo;

        // Ideally this would be static abstract but C#11 is unsupported on Subnautica's .NET Framework.
        public virtual TechType TechType => _prefabInfo.TechType;

        public void RegisterTechType()
        {
            _prefabInfo = CreatePrefabInfo();
        }

        public void SetupPrefab()
        {
            _prefab = CreatePrefab(_prefabInfo);
        }

        protected virtual PrefabInfo CreatePrefabInfo()
        {
            return default;
        }

        protected virtual CustomPrefab CreatePrefab(PrefabInfo info)
        {
            return default;
        }

        public virtual void Register()
        {
            _prefab.Register();
        }

        public virtual void Unregister()
        {
            _prefab.Unregister();
        }
    }
}