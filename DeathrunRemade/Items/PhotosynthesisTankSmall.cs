using HootLib;
using Nautilus.Assets;
using Nautilus.Crafting;
using UnityEngine;

namespace DeathrunRemade.Items
{
    internal class PhotosynthesisTankSmall : TankBase
    {
        public static TechType s_TechType;

        protected override void AssignTechType(PrefabInfo info)
        {
            s_TechType = info.TechType;
        }

        protected override string GetClassId()
        {
            return ClassIdPrefix + "photosynthesistanksmall";
        }

        protected override TechType GetCloneType()
        {
            return TechType.Tank;
        }

        protected override RecipeData GetRecipe()
        {
            return new RecipeData(
                new Ingredient(TechType.Tank, 1),
                new Ingredient(TechType.PurpleBrainCoralPiece, 1),
                new Ingredient(TechType.Glass, 1));
        }

        protected override Sprite GetSprite()
        {
            return Hootils.LoadSprite("photosynthesissmalltank.png", true);
        }

        protected override TechType GetUnlock()
        {
            return TechType.Tank;
        }
    }
}