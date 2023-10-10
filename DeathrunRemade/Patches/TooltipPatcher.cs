using System.Collections.Generic;
using System.Reflection.Emit;
using DeathrunRemade.Handlers;
using HarmonyLib;
using HootLib;

namespace DeathrunRemade.Patches
{
    [HarmonyPatch]
    internal static class TooltipPatcher
    {
        /// <summary>
        /// Insert custom tooltips in item descriptions.
        /// </summary>
        [HarmonyDebug]
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(TooltipFactory), nameof(TooltipFactory.ItemCommons))]
        private static IEnumerable<CodeInstruction> AddTooltips(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions);
            matcher
                // // Find the place just after where the first aid kit's special tooltip gets written.
                // .MatchForward(true, new CodeMatch(OpCodes.Ldc_I4, (int)TechType.FirstAidKit))
                // .MatchForward(true,
                //     new CodeMatch(i =>
                //         i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name.Equals("WriteDescription")))
                // .Advance(1)
                // // Add our own extra first aid kit tooltip here.
                // .Insert(
                //     new CodeInstruction(OpCodes.Ldarg_0),
                //     CodeInstruction.Call(typeof(SurvivalPatcher), nameof(WriteFirstAidKitTooltip)))
                
                // Next, advance to just after water values were written, right before any oxygen values.
                .MatchForward(false,
                    new CodeMatch(OpCodes.Ldarg_2),
                    new CodeMatch(),
                    HootTranspiler.VariableMatch(OpCodes.Stloc_S, typeof(IOxygenSource)),
                    HootTranspiler.VariableMatch(OpCodes.Ldloc_S, typeof(IOxygenSource)))
                // Add our own instructions as the new target for the end of all the previous if statements.
                .InsertAndPreserveLabels(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldloc_3),
                    CodeInstruction.Call(typeof(TooltipHandler), nameof(TooltipHandler.WriteNitrogenTooltip)), 
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
            // The first boolean variable is the flag for whether this item is a battery.
            matcher
                .MatchForward(false, 
                    new CodeMatch(OpCodes.Ldc_I4_0),
                    HootTranspiler.VariableMatch(OpCodes.Stloc_S, typeof(bool)))
                // Always set the value to true to bypass the check.
                .SetInstruction(new CodeInstruction(OpCodes.Ldc_I4_1));
            
            return matcher.InstructionEnumeration();
        }
    }
}