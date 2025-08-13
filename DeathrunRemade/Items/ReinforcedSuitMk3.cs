using DeathrunRemade.Handlers;
using HootLib;
using Nautilus.Assets;
using Nautilus.Crafting;
using UnityEngine;

namespace DeathrunRemade.Items
{
    internal class ReinforcedSuitMk3 : SuitBase
    {
        public static TechType s_TechType;
        
        protected override void AssignTechType(PrefabInfo info)
        {
            s_TechType = info.TechType;
        }
        
        protected override string GetClassId()
        {
            return ClassIdPrefix + "reinforcedsuit3";
        }

        protected override TechType GetCloneType()
        {
            return TechType.ReinforcedDiveSuit;
        }

        protected override RecipeData GetRecipe()
        {
            return new RecipeData(
                new Ingredient(ReinforcedSuitMk2.s_TechType, 1),
                new Ingredient(TechType.AramidFibers, 1),
                new Ingredient(TechType.Kyanite, 2),
                new Ingredient(LavaLizardScale.s_TechType, 2));
        }

        protected override Sprite GetSprite()
        {
            return Hootils.LoadSprite("reinforcedsuit3.png", true);
        }

        protected override TechType GetUnlock()
        {
            return LavaLizardScale.s_TechType;
        }

        protected override void UnlockSuitOnScanFish(PDAScanner.Entry entry)
        {
            if (entry is null)
                return;

            if (entry.techType == TechType.LavaLizard)
                KnownTech.Add(TechType);
        }

        protected override float[] GetCrushDepths()
        {
            return new[] { CrushDepthHandler.InfiniteCrushDepth };
        }

        protected override float[] GetNitrogenModifiers()
        {
            return new[] { 0.45f, 0.3f };
        }
        
        protected override float GetTemperatureLimit()
        {
            return 79f;
        }
    }
}