using DeathrunRemade.Objects.Enums;
using HootLib;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;
using Nautilus.Handlers;
using static CraftData;

namespace DeathrunRemade.Items
{
    internal class AcidBattery : DeathrunPrefabBase
    {
        public new static TechType TechType;
        
        public AcidBattery(Difficulty4 difficulty)
        {
            var sprite = Hootils.LoadSprite("AcidBattery.png", true);
            _prefabInfo = Hootils.CreatePrefabInfo(
                Constants.ClassIdPrefix + "acidbattery",
                "Copper/Zinc Battery",
                "A very basic mobile power source, and NOT rechargeable. Please dispose of safely.",
                sprite
            );
            TechType = _prefabInfo.TechType;

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
        }

        public static int GetCapacityForDifficulty(Difficulty4 difficulty)
        {
            return difficulty switch
            {
                Difficulty4.Normal => 100,
                Difficulty4.Hard => 75,
                Difficulty4.Deathrun => 50,
                Difficulty4.Kharaa => 25,
                _ => 100
            };
        }
    }
}