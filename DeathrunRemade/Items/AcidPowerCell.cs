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
    internal class AcidPowerCell : DeathrunPrefabBase
    {
        public AcidPowerCell(Difficulty difficulty)
        {
            var sprite = Hootils.LoadSprite("AcidPowerCell.png", true);
            _prefabInfo = Hootils.CreatePrefabInfo(
                ItemInfo.GetIdForItem(nameof(AcidPowerCell)),
                "Lead Acid Power Cell",
                "A basic lead/acid vehicle power source - not super powerful, but it IS rechargeable. "
                + "Keep fully charged during winter months!",
                sprite
            );

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
            _prefab.Register();
        }

        public int GetCapacityForDifficulty(Difficulty difficulty)
        {
            return difficulty switch
            {
                Difficulty.Normal => 200,
                Difficulty.Hard => 150,
                Difficulty.Deathrun => 125,
                Difficulty.Kharaa => 75,
                _ => 200
            };
        }
    }
}