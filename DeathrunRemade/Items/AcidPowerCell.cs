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
    internal class AcidPowerCell : DeathrunPrefabBase
    {
        public new static TechType TechType;
        
        public AcidPowerCell(Difficulty4 difficulty)
        {
            var sprite = Hootils.LoadSprite("AcidPowerCell.png", true);
            _prefabInfo = Hootils.CreatePrefabInfo(
                ClassIdPrefix + "acidpowercell",
                "Lead Acid Power Cell",
                "A basic lead/acid vehicle power source - not super powerful, but it IS rechargeable. "
                + "Keep fully charged during winter months!",
                sprite
            );
            TechType = _prefabInfo.TechType;

            _prefab = new CustomPrefab(_prefabInfo);
            _prefab.SetRecipe(new RecipeData(
                    new Ingredient(TechType.Lead, 2),
                    new Ingredient(TechType.AcidMushroom, 4),
                    new Ingredient(TechType.Silicone, 1)
                ))
                .WithFabricatorType(CraftTree.Type.Fabricator)
                .WithStepsToFabricatorTab(CraftTreeHandler.Paths.FabricatorsElectronics);
            _prefab.SetPdaGroupCategory(TechGroup.Resources, TechCategory.Electronics);
            _prefab.SetUnlock(TechType.AcidMushroom);
            _prefab.SetEquipment(EquipmentType.PowerCellCharger);

            var template = new EnergySourceTemplate(_prefabInfo, GetCapacityForDifficulty(difficulty));
            _prefab.SetGameObject(template);
        }

        public int GetCapacityForDifficulty(Difficulty4 difficulty)
        {
            return difficulty switch
            {
                Difficulty4.Normal => 200,
                Difficulty4.Hard => 150,
                Difficulty4.Deathrun => 125,
                Difficulty4.Kharaa => 75,
                _ => 200
            };
        }
    }
}