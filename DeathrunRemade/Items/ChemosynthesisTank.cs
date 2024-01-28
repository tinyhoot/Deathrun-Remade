using HootLib;
using Nautilus.Assets;
using Nautilus.Crafting;

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
                new CraftData.Ingredient(TechType.PlasteelTank, 1),
                new CraftData.Ingredient(ThermophileSample.s_TechType, 4),
                new CraftData.Ingredient(TechType.Kyanite, 1));
        }

        protected override Atlas.Sprite GetSprite()
        {
            return Hootils.LoadSprite("chemosynthesistank.png", true);
        }

        protected override TechType GetUnlock()
        {
            return TechType.PlasteelTank;
        }
    }
}