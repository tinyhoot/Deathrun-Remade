using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Enums;
using HarmonyLib;
using UnityEngine;

namespace DeathrunRemade.Patches
{
    [HarmonyPatch]
    public static class ExplosionPatcher
    {
        private const float DealDamageAfterSeconds = 1.6f;
        private static float _lastAnimTime;

        /// <summary>
        /// Get the damage that an entity should take from the Aurora explosion based on the entity's current depth.
        /// </summary>
        public static float GetExplosionDamage(Difficulty3 difficulty, GameObject gameObject)
        {
            if (difficulty == Difficulty3.Normal)
                return 0f;
            
            float damage = difficulty switch
            {
                Difficulty3.Hard => 300,
                Difficulty3.Deathrun => 500,
                _ => throw new InvalidDataException()
            };
            // Steadily decrease explosion strength with depth.
            float multiplier = Mathf.Clamp01(1 - (Ocean.GetDepthOf(gameObject) / GetExplosionDepth(difficulty)));

            return damage * multiplier;
        }
        
        /// <summary>
        /// Figure out the amount of damage the player takes during the Aurora explosion based on their surroundings.
        /// </summary>
        public static float GetPlayerExplosionDamageMult(Difficulty3 difficulty, Player player)
        {
            LiveMixin health = player.GetComponent<LiveMixin>();
            if (difficulty == Difficulty3.Normal || health is null)
                return 0f;

            float multiplier = 1f;

            if (player.GetVehicle() != null)
                // Medium damage reduction inside any vehicle (Seamoth/Prawn).
                multiplier *= 0.8f;
            if (player.IsInSub())
                // Large damage reduction within the safety of a base (or Cyclops, but that's unrealistic).
                multiplier *= 0.5f;

            return multiplier;
        }
        
        /// <summary>
        /// Get the depth to which the Aurora explosion reaches.
        /// </summary>
        public static int GetExplosionDepth(Difficulty3 difficulty)
        {
            return difficulty switch
            {
                Difficulty3.Normal => -1,
                Difficulty3.Hard => 50,
                Difficulty3.Deathrun => 100,
                _ => throw new InvalidDataException()
            };
        }

        /// <summary>
        /// Get the time in minutes after which the Aurora explodes.
        /// The options range between the shortest and longest possible random vanilla times.
        /// </summary>
        public static float GetExplosionTime(Timer time)
        {
            return time switch
            {
                Timer.Vanilla => Random.RandomRangeInt(46, 80),
                Timer.Short => 45,
                Timer.Medium => 60,
                Timer.Long => 90, // Not quite as vanilla but the number is nicer.
                _ => throw new InvalidDataException()
            };
        }

        /// <summary>
        /// Adjust the time to explosion.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CrashedShipExploder), nameof(CrashedShipExploder.SetExplodeTime))]
        private static void PatchExplosionTime(CrashedShipExploder __instance)
        {
            Timer setting = SaveData.Main.Config.ExplosionTime;
            // No changes necessary.
            if (setting == Timer.Vanilla)
                return;
            __instance.timeToStartCountdown = __instance.timeToStartWarning + 60 * GetExplosionTime(setting);
        }

        /// <summary>
        /// Deal damage when the visual shockwave of the explosion hits. The timing is based on my own experiments
        /// but relies on animation time, so it's independent of frame rates.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(VFXExplosionPlayerFX), nameof(VFXExplosionPlayerFX.UpdateScreenFX))]
        private static void PatchExplosionDamage(VFXExplosionPlayerFX __instance)
        {
            float threshold = DealDamageAfterSeconds / __instance.duration;
            if (__instance.animTime >= threshold && _lastAnimTime < threshold)
                PlayerTakeExplosionDamage(SaveData.Main.Config.ExplosionDepth, Player.main);
            _lastAnimTime = __instance.animTime;
        }

        /// <summary>
        /// Replaces the weak vanilla shockwave with one that is several orders of magnitude stronger.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CrashedShipExploder), nameof(CrashedShipExploder.CreateExplosiveForce))]
        private static bool ReplaceExplosionShockwave(CrashedShipExploder __instance)
        {
            WorldForces.AddExplosion(__instance.transform.position, DayNightCycle.main.timePassed + 0.01f, 12000f, 5000f);
            return false;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(WorldForces), nameof(WorldForces.DoFixedUpdate))]
        private static IEnumerable<CodeInstruction> WorldForcesTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions);
            // This fixes explosion forces only being applied within a narrow time delta. It fails to apply forces if
            // no frame happens to be rendered during that time which, you know, just might happen when the game
            // is currently throwing a huge fireball on screen.
            // With this, world forces should work even on lower frame rates (10ish fps).
            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_R8));
            matcher.SetOperandAndAdvance(0.1);
            matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_R4, 0.1f), new CodeMatch(OpCodes.Call));
            matcher.Advance(2);
            // Insert a new call to a damage-dealing function, which results in synced damage and physical impact.
            matcher.Insert(new []
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ExplosionPatcher), nameof(TakeExplosionDamage)))
            });
            return matcher.InstructionEnumeration();
        }

        /// <summary>
        /// Apply explosion damage to an object at the same time as the physical force hits it.
        /// </summary>
        public static void TakeExplosionDamage(WorldForces wf)
        {
            LiveMixin health = wf.GetComponent<LiveMixin>();
            // Player is handled separately.
            if (health is null || wf.gameObject == Player.main.gameObject)
                return;
            float damage = GetExplosionDamage(SaveData.Main.Config.ExplosionDepth, wf.gameObject);
            // The player can hide in bases and vehicles and therefore needs some extra handling.
            if (wf.gameObject == Player.main.gameObject)
                damage *= GetPlayerExplosionDamageMult(SaveData.Main.Config.ExplosionDepth, Player.main);
            // Damage type pressure because it's less likely to be resisted and it's a shockwave crushing you.
            health.TakeDamage(damage, type: DamageType.Pressure);
        }
        
        /// <summary>
        /// Apply explosion damage to the player at the same time as the physical force hits them.
        /// Separate because this needs extra calculations, and does not trigger with the other WorldForces if the
        /// player is somewhere inside at the time of the explosion.
        /// </summary>
        public static void PlayerTakeExplosionDamage(Difficulty3 difficulty, Player player)
        {
            LiveMixin health = player.liveMixin;
            if (health is null)
                return;

            float damage = GetExplosionDamage(difficulty, player.gameObject);
            // Special handling for bases here since those do not have WorldForces components.
            if (player.GetCurrentSub() is BaseRoot root)
            {
                foreach (var leaker in root.flood.leakers)
                {
                    leaker.liveMixin.TakeDamage(damage);
                }
            }
            // The player can hide in bases and vehicles and therefore needs some extra handling.
            damage *= GetPlayerExplosionDamageMult(difficulty, player);
            // Damage type pressure because it's less likely to be resisted and it's a shockwave crushing you.
            health.TakeDamage(damage, type: DamageType.Pressure);
        }
    }
}