using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using DeathrunRemade.Items;
using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Attributes;
using DeathrunRemade.Objects.Enums;
using HarmonyLib;
using UnityEngine;

namespace DeathrunRemade.Patches
{
    [HarmonyPatch]
    [PatchCategory(ApplyPatch.Config)]
    internal class PacifistPatcher
    {
        /// <summary>
        /// Pacifist challenge - Prevent the knife from dealing damage to any living creatures.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Knife), nameof(Knife.IsValidTarget))]
        private static void CancelKnifeDamage(LiveMixin liveMixin, ref bool __result)
        {
            // No changes necessary.
            if (!__result || !SaveData.Main.Config.PacifistChallenge || liveMixin == null)
                return;
            // If the target is a creature, deny.
            Creature creature = liveMixin.GetComponent<Creature>();
            if (creature != null) {
                // Since since the player can't damage creatures to get their scales,
                // we need to manually give them the scales when the appropriate conditions are met.
                TechType creatureType = CraftData.GetTechType(creature.gameObject);
                bool crushDepthDropsEnabled = SaveData.Main.Config.PersonalCrushDepth > Difficulty3.Hard;
                bool specialAirTanksDropsEnabled = SaveData.Main.Config.SpecialAirTanks;
                TechType? dropType = null;

                switch (creatureType) {
                    case TechType.SpineEel:
                        if (crushDepthDropsEnabled) dropType = SpineEelScale.s_TechType;
                        break;
                    case TechType.LavaLizard:
                        if (crushDepthDropsEnabled) dropType = LavaLizardScale.s_TechType;
                        break;
                    case TechType.LavaLarva:
                        if (specialAirTanksDropsEnabled) dropType = ThermophileSample.s_TechType;
                        break;
                    default:
                        break;
                }

                if (dropType != null)
                    CraftData.AddToInventory((TechType)dropType,1,false,false);

                // Now, actually cancel the knife damage.
                __result = false;
            }
        }
        
        /// <summary>
        /// Pacifist challenge - Prevent shooting fish with the repulsion cannon.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(RepulsionCannon), nameof(RepulsionCannon.ShootObject))]
        private static bool CancelRepulsionShoot(Rigidbody rb)
        {
            return rb.GetComponent<Creature>() == null;
        }

        /// <summary>
        /// Pacifist challenge - Prevent any harm done to fish by ramming them with vehicles.
        ///
        /// Approach: When things collide harshly, this method runs. There's an if statement buried deep within that
        /// checks whether the collision target has a valid health component and hasn't taken damage from this particular
        /// collision in the last few frames. We add an extra condition to this to prevent any damage to creatures.
        /// </summary>
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(DealDamageOnImpact), nameof(DealDamageOnImpact.OnCollisionEnter))]
        private static IEnumerable<CodeInstruction> CancelCollisionDamage(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions);

            // Find the if statement in this huge method. Incidentally, this checks the LiveMixin for null.
            matcher.MatchForward(true, 
                new CodeMatch(OpCodes.Ldloc_1),
                new CodeMatch(OpCodes.Ldnull),
                new CodeMatch(OpCodes.Call),
                new CodeMatch(OpCodes.Brfalse));
            // Copy the label to jump to for our own extra condition.
            var label = matcher.Operand;
            matcher.Advance(1);
            // Insert an extra check for making this LiveMixin take damage - no creatures.
            matcher.Insert(
                new CodeInstruction(OpCodes.Ldloc_1),
                Transpilers.EmitDelegate<Func<LiveMixin, bool>>(live => live.GetComponent<Creature>() == null),
                new CodeInstruction(OpCodes.Brfalse, label));
            return matcher.InstructionEnumeration();
        }

        /// <summary>
        /// Pacifist challenge - Prevent any harm done to fish by the seamoth electrical defense module.
        /// They still won't like it, but it will no longer be a killing tool.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ElectricalDefense), nameof(ElectricalDefense.Start))]
        private static void CancelElectricalDefenseDamage(ref ElectricalDefense __instance)
        {
            // Retain a very small amount of damage to trigger fleeing behaviours.
            __instance.damage = 1f;
            __instance.chargeDamage = 0f;
        }
        
        /// <summary>
        /// Pacifist challenge - In tandem with the patch above, increase the fear factor of the electrical defense
        /// module to compensate for the lower damage.
        /// </summary>
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(FleeOnDamage), nameof(FleeOnDamage.OnTakeDamage))]
        private static IEnumerable<CodeInstruction> ImproveElectricalDefenseFear(IEnumerable<CodeInstruction> instructions)
        {
            // Swoop in, pluck out the x35 constant multiplier, increase it to 100x. Even with the damage patch this is
            // still enough to deter a reaper in two bursts.
            CodeMatcher matcher = new CodeMatcher(instructions);
            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_R4, 35f))
                .SetOperandAndAdvance(100f);
            return matcher.InstructionEnumeration();
        }
        
        /// <summary>
        /// Pacifist challenge - Prevent any harm done to fish by exosuit grabby hands. Very similar approach to
        /// the collision damage patch above.
        /// </summary>
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(ExosuitClawArm), nameof(ExosuitClawArm.OnHit))]
        private static IEnumerable<CodeInstruction> CancelExoClawDamage(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions);
            
            // Find the if statement.
            matcher.MatchForward(false, 
                new CodeMatch(OpCodes.Brfalse),
                new CodeMatch(OpCodes.Ldloc_S),
                new CodeMatch(OpCodes.Ldc_R4, 50f));
            // Copy the label to jump to for our own extra condition.
            var label = matcher.Operand;
            matcher.Advance(1);
            var localBuilder = matcher.Operand;
            // Insert an extra check for making this LiveMixin take damage - no creatures.
            matcher.Insert(
                new CodeInstruction(OpCodes.Ldloc_S, localBuilder),
                Transpilers.EmitDelegate<Func<LiveMixin, bool>>(live => live.gameObject.FindAncestor<Creature>() == null),
                new CodeInstruction(OpCodes.Brfalse, label));
            return matcher.InstructionEnumeration();
        }
        
        /// <summary>
        /// Pacifist challenge - Prevent any harm done to fish by the exosuit drill arm. Essentially analogous to
        /// the grabby hands patch above.
        /// </summary>
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(ExosuitDrillArm), nameof(ExosuitDrillArm.OnHit))]
        private static IEnumerable<CodeInstruction> CancelExoDrillDamage(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions);
            
            // Find the if statement.
            matcher.MatchForward(false,
                new CodeMatch(i => i.opcode == OpCodes.Ldloc_S && ((LocalBuilder)i.operand).LocalType == typeof(LiveMixin)),
                new CodeMatch(OpCodes.Call),
                new CodeMatch(OpCodes.Brfalse));
            // Copy the variable builder for later.
            var localBuilder = matcher.Operand;
            matcher.Advance(2);
            // Copy the label to jump to for our own extra condition.
            var label = matcher.Operand;
            matcher.Advance(1);
            // Insert an extra check for making this LiveMixin take damage - no creatures.
            matcher.Insert(
                new CodeInstruction(OpCodes.Ldloc_S, localBuilder),
                Transpilers.EmitDelegate<Func<LiveMixin, bool>>(live => live.gameObject.FindAncestor<Creature>() == null),
                new CodeInstruction(OpCodes.Brfalse, label));
            return matcher.InstructionEnumeration();
        }
    }
}