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
            Player player = Player.main;
            // There is a different depth class system for vehicles, do not bother when the player is piloting one.
            if (SaveData.Main is null || Inventory.main == null || player.GetVehicle() != null)
                return;
            
            // The depth is always safe inside non-flooded bases.
            // IsLeaking() as opposed to IsUnderwater() allows for a red compass even while the player is still in
            // knee-deep water and not yet taking damage, which works well as a warning system.
            if (player.IsInBase() && !player.GetCurrentSub().IsLeaking())
            {
                __result = Ocean.DepthClass.Safe;
                return;
            }
            
            TechType suit = Inventory.main.equipment.GetTechTypeInSlot("Body");
            int crushDepth = Mathf.FloorToInt(CrushDepthHandler.GetCrushDepth(suit, SaveData.Main.Config));
            __result = player.GetDepth() >= crushDepth ? Ocean.DepthClass.Crush : Ocean.DepthClass.Safe;
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