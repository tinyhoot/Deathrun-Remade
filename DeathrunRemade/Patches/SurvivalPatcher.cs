using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using DeathrunRemade.Handlers;
using DeathrunRemade.Objects;
using HarmonyLib;
using UnityEngine;

namespace DeathrunRemade.Patches
{
    [HarmonyPatch]
    internal class SurvivalPatcher
    {
        // The worst things can get with severely rotten food.
        private const float MaxDecomposedNitrogenMalus = 25f;
        private static readonly Dictionary<TechType, float> NitrogenFood = new Dictionary<TechType, float>
        {
            { TechType.Boomerang, -100f },
            { TechType.LavaBoomerang, -300f },
        };

        /// <summary>
        /// Adds nitrogen-removing properties to certain food items.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Survival), nameof(Survival.Eat))]
        private static void RemoveNitrogenWithFood(GameObject useObj)
        {
            if (useObj is null)
                return;
            Eatable eatable = useObj.GetComponent<Eatable>();
            if (eatable is null)
                return;

            // Don't consider any food which doesn't even interact with nitrogen.
            if (!TryGetNitrogenValue(eatable, out float nitrogen))
                return;
            
            if (nitrogen >= 0)
                NitrogenHandler.Main.AddNitrogen(nitrogen);
            else
                NitrogenHandler.Main.RemoveNitrogen(-nitrogen);
        }

        /// <summary>
        /// Reduce nitrogen on using a first aid kit.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Survival), nameof(Survival.Use))]
        private static void RemoveNitrogenWithFirstAidKits(GameObject useObj)
        {
            if (useObj is null)
                return;
            TechType techType = CraftData.GetTechType(useObj);
            if (techType != TechType.FirstAidKit)
                return;

            // Purge around half of the current nitrogen level.
            float reduction = (SaveData.Main.Nitrogen.safeDepth + SaveData.Main.Nitrogen.nitrogen) / 2f;
            NitrogenHandler.Main.RemoveNitrogen(reduction);
        }

        /// <summary>
        /// Add tooltips about the nitrogen removal similar to food and water to the items that need it.
        /// </summary>
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(TooltipFactory), nameof(TooltipFactory.ItemCommons))]
        private static IEnumerable<CodeInstruction> AddNitrogenTooltip(IEnumerable<CodeInstruction> instructions)
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
                .MatchForward(true,
                    new CodeMatch(i =>
                        i.opcode == OpCodes.Stloc_S && ((LocalBuilder)i.operand).LocalType == typeof(IOxygenSource)),
                    new CodeMatch(i =>
                        i.opcode == OpCodes.Ldloc_S && ((LocalBuilder)i.operand).LocalType == typeof(IOxygenSource)))
                .Insert(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldloc_3),
                    CodeInstruction.Call(typeof(SurvivalPatcher), nameof(WriteNitrogenTooltip)));
            
            return matcher.InstructionEnumeration();
        }

        /// <summary>
        /// Reduce the nitrogen benefit of an Eatable as it decomposes.
        /// </summary>
        private static float DecomposeNitrogenValue(Eatable eatable, float nitrogen)
        {
            float malus = (DayNightCycle.main.timePassedAsFloat - eatable.timeDecayStart) * eatable.kDecayRate;
            // The whole way the game calculates rottenness is really weird and technically all fish start rotting
            // immediately on spawn, even while they're still swimming. Give the player some grace time here.
            malus = Mathf.Max(0f, malus - (eatable.kDecayRate * 5f * 60f));
            return Mathf.Min(nitrogen + malus, MaxDecomposedNitrogenMalus);
        }

        /// <summary>
        /// Try to get the nitrogen gained from eating something.
        /// </summary>
        private static bool TryGetNitrogenValue(Eatable eatable, out float nitrogen)
        {
            nitrogen = 0f;
            if (eatable is null)
                return false;
            TechType techType = CraftData.GetTechType(eatable.gameObject);
            if (!NitrogenFood.TryGetValue(techType, out float baseValue))
                return false;
            // Adjust for how rotten the eatable is.
            nitrogen = DecomposeNitrogenValue(eatable, baseValue);
            return true;
        }

        /// <summary>
        /// Write the tooltip displaying nitrogen values on an item in the inventory.
        /// </summary>
        private static void WriteNitrogenTooltip(StringBuilder sb, Eatable eatable)
        {
            if (!TryGetNitrogenValue(eatable, out float nitrogen))
                return;
            sb.Append($"\n<size=20><color=#DDDEDEFF>NITROGEN: {nitrogen:F0}</color></size>");
        }
    }
}