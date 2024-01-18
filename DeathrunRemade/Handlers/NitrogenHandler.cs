using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Enums;
using HootLib.Objects;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DeathrunRemade.Handlers
{
    /// <summary>
    /// Responsible for the math behind nitrogen and the bends.
    /// </summary>
    internal class NitrogenHandler : MonoBehaviour
    {
        private const float AccumulationScalar = 10f * UpdateInterval;
        private const float GraceDepth = 10f; // Consider this value and below as completely safe, no bends.
        private const int TicksBeforeDamage = (int)(2f / UpdateInterval); // Number of seconds relative to ups.
        private const float UpdateInterval = 0.25f;
        
        private readonly AnimationCurve _intensityCurve = new AnimationCurve(new[]
        {
            new Keyframe(0f, 0.20f),
            new Keyframe(1f, 1f)
        });
        private readonly AnimationCurve _nitrogenCurve = new AnimationCurve(new[]
        {
            new Keyframe((GraceDepth / 100) - 0.01f, 0f), // No nitrogen up until the grace depth.
            new Keyframe(GraceDepth / 100, 1f),
            new Keyframe((GraceDepth / 50) - 0.01f, 1.25f), // Only small increases just after the grace depth.
            new Keyframe(GraceDepth / 50, 1.25f), // After grace depth *2, let loose.
            new Keyframe(1f, 10f) // Maximum speed at 2000 depth.
        });
        
        private int _damageTicks;
        private Hootimer _timer;

        private static readonly Dictionary<TechType, float[]> SuitNitrogenModifiers = new Dictionary<TechType, float[]>
        {
            { TechType.RadiationSuit, new[] { 0.05f, 0.05f } },
            { TechType.ReinforcedDiveSuit, new []{ 0.05f, 0.05f } },
            { TechType.WaterFiltrationSuit, new []{ 0.15f, 0.1f } },
            { TechType.Rebreather, new[] { 0.05f, 0f } },
        };

        /// <summary>
        /// Add a new suit to the dictionary of TechTypes and their Nitrogen Modifiers.
        /// </summary>
        /// <param name="techType"></param>
        /// <param name="crushDepth"></param>
        public static void AddNitrogenModifier(TechType techType, IEnumerable<float> crushDepth)
        {
            SuitNitrogenModifiers[techType] = crushDepth.ToArray();
        }

        /// <summary>
        /// Get the Nitrogen modifier for the given TechType. Returns false if the suit is not in the dictionary.
        /// </summary>
        public static bool TryGetNitrogenModifier(TechType techType, out float[] nitrogenModifiers)
        {
            return SuitNitrogenModifiers.TryGetValue(techType, out nitrogenModifiers);
        }

        private void Awake()
        {
            _timer = new Hootimer(() => Time.deltaTime, UpdateInterval);
            GameEventHandler.OnPlayerDeath += OnPlayerDeath;
        }

        private void Update()
        {
            // Only run this update every so often.
            if (!_timer.Tick())
                return;

            Player player = Player.main;
            SaveData save = SaveData.Main;
            // Safety check.
            if (player == null || save is null)
                return;

            float currentDepth = player.GetDepth();
            float intensity = GetDepthIntensity(currentDepth);
            save.Nitrogen.nitrogen = UpdateNitrogen(currentDepth, save.Nitrogen.safeDepth, save.Nitrogen.nitrogen);
            float oldSafeDepth = save.Nitrogen.safeDepth;
            save.Nitrogen.safeDepth = UpdateSafeDepth(currentDepth, save.Nitrogen.safeDepth, intensity, save.Nitrogen.nitrogen);
            CheckForBendsDamage(player, save, currentDepth, save.Nitrogen.safeDepth, save.Nitrogen.nitrogen);
            UpdateHud(oldSafeDepth, save.Nitrogen.safeDepth);
        }

        /// <summary>
        /// Reset nitrogen on player death.
        /// </summary>
        private void OnPlayerDeath(Player player, DamageType damageType)
        {
            SaveData.Main.Nitrogen.nitrogen = 0f;
            SaveData.Main.Nitrogen.safeDepth = 0f;
        }

        /// <summary>
        /// Add nitrogen to the player. If nitrogen is already at its maximum value, the safe depth will increase to
        /// greater depths instead.
        /// </summary>
        public void AddNitrogen(float nitrogen)
        {
            SaveData save = SaveData.Main;
            if (save is null)
                return;

            float leftToFill = 100f - save.Nitrogen.nitrogen;
            if (leftToFill >= nitrogen)
            {
                save.Nitrogen.nitrogen += nitrogen;
                return;
            }

            save.Nitrogen.nitrogen = 100f;
            save.Nitrogen.safeDepth += nitrogen - leftToFill;
        }

        /// <summary>
        /// Remove nitrogen from the player. If safe depth is active, reduce that instead.
        /// </summary>
        public void RemoveNitrogen(float nitrogen)
        {
            SaveData save = SaveData.Main;
            if (save is null)
                return;

            // Be a bit fuzzy with safe depth so all the calculations can update properly.
            float safeDepthPortion = save.Nitrogen.safeDepth - (GraceDepth / 2);
            // Safe depth is not active, just remove nitrogen.
            if (safeDepthPortion <= 0)
            {
                save.Nitrogen.nitrogen -= Mathf.Min(nitrogen, save.Nitrogen.nitrogen);
                return;
            }

            save.Nitrogen.safeDepth -= Mathf.Min(nitrogen, safeDepthPortion);
            if (nitrogen > safeDepthPortion)
                save.Nitrogen.nitrogen -= Mathf.Min(nitrogen - safeDepthPortion, save.Nitrogen.nitrogen);
        }

        /// <summary>
        /// Reset and remove accumulated nitrogen completely.
        /// </summary>
        public void ResetNitrogen()
        {
            UpdateHud(SaveData.Main.Nitrogen.safeDepth, 0f);
            SaveData.Main.Nitrogen.nitrogen = 0f;
            SaveData.Main.Nitrogen.safeDepth = 0f;
        }

        /// <summary>
        /// Calculate the player's current safe depth status based on how close they're cutting it.
        /// </summary>
        public static SafeDepthStatus CalculateDepthStatus(float depth, float safeDepth)
        {
            if (depth < safeDepth)
                return SafeDepthStatus.Exceeded;
            if (IsApproachingSafeDepth(depth, safeDepth))
                return SafeDepthStatus.Approaching;
            return SafeDepthStatus.Safe;
        }

        /// <summary>
        /// Calculate the hypothetical safe depth for the given depth.
        /// </summary>
        public static float CalculateSafeDepth(float depth)
        {
            float safeDepth = depth * 3 / 4;
            // Make ascending just that tiny bit less annoying in the last few meters.
            if (depth < GraceDepth * 2)
                safeDepth -= 2;
            return safeDepth;
        }

        /// <summary>
        /// Check whether the player needs to take damage from rapid decompression this update.
        /// </summary>
        private void CheckForBendsDamage(Player player, SaveData save, float depth, float safeDepth, float nitrogen)
        {
            if (depth >= safeDepth - 1 || safeDepth <= GraceDepth || nitrogen < 100f)
                return;
            // No consequences for any of this happening inside vehicles or bases.
            if (player.IsInsidePoweredSubOrVehicle())
                return;

            WarningHandler.ShowWarning(Warning.Decompression);

            _damageTicks++;
            if (_damageTicks > TicksBeforeDamage)
            {
                TakeDamage(player, save, depth);
                _damageTicks = 0;
            }
        }

        /// <summary>
        /// Calculate the modifier for how quickly the player accumulates nitrogen. 
        /// Better suits accumulate nitrogen more slowly. 
        /// Wearing the rebreather will also slow down accumulation.
        /// Mods can add modifiers to items by calling AddNitrogenModifier.
        /// </summary>
        private float GetAccumulationModifier(Inventory inventory)
        {

            SaveData save = SaveData.Main;

            if (save == null)
                return 1f;

            Difficulty3 difficulty = save.Config.NitrogenBends;

            float modifier = 1f;
            foreach (var pair in inventory.equipment.equippedCount)
            {
                // Ignore items that are not equipped.
                if (pair.Value <= 0)
                    continue;

                TechType techType = pair.Key;
                if (techType == TechType.None)
                    continue;

                // Ignore items that do not have a modifier.
                if (!TryGetNitrogenModifier(techType, out var itemModifiers))
                    continue;

                float itemModifier = itemModifiers.Length > (int)difficulty ? itemModifiers[(int)difficulty] : itemModifiers[itemModifiers.Length - 1];

                // multiplying by count incase some day someone makes a nitrogen processing chip that stacks. :P
                modifier -= itemModifier * pair.Value;
            }

            return Mathf.Max(modifier, 0f);
        }
        
        /// <summary>
        /// Get the damage dealt by decompression sickness.
        /// </summary>
        public static int GetBendsDamage(Difficulty3 value)
        {
            return value switch
            {
                Difficulty3.Normal => 10,
                Difficulty3.Hard => 10,
                Difficulty3.Deathrun => 20,
                _ => throw new InvalidDataException()
            };
        }

        /// <summary>
        /// Calculate a modifier based on the given depth to indicate how heavily that depth should influence
        /// nitrogen and bends accumulation.
        /// </summary>
        private float GetDepthIntensity(float depth)
        {
            float time = depth / 2000f;
            return _intensityCurve.Evaluate(time);
        }
        
        /// <summary>
        /// Check whether the given depth is close enough to the safe depth to warrant emitting a warning.
        /// </summary>
        public static bool IsApproachingSafeDepth(float depth, float safeDepth)
        {
            float diff = depth - safeDepth;
            return diff < 10 || diff / safeDepth < 0.08f;
        }

        /// <summary>
        /// Take bends damage based on how far apart the current and safe depth are.
        /// </summary>
        private void TakeDamage(Player player, SaveData save, float currentDepth)
        {
            LiveMixin health = player.GetComponent<LiveMixin>();
            float depthDiff = save.Nitrogen.safeDepth - currentDepth;
            
            int baseDamage = GetBendsDamage(save.Config.NitrogenBends);
            // Make the damage more manageable for small transgressions.
            if (depthDiff < 5)
            {
                if (depthDiff < 2)
                    baseDamage /= 4;
                else
                    baseDamage /= 2;
            }

            float damage = baseDamage + (Random.value * baseDamage) + depthDiff;
            // Don't oneshot the player from like half health.
            if (health.health > 0.1f)
                damage = Mathf.Min(damage, health.health - 0.05f);

            WarningHandler.ShowWarning(Warning.DecompressionDamage);
            DeathrunInit._RunHandler.SetCauseOfDeathOverride("The Bends");
            health.TakeDamage(damage, type: DamageType.Starve);
            // After damage, adjust the safe depth upwards a bit.
            save.Nitrogen.safeDepth = Mathf.Max(Mathf.Min(currentDepth, GraceDepth),
                CalculateSafeDepth(save.Nitrogen.safeDepth));
        }

        /// <summary>
        /// Notify the hud element when it should show or hide itself.
        /// </summary>
        private void UpdateHud(float oldSafeDepth, float newSafeDepth)
        {
            if (oldSafeDepth < GraceDepth && newSafeDepth >= GraceDepth)
            {
                DeathrunInit._DepthHud.FadeIn();
                return;
            }
            if (newSafeDepth < 3f)
                DeathrunInit._DepthHud.FadeOut();
        }

        /// <summary>
        /// Calculate and return the new nitrogen value.
        /// </summary>
        /// <param name="depth">The current depth.</param>
        /// <param name="safeDepth">The current safe depth.</param>
        /// <param name="lastNitrogen">The nitrogen level on the last update.</param>
        private float UpdateNitrogen(float depth, float safeDepth, float lastNitrogen)
        {
            // Nitrogen always fills up before safe depth lowers, and only empties after safe depth is gone.
            float target;
            if (depth <= GraceDepth && safeDepth == 0)
                target = 0f;
            else if (safeDepth <= GraceDepth / 2)
                target = (depth - GraceDepth) * 10;
            else
                // Do not start dissipating Nitrogen until the safe depth is back within reasonable bounds.
                target = 100f;
            target = Mathf.Clamp(target, 0f, 100f);
            
            // Shortcut this if there is nothing to update.
            if (Mathf.Approximately(target, lastNitrogen))
                return lastNitrogen;

            // Be a bit more lenient in shallow depths but then quickly get harsher the lower the player goes.
            float time;
            if (depth <= GraceDepth * 2)
            {
                time = GraceDepth / 100f;
            }
            else
            {
                time = GraceDepth / 50f;
                time += depth / 2000f;
            }

            float rate = _nitrogenCurve.Evaluate(time);
            return UWE.Utils.Slerp(lastNitrogen, target, rate);
        }

        /// <summary>
        /// Calculate the safe depth and return a depth approaching it by one time step from the current depth.
        /// </summary>
        /// <param name="currentDepth">The current depth.</param>
        /// <param name="lastSafeDepth">The safe depth on the last update.</param>
        /// <param name="modifier">A modifier to influence how quickly the result adjusts to the safe depth.</param>
        /// <param name="nitrogen">The current nitrogen level.</param>
        private float UpdateSafeDepth(float currentDepth, float lastSafeDepth, float modifier, float nitrogen)
        {
            float safeDepth = 0f;
            // Use nitrogen as a buffer before safe depth really comes into play.
            if (nitrogen >= 100f)
                // Safe depth will always tend towards around 3/4 of your current depth.
                safeDepth = CalculateSafeDepth(currentDepth);
            // If we're going up, make the last few meters very fast to disappear.
            if (safeDepth <= GraceDepth && lastSafeDepth <= GraceDepth * 1.5f)
            {
                modifier *= 5f;
                safeDepth = 0f;
            }

            // If we're going down, apply modifiers from equipment to slow the adjustment rate.
            float equipMult = 1f;
            if (safeDepth > lastSafeDepth)
                equipMult = GetAccumulationModifier(Inventory.main);

            return UWE.Utils.Slerp(lastSafeDepth, safeDepth, equipMult * modifier * AccumulationScalar);
        }
    }
}