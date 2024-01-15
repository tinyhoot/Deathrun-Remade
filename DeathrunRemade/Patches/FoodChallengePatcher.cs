using System.Collections.Generic;
using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Enums;
using HarmonyLib;
using UnityEngine;

namespace DeathrunRemade.Patches
{
    [HarmonyPatch]
    internal class FoodChallengePatcher
    {
        private static readonly HashSet<TechType> Fish = new HashSet<TechType>
        {
            TechType.Bladderfish, TechType.CookedBladderfish, TechType.CuredBladderfish,
            TechType.Boomerang, TechType.CookedBoomerang, TechType.CuredBoomerang,
            TechType.Eyeye, TechType.CookedEyeye, TechType.CuredEyeye,
            TechType.GarryFish, TechType.CookedGarryFish, TechType.CuredGarryFish,
            TechType.HoleFish, TechType.CookedHoleFish, TechType.CuredHoleFish,
            TechType.Hoopfish, TechType.CookedHoopfish, TechType.CuredHoopfish,
            TechType.Hoverfish, TechType.CookedHoverfish, TechType.CuredHoverfish,
            TechType.LavaBoomerang, TechType.CookedLavaBoomerang, TechType.CuredLavaBoomerang,
            TechType.LavaEyeye, TechType.CookedLavaEyeye, TechType.CuredLavaEyeye,
            TechType.Oculus, TechType.CookedOculus, TechType.CuredOculus,
            TechType.Peeper, TechType.CookedPeeper, TechType.CuredPeeper,
            TechType.Reginald, TechType.CookedReginald, TechType.CuredReginald,
            TechType.Spadefish, TechType.CookedSpadefish, TechType.CuredSpadefish,
            TechType.Spinefish, TechType.CookedSpinefish, TechType.CuredSpinefish,
        };

        private static readonly HashSet<TechType> Plants = new HashSet<TechType>
        {
            TechType.BulboTree, TechType.HangingFruit, TechType.HangingFruitTree, TechType.Melon, TechType.MelonPlant,
            TechType.SmallMelon, TechType.PurpleVegetable, TechType.PurpleVegetablePlant
        };

        private static readonly HashSet<TechType> Water = new HashSet<TechType>
        {
            TechType.DisinfectedWater, TechType.FilteredWater, TechType.BigFilteredWater,
            TechType.Coffee, TechType.WaterFiltrationSuitWater
        };

        /// <summary>
        /// Food challenge - Enforce dietary restrictions by making food really punishing to eat.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Eatable), nameof(Eatable.GetFoodValue))]
        private static void ChangeFoodValues(Eatable __instance, ref float __result)
        {
            const float malus = -25f;
            DietPreference diet = SaveData.Main.Config.FoodChallenge;
            TechType techType = CraftData.GetTechType(__instance.gameObject);
            // No changes to omnivore diets or water.
            if (diet == DietPreference.Omnivore || Water.Contains(techType))
                return;
            
            if (diet == DietPreference.Pescatarian && !Fish.Contains(techType))
            {
                __result = malus;
                return;
            }

            if (diet == DietPreference.Vegetarian && Fish.Contains(techType))
            {
                __result = malus;
                return;
            }
            
            if (diet == DietPreference.Vegan
                && (Fish.Contains(techType) || techType == TechType.NutrientBlock || techType == TechType.Snack1))
            {
                __result = malus;
                return;
            }
        }

        /// <summary>
        /// Island food challenge - Prevent knifing plants on the island.
        /// We patch this instead of IsValidTarget to not make it impossible to remove those resources.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Knife), nameof(Knife.GiveResourceOnDamage))]
        private static bool CancelKnifeHarvest(GameObject target)
        {
            return AllowPickup(target);
        }
        
        /// <summary>
        /// Island food challenge - Prevent picking things up by hand, and the prompt from appearing at all. Also
        /// cancels picking up with the exosuit's claw arms.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PickPrefab), nameof(PickPrefab.OnHandHover))]
        [HarmonyPatch(typeof(PickPrefab), nameof(PickPrefab.OnHandClick))]
        private static bool CancelHandPickup(PickPrefab __instance)
        {
            return AllowPickup(__instance.gameObject);
        }
        
        /// <summary>
        /// Island food challenge - Prevent picking things up with the propulsion cannon.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PropulsionCannon), nameof(PropulsionCannon.ValidateObject))]
        private static bool CancelPropulsionPickup(GameObject go, ref bool __result)
        {
            __result = AllowPickup(go);
            return __result;
        }
        
        /// <summary>
        /// Island food challenge - Prevent shooting things with the repulsion cannon.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(RepulsionCannon), nameof(RepulsionCannon.ShootObject))]
        private static bool CancelRepulsionShoot(Rigidbody rb)
        {
            return AllowPickup(rb.gameObject);
        }

        /// <summary>
        /// Check whether the player is allowed to pick up food from the island.
        /// </summary>
        private static bool AllowPickup(GameObject gameObject)
        {
            RelativeToExplosion difficulty = SaveData.Main.Config.IslandFoodChallenge;
            if (difficulty == RelativeToExplosion.Always)
                return true;
            // These changes only apply to surface plants, so allow any other food. Surface bases are also alright.
            if (Ocean.GetDepthOf(gameObject) > 1f || !Plants.Contains(CraftData.GetTechType(gameObject)) || Player.main.IsInsideWalkable())
                return true;
            if (difficulty == RelativeToExplosion.Never)
                return false;

            // The Aurora has not exploded yet, so we can rule out this specific case.
            if (difficulty == RelativeToExplosion.After
                && (CrashedShipExploder.main == null || !CrashedShipExploder.main.IsExploded()))
                return false;

            // Both remaining config options are covered with this check.
            return RadiationPatcher.IsSurfaceIrradiated();
        }
    }
}