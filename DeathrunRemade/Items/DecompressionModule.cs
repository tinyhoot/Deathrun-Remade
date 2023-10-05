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
        public new static TechType TechType;
        public const TechType UnlockTechType = TechType.Cyclops;

        public DecompressionModule()
        {
            var sprite = SpriteManager.Get(TechType.PowerUpgradeModule);
            _prefabInfo = Hootils.CreatePrefabInfo(
                Constants.ClassIdPrefix + "decompressionmodule",
                "Nano Decompression Module",
                "Eliminates nitrogen from the bloodstream of vehicle pilot. Reduces energy expended when "
                + "exiting the vehicle. Stacking multiple modules increases the benefit.",
                sprite
            );
            TechType = _prefabInfo.TechType;

            _prefab = new CustomPrefab(_prefabInfo);
            _prefab.SetRecipe(new RecipeData(
                    new Ingredient(TechType.ComputerChip, 1),
                    new Ingredient(TechType.Aerogel, 2),
                    new Ingredient(TechType.Sulphur, 2),
                    new Ingredient(TechType.Lithium, 2),
                    new Ingredient(TechType.Lead, 4)
                ))
                .WithFabricatorType(CraftTree.Type.SeamothUpgrades)
                .WithStepsToFabricatorTab(CraftTreeHandler.Paths.VehicleUpgradesCommonModules);
            _prefab.SetPdaGroupCategory(TechGroup.VehicleUpgrades, TechCategory.VehicleUpgrades);
            _prefab.SetUnlock(UnlockTechType);
            _prefab.SetEquipment(EquipmentType.VehicleModule)
                .WithQuickSlotType(QuickSlotType.Passive);

            var template = new CloneTemplate(_prefabInfo, TechType.VehicleArmorPlating);
            _prefab.SetGameObject(template);
        }
    }
}