using DeathrunRemade.Configuration;
using DeathrunRemade.Objects.Enums;
using HootLib;
using Nautilus.Assets;
using Nautilus.Handlers;

namespace DeathrunRemade.Items
{
    internal class LavaLizardScale : MobDropBase
    {
        public static TechType s_TechType;

        protected override void AssignTechType(PrefabInfo info)
        {
            s_TechType = info.TechType;
        }

        protected override string GetClassId()
        {
            return ClassIdPrefix + "lavalizardscale";
        }

        protected override Atlas.Sprite GetSprite()
        {
            return Hootils.LoadSprite("lavalizardscale.png", true);
        }

        protected override bool ShouldActivateForConfig(ConfigSave config)
        {
            return config.PersonalCrushDepth > Difficulty3.Hard;
        }

        protected override void RegisterHarvestData()
        {
            CraftDataHandler.SetHarvestOutput(TechType.LavaLizard, TechType);
            CraftDataHandler.SetHarvestType(TechType.LavaLizard, HarvestType.DamageAlive);
        }

        protected override void UnregisterHarvestData()
        {
            CraftDataHandler.SetHarvestOutput(TechType.LavaLizard, TechType.None);
            CraftDataHandler.SetHarvestType(TechType.LavaLizard, HarvestType.None);
        }
    }
}