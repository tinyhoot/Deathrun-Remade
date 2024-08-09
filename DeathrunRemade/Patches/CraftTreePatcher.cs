using DeathrunRemade.Objects.Attributes;
using DeathrunRemade.Objects.Enums;
using HarmonyLib;

namespace DeathrunRemade.Patches
{
    [HarmonyPatch]
    [PatchCategory(ApplyPatch.Always)]
    internal class CraftTreePatcher
    {
        /// <summary>
        /// A common issue in this mod is recipes for custom items disappearing from one run to the next. This is caused
        /// by loading into a game, not interacting with a fabricator, and then starting a new game.
        /// The issue lies with Nautilus' way of caching changes to the craft tree. Ensuring the tree gets cached in
        /// every game "fixes" things at a surface level, but not the root cause.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(EscapePod), nameof(EscapePod.Start))]
        private static void ForceCraftTreeCalculation()
        {
            // Force the game and Nautilus to cache all types of craft tree touched by this mod.
            CraftTree.GetTree(CraftTree.Type.Fabricator);
            CraftTree.GetTree(CraftTree.Type.Workbench);
        }
    }
}