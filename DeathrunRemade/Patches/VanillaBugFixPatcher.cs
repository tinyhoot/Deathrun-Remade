using System.Collections.Generic;
using System.Reflection.Emit;
using DeathrunRemade.Objects.Attributes;
using DeathrunRemade.Objects.Enums;
using HarmonyLib;

namespace DeathrunRemade.Patches
{
    /// <summary>
    /// Fixes for vanilla bugs you might not come across normally but become very apparent through this mod.
    /// </summary>
    [HarmonyPatch]
    [PatchCategory(ApplyPatch.Always)]
    internal static class VanillaBugFixPatcher
    {
        /// <summary>
        /// The pipe placement uses a raycast which does not ignore trigger collisions. This inadvertently leads to
        /// it being impossible to draw a line of pipes down into the jellyshroom caves, as there is a trigger at the
        /// entrance.
        /// This patch simply makes the raycast ignore triggers. This *could* cause some unintended wonkiness somewhere
        /// but in my testing I haven't found any and this patch has done as intended.
        /// </summary>
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(OxygenPipe), nameof(OxygenPipe.IsInSight))]
        private static IEnumerable<CodeInstruction> FixPipeBreakingAtCave(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions);

            // Find the raycast.
            matcher.MatchForward(true,
                    new CodeMatch(OpCodes.Add),
                    // The raycast uses layermask -5...
                    new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)-5),
                    // ...and passes the enum QueryTriggerInteraction.Global (-> 0).
                    new CodeMatch(OpCodes.Ldc_I4_0))
                .ThrowIfInvalid("Failed to find pipe placement raycast!")
                // Set QueryTriggerInteraction to Ignore instead of Global.
                .SetOpcodeAndAdvance(OpCodes.Ldc_I4_1);

            return matcher.InstructionEnumeration();
        }
    }
}