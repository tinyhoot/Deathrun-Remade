using DeathrunRemade.Configuration;
using HootLib;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;
using Nautilus.Handlers;
using static CraftData;

namespace DeathrunRemade.Items
{
    internal class DecompressionModule : DeathrunPrefabBase
    {
        public static TechType s_TechType;
        public const TechType UnlockTechType = TechType.Cyclops;

        protected override PrefabInfo CreatePrefabInfo()
        {
            var sprite = SpriteManager.Get(TechType.PowerUpgradeModule);
            PrefabInfo info = Hootils.CreatePrefabInfo(ClassIdPrefix + "decompressionmodule", sprite);
            s_TechType = info.TechType;
            return info;
        }

        protected override CustomPrefab CreatePrefab(PrefabInfo info)
        {
            CustomPrefab prefab = new CustomPrefab(info);
            prefab.SetRecipe(new RecipeData(
                    new Ingredient(TechType.ComputerChip, 1),
                    new Ingredient(TechType.Aerogel, 2),
                    new Ingredient(TechType.Sulphur, 2),
                    new Ingredient(TechType.Lithium, 2),
                    new Ingredient(TechType.Lead, 4)
                ))
                .WithFabricatorType(CraftTree.Type.SeamothUpgrades)
                .WithStepsToFabricatorTab(CraftTreeHandler.Paths.VehicleUpgradesCommonModules);
            prefab.SetPdaGroupCategory(TechGroup.VehicleUpgrades, TechCategory.VehicleUpgrades);
            prefab.SetUnlock(UnlockTechType);
            prefab.SetEquipment(EquipmentType.VehicleModule)
                .WithQuickSlotType(QuickSlotType.Passive);

            var template = new CloneTemplate(info, TechType.VehicleArmorPlating);
            prefab.SetGameObject(template);
            
            return prefab;
        }

        protected override bool ShouldActivateForConfig(ConfigSave config)
        {
            return true;
        }
    }
}