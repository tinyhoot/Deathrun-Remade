using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Enums;
using HarmonyLib;
using HootLib;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;
using Nautilus.Handlers;
using UnityEngine;
using static CraftData;

namespace DeathrunRemade.Items
{
    internal class AcidBattery : DeathrunPrefabBase
    {
        public new static TechType TechType;
        
        protected override PrefabInfo CreatePrefabInfo()
        {
            var sprite = Hootils.LoadSprite("AcidBattery.png", true);
            PrefabInfo info = Hootils.CreatePrefabInfo(ClassIdPrefix + "acidbattery", sprite);
            TechType = info.TechType;
            return info;
        }

        protected override CustomPrefab CreatePrefab(PrefabInfo info)
        {
            CustomPrefab prefab = new CustomPrefab(_prefabInfo);
            prefab.SetRecipe(new RecipeData(
                    new Ingredient(TechType.Copper, 1),
                    new Ingredient(TechType.AcidMushroom, 2)
                ))
                .WithFabricatorType(CraftTree.Type.Fabricator)
                .WithStepsToFabricatorTab(CraftTreeHandler.Paths.FabricatorsElectronics);
            prefab.SetPdaGroupCategory(TechGroup.Resources, TechCategory.Electronics);
            prefab.SetUnlock(TechType.AcidMushroom);

            var template = new EnergySourceTemplate(_prefabInfo, 100);
            template.ModifyPrefab += ChangeCapacity;
            prefab.SetGameObject(template);

            return prefab;
        }

        public override void Register()
        {
            base.Register();
            AddRecyclingRecipe();
        }

        public override void Unregister()
        {
            base.Unregister();
            // Also remove the recycling recipe.
            CraftDataHandler.SetRecipeData(TechType.Copper, null);
            CraftTreeHandler.RemoveNode(CraftTree.Type.Fabricator, CraftTreeHandler.Paths.FabricatorsElectronics.AddToArray(TechType.Copper.AsString()));
        }

        /// <summary>
        /// Add a recipe for recycling acid batteries.
        /// </summary>
        private void AddRecyclingRecipe()
        {
            RecipeData recycling = new RecipeData(new Ingredient(TechType, 3));
            recycling.craftAmount = 2;
            CraftDataHandler.SetRecipeData(TechType.Copper, recycling);
            CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, TechType.Copper,
                CraftTreeHandler.Paths.FabricatorsElectronics);
            // Make sure acid battery recycling is always accessible.
            KnownTechHandler.UnlockOnStart(TechType.Copper);
        }

        /// <summary>
        /// The template requires the capacity value before our save data has loaded. Use this extra method to change
        /// the capacity every time an acid battery is created.
        /// </summary>
        private void ChangeCapacity(GameObject gameObject)
        {
            int capacity = GetCapacityForDifficulty(SaveData.Main.Config.BatteryCapacity);
            gameObject.GetComponent<Battery>()._capacity = capacity;
        }

        public static int GetCapacityForDifficulty(Difficulty4 difficulty)
        {
            return difficulty switch
            {
                Difficulty4.Normal => 125,
                Difficulty4.Hard => 100,
                Difficulty4.Deathrun => 75,
                Difficulty4.Kharaa => 50,
                _ => 100
            };
        }
    }
}