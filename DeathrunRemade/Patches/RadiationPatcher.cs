using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Attributes;
using DeathrunRemade.Objects.Enums;
using HarmonyLib;
using UnityEngine;

namespace DeathrunRemade.Patches
{
    [HarmonyPatch]
    [PatchCategory(ApplyPatch.Always)]
    internal static class RadiationPatcher
    {
        // Static readonly means it can be used by reference, which allows quick equality checks.
        private static readonly string _radiationImmuneMsg = "Radiation (Immune)";
        private static Vector3 _radWarningPos;
        private static Vector3 _radWarningImmunePos;
        private static bool _playerIsImmune;
        // This looks odd (no dictionary), but we'll be iterating over this in descending order.
        private static readonly List<(string, float)> RadiationFXStrength = new List<(string, float)>
        {
            ("Elevator", 1f),
            ("LockerRoom", 1f),
            ("Seamoth", 1f),
            ("ExoRoom", 0.8f),
            ("LivingArea", 0.8f),
            ("Cargo", 0.8f),
            ("Entrance_03", 0.7f),
            ("Entrance_01_01", 0.7f),
            ("Entrance_01", 0.6f),
            ("THallway_Lower", 0.6f),
            ("THallway", 0.5f),
            ("Entrance", 0.5f),
            ("CrashedShip", 0.4f),
            ("GeneratorRoom", 1.6f),
            ("CrashZone", 0.3f)
        };
        private static readonly List<TechType> RadSafeEquipment = new List<TechType>
        {
            TechType.RadiationHelmet, TechType.RadiationSuit, TechType.RadiationGloves
        };

        public static void Init()
        {
            UpdateIsImmune(null, null);
            // No need to unregister these listeners as the sender gets destroyed on return to main menu.
            Inventory.main.equipment.onEquip += UpdateIsImmune;
            Inventory.main.equipment.onUnequip += UpdateIsImmune;
        }

        /// <summary>
        /// Replace the hardcoded distance value in radiation damage with the more dynamic distance calculation function
        /// from this mod.
        /// </summary>
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(DamagePlayerInRadius), nameof(DamagePlayerInRadius.DoDamage))]
        private static IEnumerable<CodeInstruction> DamageDistancePatch(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions);
            matcher
                // Match to where the game loads the PlayerDistanceTracker and tries to grab its value for the
                // Aurora's distance to the player.
                .MatchForward(true, 
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld),
                    new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MemberInfo)i.operand).Name.Contains("distanceToPlayer")))
                // Insert an extra instruction to load the damage type of this instance.
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    CodeInstruction.LoadField(typeof(DamagePlayerInRadius), nameof(DamagePlayerInRadius.damageType)))
                // Replace the game's call to the distance property with our own function call.
                .SetInstruction(
                    Transpilers.EmitDelegate<Func<PlayerDistanceTracker, DamageType, float>>((tracker, type) =>
                    {
                        // For radiation trackers only, overwrite the distance calculation with our own.
                        if (type == DamageType.Radiation)
                            return GetRadiationDistance(tracker);
                        // For everything else we leave the game to do its thing.
                        return tracker.distanceToPlayer;
                    }));
            return matcher.InstructionEnumeration();
        }
        
        /// <summary>
        /// Similar to the patch above, replace the distance calculation with our own function call.
        /// In a second step, reduce radiation if inside or in a submarine.
        /// </summary>
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(RadiatePlayerInRange), nameof(RadiatePlayerInRange.Radiate))]
        private static IEnumerable<CodeInstruction> RadiateRangePatch(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions);
            matcher
                // Match to where the game loads the PlayerDistanceTracker and tries to grab its value for the
                // Aurora's distance to the player.
                .MatchForward(true, 
                    new CodeMatch(OpCodes.Ldarg_0), 
                    new CodeMatch(OpCodes.Ldfld), 
                    new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MemberInfo)i.operand).Name.Contains("distanceToPlayer")))
                // Replace the function call in this place with our own to effectively overwrite distance calculations.
                .SetInstruction(CodeInstruction.Call(typeof(RadiationPatcher), nameof(GetRadiationDistance)))
                // Skip ahead to near the end of the function.
                .MatchForward(false, 
                    new CodeMatch(OpCodes.Ldloc_2),
                    new CodeMatch(i => i.opcode == OpCodes.Call && ((MemberInfo)i.operand).Name == nameof(Mathf.Clamp01)))
                // Advance once to get past the last branch label so we don't have to deal with labels.
                .Advance(1)
                // Insert a quick delegate to reduce the radiation severity when inside. This consumes the variable on
                // the stack that was meant for a vanilla method.
                .Insert(
                    Transpilers.EmitDelegate<Func<float, float>>(rad =>
                    {
                        if (Player.main.IsInside())
                            rad /= 4;
                        if (Player.main.IsInSubmarine())
                            rad /= 2;
                        return rad;
                    }),
                    // Store the result to the local variable and load it on the stack so the vanilla method can work
                    // as expected.
                    new CodeInstruction(OpCodes.Stloc_2),
                    new CodeInstruction(OpCodes.Ldloc_2));

            return matcher.InstructionEnumeration();
        }

        /// <summary>
        /// Display the radiation warning even if the player is immune.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(uGUI_RadiationWarning), nameof(uGUI_RadiationWarning.IsRadiated))]
        private static void DisplayRadiationWarningIfImmune(uGUI_RadiationWarning __instance, ref bool __result)
        {
            // No reason to change anything if it's already displaying.
            if (__result || Player.main == null || LeakingRadiation.main == null)
                return;
            RadiatePlayerInRange radiation = LeakingRadiation.main.radiatePlayerInRange;
            // Game is still loading.
            if (radiation.tracker == null)
                return;
            
            if (GetRadiationDistance(radiation.tracker) <= radiation.radiateRadius)
            {
                PDA pda = Player.main.GetPDA();
                // Display while the PDA is not being used.
                __result = pda == null || !pda.isInUse;
            }
        }

        /// <summary>
        /// If the radiation warning is currently being displayed while the player is immune, move it to a corner
        /// and do not flash the animation.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(uGUI_RadiationWarning), nameof(uGUI_RadiationWarning.Update))]
        private static void MoveRadiationWarning(uGUI_RadiationWarning __instance)
        {
            if (Player.main == null)
                return;

            // Get the animation for the radiation warning.
            Animation animation = __instance.warning.GetComponent<Animation>();
            bool isImmuneMsg = __instance.text.text.Equals(_radiationImmuneMsg);

            // Check whether any update is necessary at all.
            if (_playerIsImmune == isImmuneMsg)
                return;
            
            if (_playerIsImmune)
            {
                // Move warning to top right corner, stop the animation.
                __instance.text.text = _radiationImmuneMsg;
                __instance.transform.localPosition = _radWarningImmunePos; //new Vector3(720f, 550f, 0f);
                if (animation != null)
                {
                    AnimationState state = animation.GetState(0);
                    state.normalizedTime = 0.25f; // Pick out a not-too-bright, not-too-dull frame of the animation
                    animation.Play();
                    animation.Sample(); // Forces the pose to be calculated
                    animation.Stop(); // Actually commits the pose without waiting until end of frame
                    animation.enabled = false; // Now cease looping the animation
                }
            }
            else
            {
                // Reset to regular message.
                __instance.OnLanguageChanged();
                __instance.transform.localPosition = _radWarningPos;
                if (animation != null)
                {
                    animation.enabled = true; // Resume looping the animation
                    animation.Play();
                }
            }
        }

        /// <summary>
        /// Patch the radiation fx controller to show stronger visual effects while inside the Aurora, calculated in
        /// a helper method.
        /// </summary>
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(RadiationsScreenFXController), nameof(RadiationsScreenFXController.Update))]
        private static IEnumerable<CodeInstruction> PatchRadiationFXController(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            CodeMatcher matcher = new CodeMatcher(instructions);
            // Ensure we get an extra local variable to work with.
            LocalBuilder local = generator.DeclareLocal(typeof(float));
            // Insert our helper method at the very beginning.
            matcher.Start()
                .Insert(
                    CodeInstruction.Call(typeof(RadiationPatcher), nameof(CalculateRadiationFX)),
                    new CodeInstruction(OpCodes.Stloc_S, local))
                // Match for the vanilla variable of the player's current radiation amount.
                .MatchForward(false,
                    new CodeMatch(i => i.opcode == OpCodes.Ldsfld && ((MemberInfo)i.operand).Name.Equals("main")),
                    new CodeMatch(OpCodes.Callvirt))
                // There are four total instances of the variable access. Replace all of them with our custom variable.
                .Repeat(match =>
                {
                    // Load the variable we prepared with the helper method.
                    match.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, local));
                    match.SetInstruction(new CodeInstruction(OpCodes.Nop));
                });
            return matcher.InstructionEnumeration();
        }

        /// <summary>
        /// Check whether the player is immune to radiation whenever their equipment changes.
        /// </summary>
        public static void UpdateIsImmune(string slot, InventoryItem item)
        {
            _playerIsImmune = IsImmune();
        }
        
        #region helpers

        /// <summary>
        /// Figure out where exactly the radiation warning should go when the player is immune.
        /// </summary>
        public static void CalculateGuiPosition()
        {
            uGUI_RadiationWarning guiWarning = uGUI.main.hud.GetComponentInChildren<uGUI_RadiationWarning>();
            // Store the default position for resetting later.
            _radWarningPos = guiWarning.transform.localPosition;
            // Position the immune warning just off the right hand side.
            float screenWidth = uGUI.main.hud.GetComponent<RectTransform>().rect.width;
            float warningWidth = guiWarning.warning.GetComponent<RectTransform>().rect.width;
            float x = (screenWidth / 2f) - (warningWidth / 2f) - 15f;
            // At the same height as the compass.
            float y = uGUI.main.hud.transform.Find("Content/DepthCompass").localPosition.y;
            _radWarningImmunePos = new Vector3(x, y, 0f);
        }

        /// <summary>
        /// Calculate the strength of the radiation's visual effects.
        /// </summary>
        private static float CalculateRadiationFX()
        {
            Player player = Player.main;
            RadiationVisuals fxConfig = SaveData.Main.Config.RadiationFX;
            // This is the vanilla value.
            float playerRads = player.radiationAmount;
            // Proceed as normal if we have nothing to do.
            if (LeakingRadiation.main == null || fxConfig == RadiationVisuals.Normal)
                return playerRads;
            
            float environmentRads = 0f;
            // Smaller value for any radiation, including depth-based.
            if (IsInRadiation(player.transform, SaveData.Main.Config.RadiationDepth))
                environmentRads = 0.1f;
            // Bigger value for actually entering the danger radius.
            if (IsInRadiationRadius(player.transform))
                environmentRads = 0.2f;

            // Get values specific to higher difficulties and the inside of the Aurora.
            if (fxConfig == RadiationVisuals.Reminder)
                environmentRads = Mathf.Max(environmentRads, GetDeathrunRadiation(player));
            if (fxConfig == RadiationVisuals.Chernobyl)
                environmentRads = Mathf.Max(environmentRads, GetChernobylRadiation(player));

            return Mathf.Max(playerRads, environmentRads);
        }

        /// <summary>
        /// Get the radiation FX factor for Chernobyl radiation difficulty based on the player's position within
        /// the Aurora.
        /// </summary>
        private static float GetChernobylRadiation(Player player)
        {
            if (CrashedShipAmbientSound.main == null || !CrashedShipAmbientSound.main.isPlayerInside)
                return 0f;
            
            const StringComparison culture = StringComparison.InvariantCultureIgnoreCase;
            string biome = DeathrunUtils.GetDetailedPlayerBiome();
            float distance = LeakingRadiation.main.playerDistanceTracker.distanceToPlayer;
            float rads = 0f;
            
            // Very special cases handled first.
            if (biome.Equals("ShipInterior_PowerRoom", culture))
                rads = player.IsSwimming() ? 2f : 1.6f;
            if (biome.IndexOf("PowerCorridor", culture) >= 0)
                rads = distance <= 32f ? 1.4f : 1.2f;
            // More general cases handled here, descending down the list.
            if (rads == 0f)
            {
                foreach ((string key, float value) in RadiationFXStrength)
                {
                    // If the key is contained in the biome at all, take that as good enough and use that rad value.
                    if (biome.IndexOf(key, culture) >= 0)
                    {
                        rads = value;
                        break;
                    }
                }
            }
            
            // At high levels, things get better for every fixed leak.
            if (rads > 1f)
                rads = Mathf.Max(1f, rads * (LeakingRadiation.main.GetNumLeaks() / 11f));

            return rads;
        }
        
        /// <summary>
        /// Get the radiation FX factor for Hard/Deathrun radiation difficulty based on the player's position within
        /// the Aurora.
        /// </summary>
        private static float GetDeathrunRadiation(Player player)
        {
            if (CrashedShipAmbientSound.main == null || !CrashedShipAmbientSound.main.isPlayerInside)
                return 0f;
            
            const StringComparison culture = StringComparison.InvariantCultureIgnoreCase;
            float rads = 0f;
            
            // Try for the main power room first.
            if (DeathrunUtils.GetDetailedPlayerBiome().Equals("ShipInterior_PowerRoom", culture))
            {
                rads = player.IsSwimming() ? 0.5f : 0.4f;
            }
            else
            {
                // On this difficulty, just apply a general weak effect for anything roughly in the vicinity.
                string largeBiome = player.GetBiomeString();
                string[] fxBiomes = { "CrashZone", "CrashedShip", "ShipInterior", "GeneratorRoom" };
                if (fxBiomes.Any(b => largeBiome.IndexOf(b, culture) >= 0))
                    rads = 0.3f;
            }

            return rads;
        }

        /// <summary>
        /// Calculate the distance to a radiation source, accounting for irradiated water below the surface.
        /// </summary>
        private static float GetRadiationDistance(PlayerDistanceTracker tracker)
        {
            if (!IsSurfaceIrradiated())
                return tracker.distanceToPlayer;

            float playerDepth = Player.main.GetDepth();
            float radDepth = GetRadiationDepth(SaveData.Main.Config.RadiationDepth);
            
            // If the player is deeper than the radiation, all is well. Use vanilla behaviour.
            if (playerDepth > radDepth)
                return tracker.distanceToPlayer;

            // Treat players as inside radiation zone if within radiation depth, closer to surface being closer to
            // radiation source.
            return Mathf.Min(tracker.maxDistance * (playerDepth / radDepth), tracker.distanceToPlayer);
        }

        /// <summary>
        /// Get the current depth to which radiation penetrates the water.
        /// </summary>
        private static float GetRadiationDepth(Difficulty4 difficulty)
        {
            if (LeakingRadiation.main == null)
                return 0f;
            
            float radStrength = LeakingRadiation.main.currentRadius / LeakingRadiation.main.kMaxRadius;
            // Dissipate water radiation faster after leaks have been repaired.
            if (IsRadiationFixed())
                radStrength *= radStrength;
            return radStrength * GetMaxRadiationDepth(difficulty);
        }
        
        /// <summary>
        /// Get the maximum depth that radiation can penetrate under water based on difficulty.
        /// </summary>
        public static float GetMaxRadiationDepth(Difficulty4 difficulty)
        {
            return difficulty switch
            {
                Difficulty4.Hard => 30f,
                Difficulty4.Deathrun => 60f,
                Difficulty4.Kharaa => 200f,
                _ => 0f
            };
        }

        /// <summary>
        /// Check whether the player is currently immune to radiation.
        /// </summary>
        public static bool IsImmune()
        {
            // Consider the player immune if things have not loaded yet.
            if (Inventory.main == null)
                return true;
            Equipment equipment = Inventory.main.equipment;
            // Count the total number of radiation safe equipment the player is wearing.
            int total = RadSafeEquipment.Sum(techType => equipment.GetCount(techType));
            // If wearing any combination of 3 (Helmet, Suit, Gloves) they are safe.
            return total >= 3;
        }

        /// <summary>
        /// Check whether the given object is within the range of the Aurora's post-explosion radiation.
        /// </summary>
        public static bool IsInRadiationRadius(Transform transform)
        {
            return IsSurfaceIrradiated()
                   && (transform.position - LeakingRadiation.main.transform.position).magnitude <= LeakingRadiation.main.currentRadius;
        }

        /// <summary>
        /// Check whether the given object is in any radiation at all.
        /// </summary>
        public static bool IsInRadiation(Transform transform, Difficulty4 difficulty)
        {
            return IsSurfaceIrradiated() && GetRadiationDepth(difficulty) >= Ocean.GetDepthOf(transform);
        }

        /// <summary>
        /// Check whether the leaks in the Aurora have been fixed. Will return false even before the Aurora explodes.
        /// </summary>
        public static bool IsRadiationFixed()
        {
            if (LeakingRadiation.main == null)
                return false;
            return LeakingRadiation.main.radiationFixed;
        }
        
        /// <summary>
        /// Check whether the surface as a whole is irradiated.
        /// </summary>
        public static bool IsSurfaceIrradiated()
        {
            // If these do not exist the game is probably still loading.
            if (CrashedShipExploder.main == null || LeakingRadiation.main == null)
                return false;
            if (!CrashedShipExploder.main.IsExploded())
                return false;
            // Surface is decontaminated once leaks are fixed and radiation has completely dissipated.
            return !LeakingRadiation.main.radiationFixed || LeakingRadiation.main.currentRadius > 5f;
        }
        
        #endregion helpers
    }
}