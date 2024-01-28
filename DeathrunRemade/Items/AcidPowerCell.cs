using DeathrunRemade.Configuration;
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
    internal class AcidPowerCell : DeathrunPrefabBase
    {
        public static TechType s_TechType;
        
        protected override PrefabInfo CreatePrefabInfo()
        {
            var sprite = Hootils.LoadSprite("AcidPowerCell.png", true);
            PrefabInfo info = Hootils.CreatePrefabInfo(ClassIdPrefix + "acidpowercell", sprite);
            s_TechType = info.TechType;
            return info;
        }

        protected override CustomPrefab CreatePrefab(PrefabInfo info)
        {
            CustomPrefab prefab = new CustomPrefab(info);
            prefab.SetRecipe(new RecipeData(
                    new Ingredient(TechType.Lead, 2),
                    new Ingredient(TechType.AcidMushroom, 4),
                    new Ingredient(TechType.Silicone, 1)
                ))
                .WithFabricatorType(CraftTree.Type.Fabricator)
                .WithStepsToFabricatorTab(CraftTreeHandler.Paths.FabricatorsElectronics);
            prefab.SetPdaGroupCategory(TechGroup.Resources, TechCategory.Electronics);
            prefab.SetUnlock(AcidBattery.s_TechType);
            prefab.SetEquipment(EquipmentType.PowerCellCharger);

            var template = new EnergySourceTemplate(info, 200);
            template.ModifyPrefab += ChangeCapacity;
            prefab.SetGameObject(template);

            return prefab;
        }
        
        protected override bool ShouldActivateForConfig(ConfigSave config)
        {
            // Disable these batteries on low difficulty.
            return config.BatteryCosts > Difficulty4.Normal;
        }

        /// <summary>
        /// The template requires the capacity value before our save data has loaded. Use this extra method to change
        /// the capacity every time an acid power cell is created.
        /// </summary>
        private void ChangeCapacity(GameObject gameObject)
        {
            int capacity = GetCapacityForDifficulty(SaveData.Main.Config.BatteryCapacity);
            gameObject.GetComponent<Battery>()._capacity = capacity;
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