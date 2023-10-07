using System;
using System.IO;
using DeathrunRemade.Items;
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
        public static NitrogenHandler Main;

        private const float AccumulationScalar = 10f * UpdateInterval;
        private const float AscentGraceTime = 2f; // Number of seconds at fast speeds before consequences set in.
        private const float AscentNitrogenPerSec = 30f;
        private const float AscentDepthPerSec = 6f;
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
        
        private float _ascentRate;
        private float _ascentTransgressionTime;
        private float _lastAscentPunishTime;
        private int _damageTicks;
        private NotificationHandler _notifications;
        private Hootimer _timer;

        private void Awake()
        {
            Main = this;
            _notifications = DeathrunInit._Notifications;
            _timer = new Hootimer(() => Time.deltaTime, UpdateInterval);
            GameEventHandler.OnPlayerDeath += OnPlayerDeath;
        }

        private void FixedUpdate()
        {
            Player player = Player.main;
            if (player is null)
                return;
            
            UpdateAscentRate(player);
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
            CheckForFastAscent(save);
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

            float safeDepthPortion = save.Nitrogen.safeDepth - GraceDepth + 1;
            if (safeDepthPortion <= 0)
            {
                save.Nitrogen.nitrogen -= Mathf.Min(nitrogen, save.Nitrogen.nitrogen);
                return;
            }

            save.Nitrogen.safeDepth -= safeDepthPortion;
            save.Nitrogen.nitrogen -= Mathf.Min(nitrogen - safeDepthPortion, save.Nitrogen.nitrogen);
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
        /// Check whether the player is ascending too quickly and needs to be punished for it.
        /// </summary>
        private void CheckForFastAscent(SaveData save)
        {
            // Reset punishments if we're moving at comfortable speeds.
            if (_ascentRate <= 2)
            {
                _ascentTransgressionTime = Math.Max(_ascentTransgressionTime - Time.deltaTime, 0f);
                _lastAscentPunishTime = 0f;
                return;
            }

            // Do nothing at medium speeds.
            if (_ascentRate <= 4)
                return;
            
            // Starting to get too fast. Start accruing "bad behaviour time".
            // *Technically* this assumes that the update runs every frame but the math works out so long as adding
            // and removing time both run at the same update rates.
            _ascentTransgressionTime += Time.deltaTime;

            WarningHandler.ShowWarning(Warning.AscentSpeed);

            // Increase nitrogen only after longer than the grace time at high speeds.
            if (_ascentRate <= 5 || (_ascentTransgressionTime / UpdateInterval) <= AscentGraceTime)
                return;

            float interval = 1 / (save.Nitrogen.nitrogen >= 100f ? AscentDepthPerSec : AscentNitrogenPerSec);
            if (Time.time >= _lastAscentPunishTime + interval)
            {
                if (_lastAscentPunishTime == 0f)
                    AddNitrogen(1f);
                else
                    // Ideally only adds one at a time, but adjusts for lower frame rates / update rates.
                    AddNitrogen((Time.time - _lastAscentPunishTime) / interval);
                _lastAscentPunishTime = Time.time;
            }
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
            if (suit == Suit.ReinforcedMk2 || suit == Suit.ReinforcedFiltration)
                modifier = 0.75f;
            if (suit == Suit.ReinforcedMk3)
                modifier = 0.55f;
            // Slightly lowered if wearing the rebreather.
            if (mask == TechType.Rebreather)
                modifier -= 0.05f;

            return modifier;
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
        /// Average the player's vertical speed over the past second.
        /// </summary>
        private void UpdateAscentRate(Player player)
        {
            float speed = player.GetComponent<Rigidbody>().velocity.y;
            float ups = (1 / Time.fixedDeltaTime);
            _ascentRate = (_ascentRate * (ups - 1) + speed) / ups;
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