using HootLib;
using Nautilus.Assets;
using Nautilus.Crafting;
using UnityEngine;

namespace DeathrunRemade.Items
{
    internal class ChemosynthesisTank : TankBase
    {
        public static TechType s_TechType;

        protected override void AssignTechType(PrefabInfo info)
        {
            s_TechType = info.TechType;
        }

        protected override string GetClassId()
        {
            return ClassIdPrefix + "chemosynthesistank";
        }

        protected override TechType GetCloneType()
        {
            return TechType.PlasteelTank;
        }

        protected override RecipeData GetRecipe()
        {
            return new RecipeData(
                new Ingredient(TechType.PlasteelTank, 1),
                new Ingredient(ThermophileSample.s_TechType, 4),
                new Ingredient(TechType.Kyanite, 1));
        }

        protected override Sprite GetSprite()
        {
            return Hootils.LoadSprite("chemosynthesistank.png", true);
        }

        protected override TechType GetUnlock()
        {
            return TechType.PlasteelTank;
        }
    }
}