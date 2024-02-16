using DeathrunRemade.Configuration;
using HootLib;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;
using Nautilus.Handlers;
using Story;
using static CraftData;

namespace DeathrunRemade.Items
{
    internal class DecompressionModule : DeathrunPrefabBase
    {
        public static TechType s_TechType;
        public const TechType UnlockTechType = TechType.Cyclops;
        public new const string ClassId = ClassIdPrefix + "decompressionmodule";

        protected override PrefabInfo CreatePrefabInfo()
        {
            var sprite = SpriteManager.Get(TechType.PowerUpgradeModule);
            PrefabInfo info = Hootils.CreatePrefabInfo(ClassId, sprite);
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
            prefab.SetPdaGroupCategory(TechGroup.VehicleUpgrades, TechCategory.VehicleUpgrades)
                .WithEncyclopediaEntry("Tech/Vehicles", null, unlockSound: PDAHandler.UnlockImportant);
            prefab.SetEquipment(EquipmentType.VehicleModule)
                .WithQuickSlotType(QuickSlotType.Passive);

            var template = new CloneTemplate(info, TechType.VehicleArmorPlating);
            prefab.SetGameObject(template);
            
            RegisterUnlockData(info.TechType);
            return prefab;
        }

        protected override bool ShouldActivateForConfig(ConfigSave config)
        {
            return true;
        }

        /// <summary>
        /// Register story goals to unlock this item. Because the handler has no way to undo its changes this must
        /// only be triggered once.
        /// </summary>
        private void RegisterUnlockData(TechType techType)
        {
            // Unlock the decompression module when a Cyclops is first constructed.
            StoryGoalHandler.RegisterItemGoal(ClassId, Story.GoalType.Encyclopedia, UnlockTechType);
            StoryGoalHandler.RegisterOnGoalUnlockData(ClassId, new[]
            {
                new UnlockBlueprintData
                {
                    unlockType = UnlockBlueprintData.UnlockType.Available,
                    techType = techType
                }
            });
        }
    }
}