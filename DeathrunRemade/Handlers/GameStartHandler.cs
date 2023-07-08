using DeathrunRemade.Monos;
using HarmonyLib;

namespace DeathrunRemade.Handlers
{
    [HarmonyPatch]
    public static class GameStartHandler
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
        public static void AddComponents(ref Player __instance)
        {
            __instance.gameObject.EnsureComponent<DeathrunTank>();
        }
    }
}