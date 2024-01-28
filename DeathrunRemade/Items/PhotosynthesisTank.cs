using HootLib;
using Nautilus.Assets;
using Nautilus.Crafting;

namespace DeathrunRemade.Items
{
    internal class PhotosynthesisTank : TankBase
    {
        public static TechType s_TechType;

        protected override void AssignTechType(PrefabInfo info)
        {
            s_TechType = info.TechType;
        }

        protected override string GetClassId()
        {
            return ClassIdPrefix + "photosynthesistank";
        }

        protected override TechType GetCloneType()
        {
            return TechType.PlasteelTank;
        }

        protected override RecipeData GetRecipe()
        {
            return new RecipeData(
                new CraftData.Ingredient(TechType.PlasteelTank, 1),
                new CraftData.Ingredient(TechType.PurpleBrainCoralPiece, 2),
                new CraftData.Ingredient(TechType.EnameledGlass, 1));
        }

        protected override Atlas.Sprite GetSprite()
        {
            return Hootils.LoadSprite("photosynthesistank.png", true);
        }

        protected override TechType GetUnlock()
        {
            return TechType.PlasteelTank;
        }
    }
}