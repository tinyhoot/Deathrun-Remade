using DeathrunRemade.Configuration;
using HootLib;
using Nautilus.Assets;
using Nautilus.Handlers;

namespace DeathrunRemade.Items
{
    internal class ThermophileSample : MobDropBase
    {
        public static TechType s_TechType;

        protected override void AssignTechType(PrefabInfo info)
        {
            s_TechType = info.TechType;
        }

        protected override string GetClassId()
        {
            return ClassIdPrefix + "thermophilesample";
        }

        protected override Atlas.Sprite GetSprite()
        {
            return Hootils.LoadSprite("thermophilesample.png", true);
        }

        protected override bool ShouldActivateForConfig(ConfigSave config)
        {
            return config.SpecialAirTanks;
        }

        protected override void RegisterHarvestData()
        {
            CraftDataHandler.SetHarvestOutput(TechType.LavaLarva, TechType);
            CraftDataHandler.SetHarvestType(TechType.LavaLarva, HarvestType.DamageAlive);
        }

        protected override void UnregisterHarvestData()
        {
            CraftDataHandler.SetHarvestOutput(TechType.LavaLarva, TechType.None);
            CraftDataHandler.SetHarvestType(TechType.LavaLarva, HarvestType.None);
        }
    }
}