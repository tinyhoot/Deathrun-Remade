using System.Collections.Generic;
using System.Reflection.Emit;
using DeathrunRemade.Handlers;
using DeathrunRemade.Items;
using HarmonyLib;

namespace DeathrunRemade.Patches
{
    [HarmonyPatch]
    internal class CompassPatcher
    {
        private static bool _compassHasInitialised;
        
        /// <summary>
        /// Ensure that FilterChip is also counted as a compass.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Equipment), nameof(Equipment.GetCount))]
        private static void CountFilterChip(ref Equipment __instance, TechType techType, ref int __result)
        {
            if (!techType.Equals(TechType.Compass) || __result > 0)
                return;
            
            __result = __instance.equippedCount.GetOrDefault(FilterChip.TechType, 0);
        }

        /// <summary>
        /// Replace the compass' depth class with our custom solution.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(uGUI_DepthCompass), nameof(uGUI_DepthCompass.Initialize))]
        private static void SwapCrushDepthUpdate(ref uGUI_DepthCompass __instance)
        {
            // Do not do this before the compass has set itself up or after we have already done it once.
            if (!__instance._initialized || _compassHasInitialised)
                return;
            
            // Disconnect the compass from vanilla depth class.
            Player.main.depthClass.changedEvent.RemoveHandler(__instance.gameObject);
            // Instead, connect it to our custom implementation.
            CrushDepthHandler.CompassDepthClassOverride.changedEvent.AddHandler(__instance, __instance.OnDepthClassChanged);
            _compassHasInitialised = true;
        }

        /// <summary>
        /// The compass overrides all player depth classes with DepthClass.Safe. Stop it from doing that.
        /// </summary>
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(uGUI_DepthCompass), nameof(uGUI_DepthCompass.OnDepthClassChanged))]
        private static IEnumerable<CodeInstruction> RemoveDepthClassOverride(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions);
            matcher.MatchForward(false,
                // Advance to where the constant for DepthClass.Safe is loaded and saved to the variable.
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(OpCodes.Stloc_0))
                // Remove the overriding code.
                .SetInstructionAndAdvance(new CodeInstruction(OpCodes.Nop))
                .SetInstruction(new CodeInstruction(OpCodes.Nop));
            return matcher.InstructionEnumeration();
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.Update))]
        private static void UpdateCompass(Player __instance)
        {
            CrushDepthHandler.UpdateCompassDepthClass(__instance);
        }
    }
}