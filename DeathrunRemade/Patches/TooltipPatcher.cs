using System.Collections.Generic;
using System.Reflection.Emit;
using DeathrunRemade.Handlers;
using DeathrunRemade.Objects.Attributes;
using DeathrunRemade.Objects.Enums;
using HarmonyLib;
using HootLib;

namespace DeathrunRemade.Patches
{
    [HarmonyPatch]
    [PatchCategory(ApplyPatch.Always)]
    internal static class TooltipPatcher
    {
        /// <summary>
        /// Insert custom tooltips in item descriptions.
        /// This method is very long and consists of many successive if statements checking whether X kind of tooltip
        /// should be added to the item. This transpiler inserts our own extra tooltips roughly in the middle, after
        /// food and water values.
        /// </summary>
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(TooltipFactory), nameof(TooltipFactory.ItemCommons))]
        private static IEnumerable<CodeInstruction> AddTooltips(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions);
            // Advance to just after water values were written, right before any oxygen values. Multiple if statements
            // from the food and water blocks end here. We match on the first time the local variable for oxygen
            // comes into play as it is populated with the component from the GameObject of the item.
            matcher
                .MatchForward(false,
                    // The GameObject of the item in the inventory.
                    new CodeMatch(OpCodes.Ldarg_2),
                    new CodeMatch(OpCodes.Callvirt), // A Unity GetComponent() call.
                    // Matches on the instruction storing the component in a local variable.
                    HootTranspiler.VariableMatch(OpCodes.Stloc_S, typeof(IOxygenSource)),
                    HootTranspiler.VariableMatch(OpCodes.Ldloc_S, typeof(IOxygenSource)))
                // We are at the dividing line between food/water and oxygen. Insert our own instructions here and move
                // the oxygen down a little. To do that, we need to preserve the jump labels in-place so that the if
                // statements of food and water point to *here* rather than to oxygen.
                .InsertAndPreserveLabels(
                    // Load the StringBuilder for the description and the Eatable component.
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldloc_3),
                    CodeInstruction.Call(typeof(TooltipHandler), nameof(TooltipHandler.WriteNitrogenTooltip)), 
                    // Load the StringBuilder for the description and the TechType of the item.
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    CodeInstruction.Call(typeof(TooltipHandler), nameof(TooltipHandler.WriteCrushDepthTooltip)));
            
            return matcher.InstructionEnumeration();
        }

        /// <summary>
        /// Subnautica usually does not show descriptive tooltips for batteries, which is bad since this mod introduces
        /// more differences between them. This patch re-adds the tooltip text for all batteries.
        /// </summary>
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(TooltipFactory), nameof(TooltipFactory.ItemCommons))]
        private static IEnumerable<CodeInstruction> RestoreBatteryTooltips(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions);
            matcher
                // The first time a bool is set to false in the code is just after the tooltip for battery charge has
                // been added. Find and override this instruction to trick the game into thinking it isn't actually
                // dealing with a battery and always add the full description.
                .MatchForward(false,
                    new CodeMatch(OpCodes.Ldc_I4_0),
                    HootTranspiler.VariableMatch(OpCodes.Stloc_S, typeof(bool)))
                // Always set the value to true to bypass the check.
                .SetInstruction(new CodeInstruction(OpCodes.Ldc_I4_1));
            
            return matcher.InstructionEnumeration();
        }
    }
}