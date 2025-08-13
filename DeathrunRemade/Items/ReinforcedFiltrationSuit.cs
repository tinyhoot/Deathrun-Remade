using DeathrunRemade.Handlers;
using HootLib;
using Nautilus.Assets;
using Nautilus.Crafting;
using UnityEngine;

namespace DeathrunRemade.Items
{
    internal class ReinforcedFiltrationSuit : SuitBase
    {
        public static TechType s_TechType;
        
        protected override void AssignTechType(PrefabInfo info)
        {
            s_TechType = info.TechType;
        }

        protected override string GetClassId()
        {
            return ClassIdPrefix + "reinforcedfiltrationsuit";
        }

        protected override TechType GetCloneType()
        {
            return TechType.WaterFiltrationSuit;
        }

        protected override RecipeData GetRecipe()
        {
            return new RecipeData(
                new Ingredient(TechType.WaterFiltrationSuit, 1),
                new Ingredient(SpineEelScale.s_TechType, 2),
                new Ingredient(TechType.AramidFibers, 2));
        }

        protected override Sprite GetSprite()
        {
            return Hootils.LoadSprite("reinforcedstillsuit.png", true);
        }

        protected override TechType GetUnlock()
        {
            return SpineEelScale.s_TechType;
        }

        protected override void UnlockSuitOnScanFish(PDAScanner.Entry entry)
        {
            if (entry is null)
                return;

            if (entry.techType == TechType.SpineEel)
                KnownTech.Add(TechType);
        }

        protected override float[] GetCrushDepths()
        {
            return new[] { CrushDepthHandler.InfiniteCrushDepth, 1300f };
        }

        protected override float[] GetNitrogenModifiers()
        {
            return new[] { 0.25f, 0.2f };
        }
        
        protected override float GetTemperatureLimit()
        {
            return 64f;
        }
    }
}