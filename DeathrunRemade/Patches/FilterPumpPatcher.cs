using System.Collections.Generic;
using System.Reflection.Emit;
using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Enums;
using HarmonyLib;

namespace DeathrunRemade.Patches
{
    [HarmonyPatch]
    internal class FilterPumpPatcher
    {
        /// <summary>
        /// Change the extra conditions for deploying a filter pump so you can deploy it from the surface too.
        ///
        /// In all honesty a return-false prefix might be the better choice for this but those make it kinda hard to
        /// call back to base(), so transpiler it is.
        /// </summary>
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PipeSurfaceFloater), nameof(PipeSurfaceFloater.OnRightHandDown))]
        private static IEnumerable<CodeInstruction> AlwaysDeployPump(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions);
            // The first branching instruction occurs after the call to base.OnRightHandDown().
            matcher.MatchForward(false, new CodeMatch(OpCodes.Brfalse));
            matcher.Advance(1);
            // The second branch is the one we need, after checking whether the player is swimming.
            matcher.MatchForward(false, new CodeMatch(OpCodes.Brfalse));
            matcher.Advance(-1);
            // Replace the isSwimming check to allow deploying even when at the water surface.
            matcher.SetInstructionAndAdvance(CodeInstruction.Call(typeof(Player), nameof(Player.IsUnderwaterForSwimming)));
            // Skip the rest of the method by replacing the branch instruction with a return.
            matcher.SetOpcodeAndAdvance(OpCodes.Ret);

            return matcher.InstructionEnumeration();
        }
        
        /// <summary>
        /// Filterpump challenge - The pump no longer works in irradiated areas.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PipeSurfaceFloater), nameof(PipeSurfaceFloater.GetProvidesOxygen))]
        private static void CancelOxygenInRadiation(PipeSurfaceFloater __instance, ref bool __result)
        {
            Difficulty3 difficulty = SaveData.Main.Config.FilterPumpChallenge;
            if (difficulty == Difficulty3.Normal)
                return;
            
            // On all remaining difficulties, the pump never works inside the Aurora.
            if ((CrashedShipAmbientSound.main && CrashedShipAmbientSound.main.isPlayerInside)
                || (GeneratorRoomAmbientSound.main && GeneratorRoomAmbientSound.main.isPlayerInside))
            {
                __result = false;
                return;
            }
            
            // On this difficulty, cancel the pump while inside the post-explosion radiation radius.
            if (difficulty == Difficulty3.Deathrun && RadiationPatcher.IsInRadiationRadius(__instance.transform))
                __result = false;
        }
    }
}