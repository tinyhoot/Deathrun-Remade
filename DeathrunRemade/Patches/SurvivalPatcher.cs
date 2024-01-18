using System.Collections.Generic;
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
            { TechType.Boomerang, -50f },
            { TechType.LavaBoomerang, -200f },
        };

        /// <summary>
        /// Adds nitrogen-removing properties to certain food items.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Survival), nameof(Survival.Eat))]
        private static void RemoveNitrogenWithFood(GameObject useObj)
        {
            if (useObj == null)
                return;
            Eatable eatable = useObj.GetComponent<Eatable>();
            if (eatable == null)
                return;

            // Don't consider any food which doesn't even interact with nitrogen.
            if (!TryGetNitrogenValue(eatable, out float nitrogen))
                return;
            
            if (nitrogen >= 0)
                Player.main.GetComponent<NitrogenHandler>().AddNitrogen(nitrogen);
            else
                Player.main.GetComponent<NitrogenHandler>().RemoveNitrogen(-nitrogen);
        }

        /// <summary>
        /// Reduce nitrogen on using a first aid kit.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Survival), nameof(Survival.Use))]
        private static void RemoveNitrogenWithFirstAidKits(GameObject useObj, bool __result)
        {
            // Do not give away free nitrogen heals if the first aid kit cannot be consumed.
            if (useObj == null || __result is false)
                return;
            TechType techType = CraftData.GetTechType(useObj);
            if (techType != TechType.FirstAidKit)
                return;

            // Purge around half of the current nitrogen level.
            float reduction = (SaveData.Main.Nitrogen.safeDepth + SaveData.Main.Nitrogen.nitrogen) / 2f;
            Player.main.GetComponent<NitrogenHandler>().RemoveNitrogen(reduction);
        }

        /// <summary>
        /// Make it possible to use a first aid kit from a quick slot.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(QuickSlots), nameof(QuickSlots.Drop))]
        private static bool UseFirstAidKitFromQuickSlot(QuickSlots __instance)
        {
            if (__instance._heldItem == null || __instance._heldItem.techType != TechType.FirstAidKit)
                return true;

            // Try to use the first aid kit and abort if that does not work for some reason, such as full health.
            Survival survival = Player.main.GetComponent<Survival>();
            if (survival == null || !survival.Use(__instance._heldItem.item.gameObject))
                return false;
            
            // This is duplicated from the original method but I was not about to transpile a whole mess into that for
            // just one type of item. Refills the quickslot if we have more first aid kits in the inventory.
            __instance.refillTechType = TechType.FirstAidKit;
            __instance.refillSlot = __instance.GetSlotByItem(__instance._heldItem);
            __instance.desiredSlot = __instance.refillSlot;
            // Actually consume the item.
            Object.Destroy(__instance._heldItem.item.gameObject);
            return false;
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
        public static bool TryGetNitrogenValue(Eatable eatable, out float nitrogen)
        {
            nitrogen = 0f;
            if (eatable == null)
                return false;
            TechType techType = CraftData.GetTechType(eatable.gameObject);
            if (!NitrogenFood.TryGetValue(techType, out nitrogen))
                return false;
            // Adjust for how rotten the eatable is.
            // Removed for now since this does not seem to work properly for living fish and you eventually reach a
            // point where all boomerangs are "decomposed" and have no value at all.
            // nitrogen = DecomposeNitrogenValue(eatable, baseValue);
            return true;
        }
    }
}