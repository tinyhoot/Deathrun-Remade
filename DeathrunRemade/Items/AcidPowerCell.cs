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
        public new static TechType TechType;
        
        public AcidPowerCell()
        {
            var sprite = Hootils.LoadSprite("AcidPowerCell.png", true);
            _prefabInfo = Hootils.CreatePrefabInfo(
                ClassIdPrefix + "acidpowercell",
                null,
                null,
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
            _prefab.SetUnlock(AcidBattery.TechType);
            _prefab.SetEquipment(EquipmentType.PowerCellCharger);

            var template = new EnergySourceTemplate(_prefabInfo, 200);
            template.ModifyPrefab += ChangeCapacity;
            _prefab.SetGameObject(template);
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