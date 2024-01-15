using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Enums;
using HarmonyLib;
using UnityEngine;

namespace DeathrunRemade.Patches
{
    [HarmonyPatch]
    internal class DamageTakenPatcher
    {
        /// <summary>
        /// Increase the damage taken by players and vehicles by a global multiplier.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(DamageSystem), nameof(DamageSystem.CalculateDamage))]
        private static void IncreaseDamageTaken(DamageType type, GameObject target, ref float __result)
        {
            // Only increase damage for the player and vehicles.
            if (target != Player.main.gameObject && target.GetComponent<Vehicle>() == null)
                return;
            
            // Two possible multipliers for damage, both are applied for different damage types.
            (float smaller, float bigger) = GetDamageMult(SaveData.Main.Config.DamageTaken);
            switch (type)
            {
                case DamageType.Normal:
                    // Oneshot protection if the player is wearing any kind of suit.
                    if (target == Player.main.gameObject && Inventory.main.equipment.GetTechTypeInSlot("Body") != TechType.None)
                    {
                        // Cap damage at 95.
                        __result = Mathf.Min(95f, __result * smaller);
                        break;
                    }
                    
                    // Reduce the multiplier for each armor plating module on the vehicle.
                    Vehicle vehicle = target.GetComponent<Vehicle>();
                    int modules = (vehicle == null) ? -1 : vehicle.modules.GetCount(TechType.VehicleArmorPlating);
                    if (modules > 0)
                    {
                        // Every module reduces the damage multiplier by 30%, diminishing multiplicatively.
                        float platingMult = Mathf.Pow(0.7f, modules);
                        __result *= smaller * platingMult;
                        break;
                    }

                    __result *= UnityEngine.Random.Range(smaller, bigger);
                    break;
                case DamageType.Starve:
                    // Add a minimum damage floor for starving.
                    if (__result < 1f)
                        __result = 1f;
                    break;
                case DamageType.Collide:
                case DamageType.Heat:
                    __result *= bigger;
                    break;
                default:
                    __result *= smaller;
                    break;
            }
        }

        /// <summary>
        /// Decrease the free health provided on respawn. Uses the 'official' event called on player respawn.
        /// </summary>
        public static void DecreaseRespawnHealth(Player player)
        {
            if (!player.liveMixin)
                return;

            float respawnMult = SaveData.Main.Config.DamageTaken switch
            {
                DamageDifficulty.LoveTaps => 0.75f,
                DamageDifficulty.Hard => 0.5f,
                DamageDifficulty.Deathrun => 0.25f,
                DamageDifficulty.Kharaa => 0.1f,
                _ => 1f
            };
            player.liveMixin.health *= respawnMult;
        }

        /// <summary>
        /// Get the lower and upper bounds of the damage multiplier for the given difficulty.
        /// </summary>
        /// <returns>A tuple with the lower and upper bound.</returns>
        public static (float, float) GetDamageMult(DamageDifficulty difficulty)
        {
            return difficulty switch
            {
                DamageDifficulty.LoveTaps => (1.1f, 1.25f),
                DamageDifficulty.Hard => (1.25f, 1.5f),
                DamageDifficulty.Deathrun => (1.5f, 2f),
                DamageDifficulty.Kharaa => (2f, 3f),
                _ => (1f, 1f)
            };
        }
    }
}