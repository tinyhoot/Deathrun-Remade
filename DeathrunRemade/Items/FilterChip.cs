using DeathrunRemade.Configuration;
using HootLib;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;
using Nautilus.Handlers;

namespace DeathrunRemade.Items
{
    internal class FilterChip : DeathrunPrefabBase
    {
        public static TechType s_TechType;
        public new const string ClassId = ClassIdPrefix + "filterchip";

        protected override PrefabInfo CreatePrefabInfo()
        {
            var sprite = SpriteManager.Get(TechType.ComputerChip);
            PrefabInfo info = Hootils.CreatePrefabInfo(ClassId, sprite);
            s_TechType = info.TechType;
            return info;
        }

        protected override CustomPrefab CreatePrefab(PrefabInfo info)
        {
            CustomPrefab prefab = new CustomPrefab(info);
            prefab.SetRecipe(new RecipeData(
                    new CraftData.Ingredient(TechType.Compass, 1),
                    new CraftData.Ingredient(TechType.ComputerChip, 1),
                    new CraftData.Ingredient(TechType.Polyaniline, 1),
                    new CraftData.Ingredient(TechType.AramidFibers, 1)
                ))
                .WithFabricatorType(CraftTree.Type.Fabricator)
                .WithStepsToFabricatorTab(CraftTreeHandler.Paths.FabricatorEquipment);
            prefab.SetPdaGroupCategory(TechGroup.Personal, TechCategory.Equipment)
                .WithEncyclopediaEntry("Tech/Equipment", null, unlockSound: PDAHandler.UnlockImportant);
            prefab.SetEquipment(EquipmentType.Chip)
                .WithQuickSlotType(QuickSlotType.None);

            var template = new CloneTemplate(info, TechType.Compass);
            prefab.SetGameObject(template);

            return prefab;
        }
        
        protected override bool ShouldActivateForConfig(ConfigSave config)
        {
            return true;
        }
    }
}