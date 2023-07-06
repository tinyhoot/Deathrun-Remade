using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;
using Nautilus.Handlers;
using SubnauticaCommons;

namespace DeathrunRemade.Items
{
    internal class FilterChip : DeathrunPrefabBase
    {
        public FilterChip()
        {
            var sprite = Hootils.GetSprite(TechType.ComputerChip);
            _prefabInfo = Hootils.CreatePrefabInfo(
                ItemInfo.GetIdForItem(nameof(FilterChip)),
                "Integrated Air Filter",
                "Makes surface air breathable and purges nitrogen from the bloodstream while indoors. "
                + "Comes with an integrated Compass.",
                sprite
            );

            _prefab = new CustomPrefab(_prefabInfo);
            _prefab.SetRecipe(new RecipeData(
                    new CraftData.Ingredient(TechType.Compass, 1),
                    new CraftData.Ingredient(TechType.ComputerChip, 1),
                    new CraftData.Ingredient(TechType.Polyaniline, 1),
                    new CraftData.Ingredient(TechType.AramidFibers, 1)
                ))
                .WithFabricatorType(CraftTree.Type.Fabricator)
                .WithStepsToFabricatorTab(CraftTreeHandler.Paths.FabricatorEquipment);
            _prefab.SetPdaGroupCategory(TechGroup.Personal, TechCategory.Equipment);
            _prefab.SetUnlock(TechType.Cyclops);
            _prefab.SetEquipment(EquipmentType.Chip)
                .WithQuickSlotType(QuickSlotType.None);

            var template = new CloneTemplate(_prefabInfo, TechType.Compass);
            _prefab.SetGameObject(template);
            _prefab.Register();
        }
    }
}