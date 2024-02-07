using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private const float AccumulationScalar = 10f * UpdateInterval;
        public const float GraceDepth = 10f; // Consider this value and below as completely safe, no bends.
        public const float MaxDepth = 2000f; // The maximum depth as far as calculations are concerned.
        public const float MaxNitrogenBuildup = 70f; // The value nitrogen buildup must reach for safe depth to kick in.
        private const int TicksBeforeDamage = (int)(2f / UpdateInterval); // Number of seconds relative to ups.
        private const float UpdateInterval = 0.1f; // Controls the number of updates per second.
        private const float WindupMin = 0.5f;
        private const float WindupMax = 1f;
        private const float WindupDelta = 0.5f * UpdateInterval; // Reach max speed in about a second.
        
        private int _damageTicks;
        private Hootimer _timer;
        private float _windupModifier;
        
        private readonly AnimationCurve _dissipationCurve = new AnimationCurve(new[]
        {
            new Keyframe(0f, 1f), // Fast dissipation on the last few meters.
            new Keyframe(GraceDepth / MaxDepth, 0.75f),
            new Keyframe((GraceDepth / MaxDepth) + 0.01f, 0.25f), // Dissipation speed is about the same at all times
            new Keyframe(1f, 0.35f)
        });
        private readonly AnimationCurve _accumulationCurve = new AnimationCurve(new[]
        {
            new Keyframe(0f, 0f),
            new Keyframe((GraceDepth / MaxDepth) - 0.01f, 0f), // No buildup until the grace depth.
            new Keyframe(GraceDepth / MaxDepth, 0.3f),
            new Keyframe(0.35f, 1f), // 700m. Tighten the screws after the Lost River.
            new Keyframe(0.55f, 3f), // 1100m, lava zones.
            new Keyframe(0.75f, 4f), // Be lenient at 1500m and below - the prison aquarium.
            new Keyframe(1f, 1f)
        });
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
            UpdateNitrogen(player, save, currentDepth, save.Nitrogen.safeDepth);
            CheckForBendsDamage(player, save, currentDepth, save.Nitrogen.safeDepth, save.Nitrogen.nitrogen);
            // ShowDebugInfo();
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

            float leftToFill = MaxNitrogenBuildup - save.Nitrogen.nitrogen;
            if (leftToFill >= nitrogen)
            {
                save.Nitrogen.nitrogen += nitrogen;
                return;
            }

            save.Nitrogen.nitrogen = MaxNitrogenBuildup;
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
            
            float safeDepthPortion = save.Nitrogen.safeDepth;
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
            SaveData.Main.Nitrogen.nitrogen = 0f;
            SaveData.Main.Nitrogen.safeDepth = 0f;
        }

        /// <summary>
        /// Calculate the hypothetical safe depth for the given depth.
        /// </summary>
        public static float CalculateSafeDepth(float depth)
        {
            float safeDepth = depth * 3f / 4f;
            // Make ascending just that tiny bit less annoying in the last few meters.
            if (depth < GraceDepth * 2f)
                safeDepth -= 3f;
            return safeDepth;
        }

        /// <summary>
        /// Check whether the player needs to take damage from rapid decompression this update.
        /// </summary>
        private void CheckForBendsDamage(Player player, SaveData save, float depth, float safeDepth, float nitrogen)
        {
            if (depth >= safeDepth - 1 || safeDepth <= GraceDepth || nitrogen < MaxNitrogenBuildup)
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
        /// Mods can add modifiers to items by calling <see cref="AddNitrogenModifier"/>.
        /// </summary>
        private float GetEquipmentModifier(Inventory inventory)
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
        /// Get the difficulty-based modifier for nitrogen accumulation speed.
        /// </summary>
        private static float GetDifficultyAccumulationModifier(Difficulty3 difficulty)
        {
            return difficulty switch
            {
                Difficulty3.Normal => 0f, // Should never even happen, nitrogen is disabled on Normal.
                Difficulty3.Hard => 0.75f,
                Difficulty3.Deathrun => 1f,
                _ => throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, null)
            };
        }

        /// <summary>
        /// Handle special circumstances which modify the ideal safe depth, such as decompression modules.
        /// </summary>
        private float GetSafeDepthOverride(Player player, SaveData save, float safeDepth)
        {
            // Decompression module.
            if (player.IsInsidePoweredVehicle() 
                && player.GetVehicle().modules.GetCount(DecompressionModule.s_TechType) > 0)
                return 0f;
            // Filterchip.
            if ((player.IsInsideWalkable() || player.precursorOutOfWater) 
                && Inventory.main.equipment.GetCount(FilterChip.s_TechType) > 0)
                return 0f;
            // Alien bases dissipate nitrogen if that setting is enabled.
            if (save.Config.AlienBaseSafety && player.precursorOutOfWater)
                return 0f;
            
            return safeDepth;
        }

        private float GetWindupModifier()
        {
            if (_windupModifier < WindupMax)
                _windupModifier = Mathf.Min(_windupModifier + WindupDelta, WindupMax);
            return _windupModifier;
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
        /// Returns true if the player is currently ascending.
        /// </summary>
        public static bool IsGoingUp(float safeDepth, float lastSafeDepth)
        {
            // Set to lower or *equals* so we get dissipation logic for the nitrogen buffer at 0 depth.
            return safeDepth <= lastSafeDepth;
        }

        /// <summary>
        /// Take bends damage based on how far apart the current and safe depth are.
        /// </summary>
        private void TakeDamage(Player player, SaveData save, float currentDepth)
        {
            float depthDiff = save.Nitrogen.safeDepth - currentDepth;
            int baseDamage = GetBendsDamage(save.Config.NitrogenBends);
            // Make the damage more manageable for small transgressions.
            baseDamage = depthDiff switch
            {
                < 2f => baseDamage / 4,
                < 5f => baseDamage / 2,
                _ => baseDamage
            };
            
            WarningHandler.ShowWarning(Warning.DecompressionDamage);
            DeathrunInit._RunHandler.SetCauseOfDeathOverride("The Bends");
            float damage = baseDamage + (Random.value * baseDamage) + depthDiff;
            DeathrunUtils.TakeOneShotProtectedDamage(player.liveMixin, damage, DamageType.Starve);
            
            // Bring the player halfway to their ideal safe depth to prevent punishing multiple times for the same
            // mistake while making it hard to tank through.
            RemoveNitrogen((currentDepth - CalculateSafeDepth(currentDepth)) / 2f);
        }

        /// <summary>
        /// Calculate the nitrogen difference of the current timestep.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="save">The current save game.</param>
        /// <param name="depth">The player's current depth.</param>
        /// <param name="lastSafeDepth">The safe depth during the last timestep.</param>
        private void UpdateNitrogen(Player player, SaveData save, float depth, float lastSafeDepth)
        {
            // Calculate the ideal safe depth for the current depth.
            float safeDepth = CalculateSafeDepth(depth);
            safeDepth = GetSafeDepthOverride(player, save, safeDepth);
            bool dissipating = IsGoingUp(safeDepth, lastSafeDepth);
            // Don't adjust anything if we're already close enough.
            if (Mathf.Abs(safeDepth - lastSafeDepth) < 0.5f && !dissipating)
            {
                // Reset windup.
                _windupModifier = Mathf.Max(_windupModifier - WindupDelta, WindupMin);
                return;
            }
            
            // Use different speed modifiers for going up vs going down.
            AnimationCurve curve = dissipating ? _dissipationCurve : _accumulationCurve;
            float depthMult = curve.Evaluate(safeDepth / MaxDepth);
            
            float delta = depthMult * AccumulationScalar;
            if (dissipating)
            {
                RemoveNitrogen(delta);
            }
            else
            {
                // If we're going down, reduce accumulation based on equipment.
                float equipMult = GetEquipmentModifier(Inventory.main);
                // Make accumulation less punishing on lower difficulties.
                float difficultyMult = GetDifficultyAccumulationModifier(save.Config.NitrogenBends);
                // Add a windup modifier that makes the depth lower slowly and gradually adjusts to full speed.
                float windupMult = GetWindupModifier();
                
                delta = delta * equipMult * difficultyMult * windupMult;
                AddNitrogen(delta);
            }
        }

        private void ShowDebugInfo()
        {
            string msg = $"Nitrogen: {SaveData.Main.Nitrogen.nitrogen}";
            NotificationHandler.Main.AddMessage(NotificationHandler.MiddleLeft, msg);
        }
    }
}