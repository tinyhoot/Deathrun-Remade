using DeathrunRemade.Components;
using DeathrunRemade.Handlers;
using DeathrunRemade.Objects;
using HootLib;
using Nautilus.Assets;
using Nautilus.Assets.Gadgets;
using Nautilus.Assets.PrefabTemplates;
using Nautilus.Crafting;
using static CraftData;

namespace DeathrunRemade.Items
{
    /// <summary>
    /// An overall class for representing all special oxygen tanks.
    /// </summary>
    internal class Tank : DeathrunPrefabBase
    {
        public const string WorkbenchTankTab = ClassIdPrefix + "specialtanks";
        
        public enum Variant
        {
            ChemosynthesisTank,
            PhotosynthesisTank,
            PhotosynthesisTankSmall,
        }
        
        public Variant TankVariant { get; }
        public static TechType ChemosynthesisTank;
        public static TechType PhotosynthesisTank;
        public static TechType PhotosynthesisTankSmall;
        
        public Tank(Variant variant)
        {
            TankVariant = variant;
            
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
                .WithStepsToFabricatorTab(WorkbenchTankTab);
            _prefab.SetPdaGroupCategory(TechGroup.Personal, TechCategory.Equipment);
            _prefab.SetEquipment(EquipmentType.Tank);
            _prefabInfo.WithSizeInInventory(new Vector2int(2, 3));
            // The small tank is unlocked earlier and easier to acquire than the two bigger ones.
            TechType unlock = variant == Variant.PhotosynthesisTankSmall ? TechType.Rebreather : TechType.PlasteelTank;
            _prefab.SetUnlock(unlock);

            TechType cloneType = variant == Variant.PhotosynthesisTankSmall ? TechType.Tank : TechType.PlasteelTank;
            var template = new CloneTemplate(_prefabInfo, cloneType);
            _prefab.SetGameObject(template);
            
            // Add the special tank behaviour to the player as soon as they're ready.
            // This won't do anything if the player isn't wearing any special tanks, so no config check necessary.
            GameEventHandler.OnPlayerAwake += player => player.gameObject.EnsureComponent<DeathrunTank>();
        }

        public override void Register()
        {
            // Register these special tanks later, and only if they're actually enabled in the config.
            SaveData.OnSaveDataLoaded += data =>
            {
                if (data.Config.SpecialAirTanks)
                    _prefab.Register();
            };
        }
        
        private void AssignTechType(PrefabInfo info, Variant variant)
        {
            switch (variant)
            {
                case Variant.ChemosynthesisTank:
                    ChemosynthesisTank = info.TechType;
                    break;
                case Variant.PhotosynthesisTank:
                    PhotosynthesisTank = info.TechType;
                    break;
                case Variant.PhotosynthesisTankSmall:
                    PhotosynthesisTankSmall = info.TechType;
                    break;
            }
        }
        
        /// <summary>
        /// Get the class id for the type of tank.
        /// </summary>
        private string GetClassId(Variant variant)
        {
            string id = variant switch
            {
                Variant.ChemosynthesisTank => "chemosynthesistank",
                Variant.PhotosynthesisTank => "photosynthesistank",
                Variant.PhotosynthesisTankSmall => "photosynthesistanksmall",
                _ => null
            };
            return $"{ClassIdPrefix}{id}";
        }
        
        /// <summary>
        /// Gets the display name for the type of tank.
        /// </summary>
        private string GetDisplayName(Variant variant)
        {
            return variant switch
            {
                Variant.ChemosynthesisTank => "Chemosynthesis Tank",
                Variant.PhotosynthesisTank => "Photosynthesis Tank",
                Variant.PhotosynthesisTankSmall => "Small Photosynthesis Tank",
                _ => null
            };
        }

        /// <summary>
        /// Gets the description for the type of tank.
        /// </summary>
        private string GetDescription(Variant variant)
        {
            return variant switch
            {
                Variant.ChemosynthesisTank => "A lightweight O2 tank that houses microorganisms that produce oxygen under high temperatures.",
                Variant.PhotosynthesisTank => "A lightweight air tank housing microorganisms which produce oxygen when exposed to sunlight.",
                Variant.PhotosynthesisTankSmall => "An air tank housing microorganisms which produce oxygen when exposed to sunlight.",
                _ => null
            };
        }
        
        /// <summary>
        /// Gets the recipe for the tank.
        /// </summary>
        private RecipeData GetRecipe(Variant variant)
        {
            RecipeData recipe = variant switch
            {
                Variant.ChemosynthesisTank => new RecipeData(
                    new Ingredient(TechType.PlasteelTank, 1),
                    new Ingredient(MobDrop.ThermophileSample, 4),
                    new Ingredient(TechType.Kyanite, 1)),
                Variant.PhotosynthesisTank => new RecipeData(
                    new Ingredient(TechType.PlasteelTank, 1),
                    new Ingredient(TechType.PurpleBrainCoralPiece, 2),
                    new Ingredient(TechType.EnameledGlass, 1)),
                Variant.PhotosynthesisTankSmall => new RecipeData(
                    new Ingredient(TechType.Tank, 1),
                    new Ingredient(TechType.PurpleBrainCoralPiece, 1),
                    new Ingredient(TechType.Glass, 1)),
                _ => null
            };
            return recipe;
        }

        /// <summary>
        /// Gets the right sprite for the tank.
        /// </summary>
        private Atlas.Sprite GetSprite(Variant variant)
        {
            string filePath = variant switch
            {
                Variant.ChemosynthesisTank => "chemosynthesistank.png",
                Variant.PhotosynthesisTank => "photosynthesistank.png",
                Variant.PhotosynthesisTankSmall => "photosynthesissmalltank.png",
                _ => null
            };
            return Hootils.LoadSprite(filePath, true);
        }
    }
}