using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Enums;
using HarmonyLib;
using UnityEngine;

namespace DeathrunRemade.Patches
{
    [HarmonyPatch]
    internal class AggressionPatcher
    {
        private const float MoreAggressionTime = 2400f;
        private const float MaxAggressionTime = 4800f;
        
        /// <summary>
        /// Patch the method to use a search radius of our choice rather than a hardcoded value.
        /// </summary>
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(AggressiveWhenSeeTarget), nameof(AggressiveWhenSeeTarget.GetAggressionTarget))]
        private static IEnumerable<CodeInstruction> ExpandSearchRadius(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions);
            matcher.MatchForward(true,
                    new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "maxSearchRings"))
                .Advance(1)
                .Insert(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(AggressionPatcher), nameof(GetSearchRings)))
                );
            return matcher.InstructionEnumeration();
        }

        /// <summary>
        /// Mark the player as a valid target more often.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(AggressiveWhenSeeTarget), nameof(AggressiveWhenSeeTarget.IsTargetValid), typeof(GameObject))]
        private static void PatchValidTarget(AggressiveWhenSeeTarget __instance, ref bool __result, GameObject target)
        {
            // Never change a target that is already valid.
            if (__result)
                return;
            
            // Don't patch crash fish, as those are more dangerous when in hiding until the player is close.
            if (CraftData.GetTechType(__instance.gameObject) == TechType.Crash)
                return;
            
            // Don't change anything if the difficulty is set too low.
            Difficulty4 difficulty = SaveData.Main.Config.CreatureAggression;
            if (difficulty == Difficulty4.Normal || difficulty == Difficulty4.Hard)
                return;
            
            // Don't change anything if we're still early in the game.
            if (DayNightCycle.main.timePassedAsFloat < MoreAggressionTime)
                return;

            // Allow for some extra distance, but not too much.
            float dist = Vector3.Distance(target.transform.position, __instance.transform.position);
            if (dist > __instance.maxRangeScalar * 4)
                return;
            
            // Only change results for players and vehicles.
            Player player = Player.main;
            Vehicle vehicle = target.GetComponent<Vehicle>();
            if (vehicle is null && target != player.gameObject)
                return;
            
            // If the target is the player, don't attack them on land.
            if (player.precursorOutOfWater || player.GetDepth() <= 5)
                return;
            
            // If the target is a vehicle, only attack it if it is currently being piloted by the player.
            if (vehicle != null && (vehicle.precursorOutOfWater || vehicle != player.GetVehicle()))
                return;
            
            // On Deathrun, do a simple raycast to simulate 360 degree vision.
            if (difficulty == Difficulty4.Deathrun)
                __result = !Physics.Linecast(__instance.transform.position, target.transform.position, Voxeland.GetTerrainLayerMask());

            // On Kharaa, the player can always be seen, even through terrain.
            if (difficulty == Difficulty4.Kharaa)
                __result = true;
        }

        /// <summary>
        /// Increase the likelihood of the player being chosen as a valid target.
        /// </summary>
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(EcoRegion), nameof(EcoRegion.FindNearestTarget))]
        [HarmonyPatch(typeof(EcoRegion), nameof(EcoRegion.FindNearestTargetSqr))]
        private static IEnumerable<CodeInstruction> PrioritisePlayer(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions);
            matcher.MatchForward(true, 
                    new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "get_sqrMagnitude"),
                    new CodeMatch(OpCodes.Stloc_S))
                .Advance(1)
                .Insert(
                    new CodeInstruction(OpCodes.Ldloc_3),
                    new CodeInstruction(OpCodes.Ldloc_S, 4),
                    new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(AggressionPatcher), nameof(AdjustTargetDesirability))),
                    new CodeInstruction(OpCodes.Stloc_S, 4)
                );
            return matcher.InstructionEnumeration();
        }

        /// <summary>
        /// Make creatures more aggressive, letting them attack and search for targets more often.
        /// </summary>
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(MoveTowardsTarget), nameof(MoveTowardsTarget.UpdateCurrentTarget))]
        private static IEnumerable<CodeInstruction> IncreaseAggression(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            CodeMatcher matcher = new CodeMatcher(instructions);
            // We need a new local variable for this, so make sure it gets set up.
            generator.DeclareLocal(typeof(int));
            matcher.MatchForward(false,
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld),
                    new CodeMatch(OpCodes.Ldfld),
                    new CodeMatch(OpCodes.Ldfld))
                .SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_1))
                .SetInstructionAndAdvance(new CodeInstruction(OpCodes.Stloc_1))
                .SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldarg_0))
                // Load local variable 1 as *ref* so we can manipulate it directly.
                .SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldloca, 1))
                .Insert(new CodeInstruction(OpCodes.Call,
                    AccessTools.DeclaredMethod(typeof(AggressionPatcher), nameof(AdjustTargetAggression))))
                // Make use of the previously set-up local variable to replace the constant.
                .MatchForward(false, new CodeMatch(OpCodes.Ldc_I4_1))
                .SetInstruction(new CodeInstruction(OpCodes.Ldloc_1));
            return matcher.InstructionEnumeration();
        }

        /// <summary>
        /// Increase aggression level and radius for the current creature.
        /// </summary>
        /// <param name="instance">The component on the creature choosing what to do.</param>
        /// <param name="searchRadius">The maxSearchRadius for a follow-up method to use.</param>
        /// <returns>The creature's aggression level.</returns>
        private static float AdjustTargetAggression(MoveTowardsTarget instance, ref int searchRadius)
        {
            float aggression = instance.creature.Aggression.Value;
            
            // No changes for the crash fish.
            if (CraftData.GetTechType(instance.gameObject) == TechType.Crash)
                return aggression;

            // No changes when the player is on land.
            if (!Player.main.IsUnderwater()
                || (Player.main.GetVehicle() != null && Player.main.GetVehicle().precursorOutOfWater))
                return aggression;

            Difficulty4 difficulty = SaveData.Main.Config.CreatureAggression;
            float time = DayNightCycle.main.timePassedAsFloat;
            // Adjust aggression based on difficulty and time passed.
            if (difficulty == Difficulty4.Hard && time > MoreAggressionTime)
            {
                searchRadius = 3;
                return aggression * 2f;
            }
            if (difficulty == Difficulty4.Deathrun || difficulty == Difficulty4.Kharaa)
            {
                if (time > MaxAggressionTime)
                {
                    searchRadius = 6;
                    return aggression * 4f;
                }
                if (time > MoreAggressionTime)
                {
                    searchRadius = 3;
                    return aggression * 2f;
                }
            }

            return aggression;
        }

        /// <summary>
        /// Increase the likelihood of the player being chosen as a valid target.
        /// </summary>
        private static float AdjustTargetDesirability(IEcoTarget target, float sqrMagnitude)
        {
            Player player = Player.main;
            Vehicle vehicle = target.GetGameObject().GetComponent<Vehicle>();
            // Don't do anything if the target is not the player or the player's vehicle.
            if (vehicle is null && target.GetGameObject() != player.gameObject)
                return sqrMagnitude;

            // If the target is the player, ensure they're a good target right now.
            if (target.GetGameObject() == player.gameObject && !player.IsUnderwater())
                return sqrMagnitude;
            
            // If the target is a vehicle, only attack it if it is currently being piloted by the player.
            if (vehicle != null && (vehicle.precursorOutOfWater || vehicle != player.GetVehicle()))
                return sqrMagnitude;
            
            // No increase if the player is holding a fish, i.e. ready to feed the predator.
            if (target.GetGameObject() == player.gameObject)
            {
                Pickupable heldItem = Inventory.main.GetHeld();
                if (heldItem && heldItem.gameObject.GetComponent<Creature>())
                    return sqrMagnitude;
            }

            if (DayNightCycle.main.timePassedAsFloat > MaxAggressionTime)
                return SaveData.Main.Config.CreatureAggression switch
                {
                    Difficulty4.Hard => sqrMagnitude / 3,
                    Difficulty4.Deathrun => 1,  // Extremely attractive.
                    Difficulty4.Kharaa => 1,
                    _ => sqrMagnitude
                };
            
            if (DayNightCycle.main.timePassedAsFloat > MoreAggressionTime)
                return SaveData.Main.Config.CreatureAggression switch
                {
                    Difficulty4.Hard => sqrMagnitude / 2,
                    Difficulty4.Deathrun => sqrMagnitude / 4,
                    Difficulty4.Kharaa => sqrMagnitude / 4,
                    _ => sqrMagnitude
                };

            return sqrMagnitude;
        }

        /// <summary>
        /// Get the number of search rings for the passed time and difficulty.
        /// </summary>
        public static int GetSearchRings(int maxSearchRings, AggressiveWhenSeeTarget instance)
        {
            // Don't patch crash fish, as those are more dangerous when in hiding until the player is close.
            if (CraftData.GetTechType(instance.gameObject) == TechType.Crash)
                return maxSearchRings;

            // Don't increase search range while the player is on land.
            if (Player.main.GetDepth() <= 5f)
                return maxSearchRings;
            
            float time = DayNightCycle.main.timePassedAsFloat;
            // Start scaling up the search distance after enough time has passed.
            if (time > MaxAggressionTime)
                return SaveData.Main.Config.CreatureAggression switch
                {
                    Difficulty4.Hard => maxSearchRings + 1,
                    Difficulty4.Deathrun => maxSearchRings * 3,
                    Difficulty4.Kharaa => maxSearchRings * 3,
                    _ => maxSearchRings
                };
            if (time > MoreAggressionTime)
                return SaveData.Main.Config.CreatureAggression switch
                {
                    Difficulty4.Hard => maxSearchRings + 1,
                    Difficulty4.Deathrun => maxSearchRings * 2,
                    Difficulty4.Kharaa => maxSearchRings * 2,
                    _ => maxSearchRings
                };
            
            return maxSearchRings;
        }
    }
}