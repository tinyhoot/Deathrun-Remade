using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Enums;
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
        
        public AcidBattery()
        {
            var sprite = Hootils.LoadSprite("AcidBattery.png", true);
            _prefabInfo = Hootils.CreatePrefabInfo(
                ClassIdPrefix + "acidbattery",
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

            var template = new EnergySourceTemplate(_prefabInfo, 100);
            template.ModifyPrefab += ChangeCapacity;
            _prefab.SetGameObject(template);
        }

        /// <summary>
        /// Add a recipe for recycling acid batteries.
        /// </summary>
        public static void AddRecyclingRecipe()
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