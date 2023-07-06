using DeathrunRemade.Objects.Enums;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;
using Nautilus.Handlers;
using SubnauticaCommons;
using static CraftData;

namespace DeathrunRemade.Items
{
    internal class AcidBattery : DeathrunPrefabBase
    {
        public AcidBattery(Difficulty difficulty)
        {
            var sprite = Hootils.LoadSprite("AcidBattery.png", true);
            _prefabInfo = Hootils.CreatePrefabInfo(
                ItemInfo.GetIdForItem(nameof(AcidBattery)),
                "Copper/Zinc Battery",
                "A very basic mobile power source, and NOT rechargeable. Please dispose of safely.",
                sprite
            );

            _prefab = new CustomPrefab(_prefabInfo);
            _prefab.SetRecipe(new RecipeData(
                    new Ingredient(TechType.Copper, 1),
                    new Ingredient(TechType.AcidMushroom, 2)
                ))
                .WithFabricatorType(CraftTree.Type.Fabricator)
                .WithStepsToFabricatorTab(CraftTreeHandler.Paths.FabricatorsElectronics);
            _prefab.SetPdaGroupCategory(TechGroup.Resources, TechCategory.Electronics);
            _prefab.SetUnlock(TechType.AcidMushroom);
            // Prefab.SetEquipment(EquipmentType.BatteryCharger);

            var template = new EnergySourceTemplate(_prefabInfo, GetCapacityForDifficulty(difficulty));
            _prefab.SetGameObject(template);
            _prefab.Register();
        }

        public int GetCapacityForDifficulty(Difficulty difficulty)
        {
            return difficulty switch
            {
                Difficulty.Normal => 100,
                Difficulty.Hard => 75,
                Difficulty.Deathrun => 50,
                Difficulty.Kharaa => 25,
                _ => 100
            };
        }
    }
}