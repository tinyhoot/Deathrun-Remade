using DeathrunRemade.Components;
using DeathrunRemade.Configuration;
using DeathrunRemade.Handlers;
using HootLib;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;

namespace DeathrunRemade.Items
{
    /// <summary>
    /// An overall class for representing all special oxygen tanks.
    /// </summary>
    internal abstract class TankBase : DeathrunPrefabBase
    {
        public const string WorkbenchTankTab = ClassIdPrefix + "specialtanks";
        
        protected override PrefabInfo CreatePrefabInfo()
        {
            PrefabInfo info = Hootils.CreatePrefabInfo(GetClassId(), GetSprite());
            info.WithSizeInInventory(new Vector2int(2, 3));
            AssignTechType(info);
            return info;
        }

        protected override CustomPrefab CreatePrefab(PrefabInfo info)
        {
            CustomPrefab prefab = new CustomPrefab(info);
            prefab.SetRecipe(GetRecipe())
                .WithFabricatorType(CraftTree.Type.Workbench)
                .WithStepsToFabricatorTab(WorkbenchTankTab);
            prefab.SetPdaGroupCategory(TechGroup.Workbench, TechCategory.Workbench);
            prefab.SetEquipment(EquipmentType.Tank);
            prefab.SetUnlock(GetUnlock());

            var template = new CloneTemplate(info, GetCloneType());
            prefab.SetGameObject(template);

            return prefab;
        }
        
        protected override bool ShouldActivateForConfig(ConfigSave config)
        {
            return config.SpecialAirTanks;
        }

        protected override void Register()
        {
            base.Register();
            GameEventHandler.OnPlayerAwake += EnsureSpecialTankComponent;
        }

        public override void Unregister()
        {
            base.Unregister();
            GameEventHandler.OnPlayerAwake -= EnsureSpecialTankComponent;
        }

        protected abstract void AssignTechType(PrefabInfo info);

        /// <summary>
        /// Get the class id for the type of suit.
        /// </summary>
        protected abstract string GetClassId();

        protected abstract TechType GetCloneType();

        /// <summary>
        /// Gets the right recipe for the type of suit upgrade.
        /// </summary>
        protected abstract RecipeData GetRecipe();

        /// <summary>
        /// Gets the right sprite for the suit upgrade.
        /// </summary>
        protected abstract Atlas.Sprite GetSprite();

        /// <summary>
        /// Gets the right unlocking item for the type of suit.
        /// </summary>
        protected abstract TechType GetUnlock();

        private void EnsureSpecialTankComponent(Player player)
        {
            player.gameObject.EnsureComponent<DeathrunTank>();
        }
    }
}