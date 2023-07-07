using System.Collections.Generic;
using System.Linq;

namespace DeathrunRemade.Items
{
    internal static class ItemInfo
    {
        // Make testing easier by making item names shorter (but less unique!) in debug builds.
#if DEBUG
        private const string _prefix = "";
#else
        private const string _prefix = "deathrunremade_";
#endif
        public const string SuitTabKey = "WorkBenchSuitTab";
        public const string TankTabKey = "WorkBenchTankTab";
        
        public static readonly Dictionary<string, string> ClassIds = new Dictionary<string, string>
        {
            { nameof(AcidBattery), _prefix + "acidbattery" },
            { nameof(AcidPowerCell), _prefix + "acidpowercell" },
            { nameof(DecompressionModule), _prefix + "decompressionmodule" },
            { nameof(FilterChip), _prefix + "filterchip" },
            { nameof(MobDrop.Variant.LavaLizardScale), _prefix + "lavalizardscale" },
            { nameof(MobDrop.Variant.SpineEelScale), _prefix + "spineeelscale" },
            { nameof(MobDrop.Variant.ThermophileSample), _prefix + "thermophilesample" },
            { nameof(Suit.Variant.ReinforcedFiltrationSuit), _prefix + "reinforcedfiltrationsuit" },
            { nameof(Suit.Variant.ReinforcedSuitMk2), _prefix + "reinforcedsuit2" },
            { nameof(Suit.Variant.ReinforcedSuitMk3), _prefix + "reinforcedsuit3" },
            { nameof(Tank.Variant.ChemosynthesisTank), _prefix + "chemosynthesistank" },
            { nameof(Tank.Variant.PhotosynthesisTank), _prefix + "photosynthesistank"},
            { nameof(Tank.Variant.PhotosynthesisTankSmall), _prefix + "photosynthesistanksmall" },
            { SuitTabKey, _prefix + "specialsuits" },
            { TankTabKey, _prefix + "specialtanks" },
        };

        public static readonly Dictionary<string, DeathrunPrefabBase> Prefabs = new Dictionary<string, DeathrunPrefabBase>();

        /// <summary>
        /// Add a prefab to the registry for easy access across the mod.
        /// </summary>
        /// <param name="prefab">The prefab to register.</param>
        /// <param name="key">The key to register the prefab with. If not provided, uses the name of the prefab type
        /// by default.</param>
        public static void AddPrefab(DeathrunPrefabBase prefab, string key = null)
        {
            key ??= prefab.GetType().Name;
            DeathrunInit._Log.Debug($"Registering prefab {key}");
            Prefabs.Add(key, prefab);
        }

        /// <summary>
        /// Get the unique classId for the item.
        /// </summary>
        /// <param name="item">The internal name of the item.</param>
        public static string GetIdForItem(string item)
        {
            return ClassIds.GetOrDefault(item, null);
        }

        /// <summary>
        /// Get the prefab which was assigned the given TechType.
        /// </summary>
        /// <param name="techType">The Nautilus-generated TechType.</param>
        public static DeathrunPrefabBase GetPrefabForTechType(TechType techType)
        {
            return Prefabs.Values.First(prefab => prefab.TechType.Equals(techType));
        }

        /// <summary>
        /// Get the TechType the item was assigned by Nautilus.
        /// </summary>
        /// <param name="item">The internal name of the item.</param>
        public static TechType GetTechTypeForItem(string item)
        {
            return Prefabs.GetOrDefault(item, null)?.PrefabInfo.TechType ?? TechType.None;
        }

        /// <summary>
        /// Get the id used to register the craft tab for dive suit upgrades in the workbench.
        /// </summary>
        public static string GetSuitCraftTabId() => ClassIds[SuitTabKey];
        
        /// <summary>
        /// Get the id used to register the craft tab for special oxygen tank upgrades in the workbench.
        /// </summary>
        public static string GetTankCraftTabId() => ClassIds[TankTabKey];
    }
}