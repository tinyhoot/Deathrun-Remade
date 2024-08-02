using DeathrunRemade.Configuration;
using DeathrunRemade.Objects.Enums;
using HootLib;
using Nautilus.Assets;
using Nautilus.Handlers;

namespace DeathrunRemade.Items
{
    internal class SpineEelScale : MobDropBase
    {
        public static TechType s_TechType;

        protected override void AssignTechType(PrefabInfo info)
        {
            s_TechType = info.TechType;
        }

        protected override string GetClassId()
        {
            return ClassIdPrefix + "spineeelscale";
        }

        protected override Atlas.Sprite GetSprite()
        {
            return Hootils.LoadSprite("rivereelscale.png", true);
        }

        protected override bool ShouldActivateForConfig(ConfigSave config)
        {
            return config.PersonalCrushDepth > Difficulty3.Hard;
        }

        protected override void RegisterHarvestData()
        {
            CraftDataHandler.SetHarvestOutput(TechType.SpineEel, TechType);
            CraftDataHandler.SetHarvestType(TechType.SpineEel, HarvestType.DamageAlive);
        }

        protected override void UnregisterHarvestData()
        {
            CraftDataHandler.SetHarvestOutput(TechType.SpineEel, TechType.None);
            CraftDataHandler.SetHarvestType(TechType.SpineEel, HarvestType.None);
        }
    }
}