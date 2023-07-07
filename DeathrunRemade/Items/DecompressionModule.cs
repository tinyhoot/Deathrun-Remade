using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;
using Nautilus.Handlers;
using HootLib;
using static CraftData;

namespace DeathrunRemade.Items
{
    internal class DecompressionModule : DeathrunPrefabBase
    {
        public DecompressionModule()
        {
            var sprite = Hootils.GetSprite(TechType.PowerUpgradeModule);
            _prefabInfo = Hootils.CreatePrefabInfo(
                ItemInfo.GetIdForItem(nameof(DecompressionModule)),
                "Nano Decompression Module",
                "Eliminates nitrogen from the bloodstream of vehicle pilot. Reduces energy expended when "
                + "exiting the vehicle. Stacking multiple modules increases the benefit.",
                sprite
            );

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
            _prefab.SetUnlock(TechType.Cyclops);
            _prefab.SetEquipment(EquipmentType.VehicleModule)
                .WithQuickSlotType(QuickSlotType.Passive);

            var template = new CloneTemplate(_prefabInfo, TechType.VehicleArmorPlating);
            _prefab.SetGameObject(template);
            _prefab.Register();
        }
    }
}