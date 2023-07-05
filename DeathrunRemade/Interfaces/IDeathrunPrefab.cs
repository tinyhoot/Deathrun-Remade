using DeathrunRemade.Items;
using Nautilus.Assets;

namespace DeathrunRemade.Interfaces
{
    internal interface IDeathrunPrefab
    {
        public PrefabInfo GetPrefabInfo();
        public CustomPrefab GetPrefab();
    }

    internal static class DeathRunPrefabExtensions
    {
        public static string GetClassId(this IDeathrunPrefab prefab)
        {
            return ItemInfo.ClassIds.GetOrDefault(prefab.GetType().ToString(), null);
        }
    }
}