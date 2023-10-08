using System.Collections.Generic;
using System.Reflection.Emit;
using DeathrunRemade.Handlers;
using DeathrunRemade.Items;
using DeathrunRemade.Objects;
using HarmonyLib;
using UnityEngine;

namespace DeathrunRemade.Patches
{
    [HarmonyPatch]
    internal class CompassPatcher
    {
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
        /// The compass is meant to change to red when the player exceeds their crush depth. This is vanilla, but needs
        /// to be re-enabled with two patches.
        /// For the compass to properly show the depth, the player's depth class needs to be set correctly.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.GetDepthClass))]
        private static void SetDepthClass(ref Ocean.DepthClass __result)
        {
            if (SaveData.Main is null || Inventory.main is null || Player.main.IsInsideSubOrVehicle())
                return;
            TechType suit = Inventory.main.equipment.GetTechTypeInSlot("Body");
            int crushDepth = Mathf.FloorToInt(CrushDepthHandler.GetCrushDepth(suit, SaveData.Main.Config));
            __result = Player.main.GetDepth() >= crushDepth ? Ocean.DepthClass.Crush : Ocean.DepthClass.Safe;
        }

        /// <summary>
        /// The game overrides all player depth classes with DepthClass.Safe. Stop it from doing that.
        /// </summary>
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(uGUI_DepthCompass), nameof(uGUI_DepthCompass.OnDepthClassChanged))]
        private static IEnumerable<CodeInstruction> RemoveDepthClassOverride(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions);
            matcher.MatchForward(false, 
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(OpCodes.Stloc_0))
                // Remove the overriding code.
                .SetInstructionAndAdvance(new CodeInstruction(OpCodes.Nop))
                .SetInstruction(new CodeInstruction(OpCodes.Nop));
            return matcher.InstructionEnumeration();
        }
    }
}