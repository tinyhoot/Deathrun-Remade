using System;
using System.Collections.Generic;

namespace DeathrunRemade.Items
{
    internal static class ItemInfo
    {
        private const string _prefix = "deathrunremade_";
        
        public static readonly Dictionary<Type, string> ClassIds = new Dictionary<Type, string>
        {
            { typeof(AcidBattery), _prefix + "acidbattery" },
            { typeof(AcidPowerCell), _prefix + "acidpowercell" },
        };

        public static readonly Dictionary<Type, DeathrunPrefabBase> Prefabs = new Dictionary<Type, DeathrunPrefabBase>();

        public static void AddPrefab(DeathrunPrefabBase prefab)
        {
            Prefabs.Add(prefab.GetType(), prefab);
        }

        public static string GetIdForItem(Type item)
        {
            return ClassIds.GetOrDefault(item, null);
        }

        public static TechType GetTechTypeForItem(Type item)
        {
            return Prefabs.GetOrDefault(item, null)?.GetPrefabInfo().TechType ?? TechType.None;
        }
    }
}