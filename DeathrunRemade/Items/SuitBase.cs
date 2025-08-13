using System.Linq;
using DeathrunRemade.Configuration;
using DeathrunRemade.Handlers;
using DeathrunRemade.Objects.Enums;
using HootLib;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;
using Nautilus.Handlers;
using UnityEngine;

namespace DeathrunRemade.Items
{
    /// <summary>
    /// A class representing all new special dive suit upgrades.
    /// </summary>
    internal abstract class SuitBase : DeathrunPrefabBase
    {
        public const string WorkbenchSuitTab = ClassIdPrefix + "specialsuits";
        
        protected override PrefabInfo CreatePrefabInfo()
        {
            PrefabInfo info = Hootils.CreatePrefabInfo(GetClassId(), GetSprite());
            info.WithSizeInInventory(new Vector2int(2, 2));
            AssignTechType(info);
            
            DeathrunAPI.AddSuitCrushDepth(info.TechType, GetCrushDepths());
            DeathrunAPI.AddNitrogenModifier(info.TechType, GetNitrogenModifiers());
            return info;
        }

        protected override CustomPrefab CreatePrefab(PrefabInfo info)
        {
            CustomPrefab prefab = new CustomPrefab(info);
            prefab.SetRecipe(GetRecipe())
                .WithFabricatorType(CraftTree.Type.Workbench)
                .WithStepsToFabricatorTab(WorkbenchSuitTab);
            prefab.SetPdaGroupCategory(TechGroup.Workbench, TechCategory.Workbench);
            prefab.SetEquipment(EquipmentType.Body);
            prefab.SetUnlock(GetUnlock())
                .WithAnalysisTech(null, unlockSound: PDAHandler.UnlockImportant);
            
            var template = new CloneTemplate(info, GetCloneType());
            prefab.SetGameObject(template);

            return prefab;
        }
        
        protected override bool ShouldActivateForConfig(ConfigSave config)
        {
            return config.PersonalCrushDepth > Difficulty3.Hard;
        }

        protected override void Register()
        {
            base.Register();
            PDAScanner.onAdd += UnlockSuitOnScanFish;
            GameEventHandler.OnPlayerAwake += CloneExistingSuitModel;
        }

        public override void Unregister()
        {
            base.Unregister();
            PDAScanner.onAdd -= UnlockSuitOnScanFish;
            GameEventHandler.OnPlayerAwake -= CloneExistingSuitModel;
        }

        /// <summary>
        /// Add model information to the game to ensure the correct player model shows up when wearing a suit.
        /// </summary>
        private void CloneExistingSuitModel(Player player)
        {
            TechType cloneType = GetCloneType();
            for (var slotIdx = 0; slotIdx < player.equipmentModels.Length; slotIdx++)
            {
                Player.EquipmentType equipmentType = player.equipmentModels[slotIdx];
                foreach (Player.EquipmentModel model in equipmentType.equipment)
                {
                    if (model.techType == cloneType)
                    {
                        DeathrunInit._Log.Debug($"Creating new model path entry for {TechType} based on {cloneType}");
                        Player.EquipmentModel customSuitModel = new()
                        {
                            // Creating a copy is required, otherwise items using the same model will turn each other off.
                            model = Object.Instantiate(model.model),
                            techType = TechType
                        };
                        player.equipmentModels[slotIdx].equipment = equipmentType.equipment.Append(customSuitModel).ToArray();
                        DeathrunInit._Log.Debug($"Model path: {customSuitModel.model}");
                        return;
                    }
                }
            }
            
            DeathrunInit._Log.Warn($"Failed to create model path entry for {TechType}");
        }

        protected abstract void AssignTechType(PrefabInfo info);

        /// <summary>
        /// Get the class id for the type of suit.
        /// </summary>
        protected abstract string GetClassId();

        protected abstract TechType GetCloneType();

        /// <summary>
        /// Gets the right recipe for the type of suit upgrade.
        /// </summary>
        protected abstract RecipeData GetRecipe();

        /// <summary>
        /// Gets the right sprite for the suit upgrade.
        /// </summary>
        protected abstract Sprite GetSprite();

        /// <summary>
        /// Gets the right unlocking item for the type of suit.
        /// </summary>
        protected abstract TechType GetUnlock();

        /// <summary>
        /// In addition to unlocking when the right item is picked up, also unlock these suits when the fish the item
        /// belongs to is scanned.
        /// </summary>
        protected abstract void UnlockSuitOnScanFish(PDAScanner.Entry entry);

        protected abstract float[] GetCrushDepths();

        protected abstract float[] GetNitrogenModifiers();

        protected abstract float GetTemperatureLimit();

        /// <summary>
        /// Try to get the custom temperature limit of the given suit. Returns false if the techType is not a suit
        /// registered by this mod.
        /// </summary>
        public static bool TryGetTemperatureLimit(TechType techType, out float limit)
        {
            // Vanilla limit without any suit at all is 49°C. Reinforced suit is 64°C.
            limit = 0f;

            // Iterate through all loaded custom suits and get the limit if we match.
            var suits = DeathrunInit.CustomItems.OfType<SuitBase>().ToList();
            foreach (SuitBase suit in suits)
            {
                if (suit.TechType == techType)
                    limit = suit.GetTemperatureLimit();
            }
            
            return limit != 0f;
        }
    }
}