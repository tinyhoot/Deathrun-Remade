using DeathrunRemade.Items;
using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Enums;
using HootLib.Objects;
using UnityEngine;

namespace DeathrunRemade.Handlers
{
    /// <summary>
    /// Responsible for the math behind nitrogen and the bends.
    /// </summary>
    internal class NitrogenHandler : MonoBehaviour
    {
        public static NitrogenHandler Main;

        private const float AccumulationScalar = 10f * UpdateInterval;
        private const float GraceDepth = 10f; // Consider this value and below as completely safe, no bends.
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

        private const float UpdateInterval = 0.25f;
        private Hootimer _timer;

        private void Awake()
        {
            Main = this;
            _timer = new Hootimer(PDA.GetDeltaTime, UpdateInterval);
        }

        private void Update()
        {
            // Only run this update every so often.
            if (!_timer.Tick())
                return;

            Player player = Player.main;
            SaveData save = SaveData.Main;
            // Safety check.
            if (player is null || save is null)
                return;

            float currentDepth = player.GetDepth();
            float intensity = GetDepthIntensity(currentDepth);
            save.Nitrogen.nitrogen = UpdateNitrogen(currentDepth, save.Nitrogen.safeDepth, save.Nitrogen.nitrogen);
            float oldSafeDepth = save.Nitrogen.safeDepth;
            save.Nitrogen.safeDepth = UpdateSafeDepth(currentDepth, save.Nitrogen.safeDepth, intensity, save.Nitrogen.nitrogen);
            UpdateHud(oldSafeDepth, save.Nitrogen.safeDepth);
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
        /// Calculate the modifier for how quickly the player accumulates nitrogen. Better suits accumulate nitrogen
        /// more slowly.
        /// </summary>
        private float GetAccumulationModifier(Inventory inventory)
        {
            TechType mask = inventory.equipment.GetTechTypeInSlot("Head");
            TechType suit = inventory.equipment.GetTechTypeInSlot("Body");

            // For vanilla suits. Can't have modded ones in here sadly.
            float modifier = suit switch
            {
                TechType.RadiationSuit => 0.95f,
                TechType.WaterFiltrationSuit => 0.95f,
                TechType.ReinforcedDiveSuit => 0.85f,
                _ => 1f
            };
            if (suit.Equals(Suit.ReinforcedMk2) || suit.Equals(Suit.ReinforcedFiltration))
                modifier = 0.75f;
            if (suit.Equals(Suit.ReinforcedMk3))
                modifier = 0.55f;
            // Slightly lowered if wearing the rebreather.
            if (mask.Equals(TechType.Rebreather))
                modifier -= 0.05f;

            return modifier;
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
        /// Notify the hud element when it should show or hide itself.
        /// </summary>
        private void UpdateHud(float oldSafeDepth, float newSafeDepth)
        {
            if (oldSafeDepth < GraceDepth && newSafeDepth >= GraceDepth)
            {
                DeathrunInit._DepthHud.FadeIn();
                return;
            }
            if (newSafeDepth < GraceDepth)
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
            if (depth <= GraceDepth)
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
            // If we're going down, apply modifiers from equipment to slow the adjustment rate.
            float equipMult = 1f;
            if (safeDepth > lastSafeDepth)
                equipMult = GetAccumulationModifier(Inventory.main);

            return UWE.Utils.Slerp(lastSafeDepth, safeDepth, equipMult * modifier * AccumulationScalar);
        }
    }
}