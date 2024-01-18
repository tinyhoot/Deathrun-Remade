using DeathrunRemade.Handlers;
using DeathrunRemade.Patches;
using HootLib;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;
using Nautilus.Handlers;
using static CraftData;

namespace DeathrunRemade.Items
{
    /// <summary>
    /// A class representing all new special dive suit upgrades.
    /// </summary>
    internal class Suit : DeathrunPrefabBase
    {
        public const string WorkbenchSuitTab = ClassIdPrefix + "specialsuits";
        
        public enum Variant
        {
            ReinforcedSuitMk2,
            ReinforcedSuitMk3,
            ReinforcedFiltrationSuit,
        }

        public Variant SuitVariant { get; }
        public static TechType ReinforcedFiltration;
        public static TechType ReinforcedMk2;
        public static TechType ReinforcedMk3;
        
        public Suit(Variant variant)
        {
            SuitVariant = variant;
            
            _prefabInfo = Hootils.CreatePrefabInfo(
                GetClassId(variant),
                GetDisplayName(variant),
                GetDescription(variant),
                GetSprite(variant)
            );
            AssignTechType(_prefabInfo, variant);

            _prefab = new CustomPrefab(_prefabInfo);
            _prefab.SetRecipe(GetRecipe(variant))
                .WithFabricatorType(CraftTree.Type.Workbench)
                .WithStepsToFabricatorTab(WorkbenchSuitTab);
            _prefab.SetPdaGroupCategory(TechGroup.Personal, TechCategory.Workbench);
            _prefab.SetEquipment(EquipmentType.Body);
            _prefabInfo.WithSizeInInventory(new Vector2int(2, 2));
            _prefab.SetUnlock(GetUnlock(variant))
                .WithAnalysisTech(null, unlockSound: PDAHandler.UnlockImportant);

            TechType cloneType = variant == Variant.ReinforcedFiltrationSuit ? TechType.WaterFiltrationSuit : TechType.ReinforcedDiveSuit;
            var template = new CloneTemplate(_prefabInfo, cloneType);
            _prefab.SetGameObject(template);
        }

        private void AssignTechType(PrefabInfo info, Variant variant)
        {
            switch (variant)
            {
                case Variant.ReinforcedFiltrationSuit:
                    ReinforcedFiltration = info.TechType;
                    break;
                case Variant.ReinforcedSuitMk2:
                    ReinforcedMk2 = info.TechType;
                    break;
                case Variant.ReinforcedSuitMk3:
                    ReinforcedMk3 = info.TechType;
                    break;
            }
        }
        
        /// <summary>
        /// Get the class id for the type of suit.
        /// </summary>
        private string GetClassId(Variant variant)
        {
            string id = variant switch
            {
                Variant.ReinforcedFiltrationSuit => "reinforcedfiltrationsuit",
                Variant.ReinforcedSuitMk2 => "reinforcedsuit2",
                Variant.ReinforcedSuitMk3 => "reinforcedsuit3",
                _ => null
            };
            return $"{ClassIdPrefix}{id}";
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
                    new Ingredient(MobDrop.SpineEelScale, 2)),
                Variant.ReinforcedSuitMk3 => new RecipeData(
                    new Ingredient(ReinforcedMk2, 1),
                    new Ingredient(TechType.AramidFibers, 1),
                    new Ingredient(TechType.Kyanite, 2),
                    new Ingredient(MobDrop.LavaLizardScale, 2)),
                Variant.ReinforcedFiltrationSuit => new RecipeData(
                    new Ingredient(TechType.WaterFiltrationSuit, 1),
                    new Ingredient(MobDrop.SpineEelScale, 2),
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

        /// <summary>
        /// Gets the right unlocking item for the type of suit.
        /// </summary>
        private TechType GetUnlock(Variant variant)
        {
            return variant switch
            {
                Variant.ReinforcedFiltrationSuit => MobDrop.SpineEelScale,
                Variant.ReinforcedSuitMk2 => MobDrop.SpineEelScale,
                Variant.ReinforcedSuitMk3 => MobDrop.LavaLizardScale,
                _ => TechType.None
            };
        }

        /// <summary>
        /// In addition to unlocking when the right item is picked up, also unlock these suits when the fish the item
        /// belongs to is scanned.
        /// </summary>
        public static void UnlockSuitOnScanFish(PDAScanner.Entry entry)
        {
            if (entry is null)
                return;
            TechType techType = entry.techType;
            if (techType == TechType.SpineEel)
                KnownTech.AddRange(new []{ ReinforcedFiltration, ReinforcedMk2 }, true);
            if (techType == TechType.LavaLizard)
                KnownTech.Add(ReinforcedMk3, true);
            
        }

        /// <summary>
        /// Register the crush depths of all suits to the <see cref="CrushDepthHandler"/>.
        /// </summary>
        public static void RegisterCrushDepths()
        {
            // Using our own API for this rather than internal methods catches any issues with the API much earlier.
            DeathrunAPI.AddSuitCrushDepth(ReinforcedFiltration, new[] { CrushDepthHandler.InfiniteCrushDepth, 1300f });
            DeathrunAPI.AddSuitCrushDepth(ReinforcedMk2, new[] { CrushDepthHandler.InfiniteCrushDepth, 1300f });
            DeathrunAPI.AddSuitCrushDepth(ReinforcedMk3, CrushDepthHandler.InfiniteCrushDepth);
        }

        /// <summary>
        /// Register the nitrogen modifiers of all suits to the <see cref="NitrogenHandler"/>.
        /// </summary>
        public static void RegisterNitrogenModifiers()
        {
            // Using our own API for this rather than internal methods catches any issues with the API much earlier.
            DeathrunAPI.AddNitrogenModifier(ReinforcedFiltration, new[] { 0.25f, 0.2f });
            DeathrunAPI.AddNitrogenModifier(ReinforcedMk2, new[] { 0.25f, 0.2f });
            DeathrunAPI.AddNitrogenModifier(ReinforcedMk3, new[] { 0.45f, 0.3f });
        }

        /// <summary>
        /// Get the temperature limit of the given suit.
        /// </summary>
        public static float GetTemperatureLimit(TechType techType)
        {
            float tempLimit = SuitPatcher.MinTemperatureLimit;
            if (techType.Equals(ReinforcedFiltration))
                tempLimit += 15f;
            if (techType.Equals(ReinforcedMk2))
                tempLimit += 20f;
            if (techType.Equals(ReinforcedMk3))
                tempLimit += 30f;
            return tempLimit;
        }
    }
}