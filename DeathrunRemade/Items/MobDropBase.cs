using DeathrunRemade.Configuration;
using HootLib;
using Nautilus.Assets;
using Nautilus.Assets.PrefabTemplates;

namespace DeathrunRemade.Items
{
    /// <summary>
    /// Represents all items that can be acquired by knifing specific fish.
    /// </summary>
    internal abstract class MobDropBase : DeathrunPrefabBase
    {
        protected override PrefabInfo CreatePrefabInfo()
        {
            PrefabInfo info = Hootils.CreatePrefabInfo(GetClassId(), GetSprite());
            AssignTechType(info);
            return info;
        }

        protected override CustomPrefab CreatePrefab(PrefabInfo info)
        {
            CustomPrefab prefab = new CustomPrefab(info);
            // prefab.SetPdaGroupCategory(TechGroup.Resources, TechCategory.BasicMaterials);

            var template = new CloneTemplate(info, TechType.StalkerTooth);
            prefab.SetGameObject(template);

            return prefab;
        }

        protected override void Register()
        {
            base.Register();
            RegisterHarvestData();
        }

        public override void Unregister()
        {
            base.Unregister();
            UnregisterHarvestData();
        }

        protected abstract void AssignTechType(PrefabInfo info);
        
        protected abstract string GetClassId();
        
        protected abstract Atlas.Sprite GetSprite();
        
        protected abstract void RegisterHarvestData();

        protected abstract void UnregisterHarvestData();
    }
}