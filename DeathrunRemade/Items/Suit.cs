using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;
using HootLib;
using static CraftData;

namespace DeathrunRemade.Items
{
    /// <summary>
    /// A class representing all new special dive suit upgrades.
    /// </summary>
    internal class Suit : DeathrunPrefabBase
    {
        public enum Variant
        {
            ReinforcedSuitMk2,
            ReinforcedSuitMk3,
            ReinforcedFiltrationSuit,
        }

        public Variant SuitVariant;
        
        public Suit(Variant variant)
        {
            SuitVariant = variant;
            
            _prefabInfo = Hootils.CreatePrefabInfo(
                ItemInfo.GetIdForItem(variant.ToString()),
                GetDisplayName(variant),
                GetDescription(variant),
                GetSprite(variant)
            );

            _prefab = new CustomPrefab(_prefabInfo);
            _prefab.SetRecipe(GetRecipe(variant))
                .WithFabricatorType(CraftTree.Type.Workbench)
                .WithStepsToFabricatorTab(ItemInfo.GetSuitCraftTabId());
            _prefab.SetPdaGroupCategory(TechGroup.Personal, TechCategory.Equipment);
            _prefab.SetEquipment(EquipmentType.Body);
            _prefabInfo.WithSizeInInventory(new Vector2int(2, 2));
            _prefab.SetUnlock(TechType.HatchingEnzymes);

            TechType cloneType = variant == Variant.ReinforcedFiltrationSuit ? TechType.WaterFiltrationSuit : TechType.ReinforcedDiveSuit;
            var template = new CloneTemplate(_prefabInfo, cloneType);
            _prefab.SetGameObject(template);
            _prefab.Register();
        }

        /// <summary>
        /// Gets the display name for the type of suit upgrade.
        /// </summary>
        private string GetDisplayName(Variant variant)
        {
            return variant switch
            {
                Variant.ReinforcedSuitMk2 => "Reinforced Dive Suit Mk2",
                Variant.ReinforcedSuitMk3 => "Reinforced Dive Suit Mk3",
                Variant.ReinforcedFiltrationSuit => "Reinforced Water Filtration Suit",
                _ => null
            };
        }

        /// <summary>
        /// Gets the description for the type of suit upgrade.
        /// </summary>
        private string GetDescription(Variant variant)
        {
            return variant switch
            {
                Variant.ReinforcedSuitMk2 => "An upgraded dive suit capable of protecting the user at depths up to "
                                             + "1300m and providing heat protection up to 75C.",
                Variant.ReinforcedSuitMk3 => "An upgraded dive suit capable of protecting the user at all depths and "
                                             + "providing heat protection up to 90C.",
                Variant.ReinforcedFiltrationSuit => "An upgraded filtration suit capable of protecting the user at "
                                                    + "depths up to 1300m and temperatures up to 70C.",
                _ => null
            };
        }

        /// <summary>
        /// Gets the right recipe for the type of suit upgrade.
        /// </summary>
        private RecipeData GetRecipe(Variant variant)
        {
            RecipeData recipe = variant switch
            {
                Variant.ReinforcedSuitMk2 => new RecipeData(
                    new Ingredient(TechType.ReinforcedDiveSuit, 1),
                    new Ingredient(TechType.AramidFibers, 1),
                    new Ingredient(TechType.AluminumOxide, 2),
                    new Ingredient(ItemInfo.GetTechTypeForItem(nameof(MobDrop.Variant.SpineEelScale)), 2)),
                Variant.ReinforcedSuitMk3 => new RecipeData(
                    new Ingredient(ItemInfo.GetTechTypeForItem(nameof(Variant.ReinforcedSuitMk2)), 1),
                    new Ingredient(TechType.AramidFibers, 1),
                    new Ingredient(TechType.Kyanite, 2),
                    new Ingredient(ItemInfo.GetTechTypeForItem(nameof(MobDrop.Variant.LavaLizardScale)), 2)),
                Variant.ReinforcedFiltrationSuit => new RecipeData(
                    new Ingredient(TechType.WaterFiltrationSuit, 1),
                    new Ingredient(ItemInfo.GetTechTypeForItem(nameof(MobDrop.Variant.SpineEelScale)), 2),
                    new Ingredient(TechType.AramidFibers, 2)),
                _ => null
            };
            return recipe;
        }

        /// <summary>
        /// Gets the right sprite for the suit upgrade.
        /// </summary>
        private Atlas.Sprite GetSprite(Variant variant)
        {
            string filePath = variant switch
            {
                Variant.ReinforcedSuitMk2 => "reinforcedsuit2.png",
                Variant.ReinforcedSuitMk3 => "reinforcedsuit3.png",
                Variant.ReinforcedFiltrationSuit => "reinforcedstillsuit.png",
                _ => null
            };
            return Hootils.LoadSprite(filePath, true);
        }
    }
}