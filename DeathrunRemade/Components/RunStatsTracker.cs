using System.Collections.Generic;
using DeathrunRemade.Configuration;
using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Enums;
using HarmonyLib;
using HootLib.Objects;
using Nautilus.Handlers;
using UnityEngine;

namespace DeathrunRemade.Components
{
    /// <summary>
    /// A tracker which updates the run stats periodically.
    /// This component is added to the player's GameObject, meaning it awakes and dies while in-game.
    /// </summary>
    [HarmonyPatch]
    internal class RunStatsTracker : MonoBehaviour
    {
        // By the time this component awakes, the savedata should have been initialised for a long time already.
        // Passing by reference because RunStats is a value type.
        private ref RunStats _stats => ref SaveData.Main.Stats;
        private Player _player;
        private Hootimer _timer;
        
        private const string GoalPrefix = "deathrun_";
        private readonly Dictionary<string, RunAchievements> _goals = new Dictionary<string, RunAchievements>
        {
            { GoalPrefix + nameof(TechType.Seaglide), RunAchievements.Seaglide },
            { GoalPrefix + nameof(TechType.Seamoth), RunAchievements.Seamoth },
            { GoalPrefix + nameof(TechType.Exosuit), RunAchievements.Exosuit },
            { GoalPrefix + nameof(TechType.Cyclops), RunAchievements.Cyclops },
            { GoalPrefix + nameof(TechType.Builder), RunAchievements.HabitatBuilder },
            { GoalPrefix + nameof(TechType.ReinforcedDiveSuit), RunAchievements.ReinforcedSuit },
            { GoalPrefix + nameof(TechType.RadiationSuit), RunAchievements.RadiationSuit },
            { GoalPrefix + nameof(TechType.LaserCutter), RunAchievements.LaserCutter },
            { GoalPrefix + nameof(TechType.UltraGlideFins), RunAchievements.UltraglideFins },
            { GoalPrefix + nameof(TechType.DoubleTank), RunAchievements.DoubleTank },
            { GoalPrefix + nameof(TechType.PlasteelTank), RunAchievements.PlasteelTank },
            { GoalPrefix + nameof(TechType.HighCapacityTank), RunAchievements.HighCapacityTank },
            { GoalPrefix + nameof(TechType.Cutefish), RunAchievements.CuteFish },
            { GoalPrefix + nameof(TechType.PrecursorKey_Purple), RunAchievements.PurpleTablet },
            { GoalPrefix + nameof(TechType.PrecursorKey_Orange), RunAchievements.OrangeTablet },
            { GoalPrefix + nameof(TechType.PrecursorKey_Blue), RunAchievements.BlueTablet },
        };

        private void Awake()
        {
            _player = GetComponent<Player>();
            // Only update the stats in Update() periodically rather than every frame.
            _timer = new Hootimer(() => Time.deltaTime, 3f);
            RegisterEvents();
        }

        private void Update()
        {
            if (!SaveData.Main.Ready)
                return;
            if (!_timer.Tick())
                return;
            
            float depth = _player.GetDepth();
            if (depth > _stats.depthReached)
                _stats.depthReached = depth;
            
            // No using PDA to stall out the timer.
            _stats.time = DayNightCycle.main.timePassed;
        }
        
        /// <summary>
        /// Prepare a stats instance for a new game.
        /// </summary>
        public static void InitStats(ref RunStats stats, ConfigSave config, int id)
        {
            stats.config = config;
            stats.startPoint = config.StartLocation;
            stats.gameMode = GameModeUtils.currentGameMode;
            stats.id = id;
            stats.version = DeathrunInit.VERSION;
        }

        private void RegisterEvents()
        {
            // Register a listener for when the player cures their infection.
            StoryGoalHandler.RegisterCustomEvent("Infection_Progress5", OnCure);

            RegisterItemGoal(TechType.Seaglide);
            RegisterItemGoal(TechType.Exosuit);
            RegisterItemGoal(TechType.Cyclops);
            RegisterItemGoal(TechType.Builder);
            RegisterItemGoal(TechType.ReinforcedDiveSuit);
            RegisterItemGoal(TechType.RadiationSuit);
            RegisterItemGoal(TechType.LaserCutter);
            RegisterItemGoal(TechType.UltraGlideFins);
            RegisterItemGoal(TechType.DoubleTank);
            RegisterItemGoal(TechType.PlasteelTank);
            RegisterItemGoal(TechType.HighCapacityTank);
            RegisterItemGoal(TechType.Cutefish);
            RegisterItemGoal(TechType.PrecursorKey_Purple);
            RegisterItemGoal(TechType.PrecursorKey_Orange);
            RegisterItemGoal(TechType.PrecursorKey_Blue);
        }

        private void RegisterItemGoal(TechType techType)
        {
            StoryGoalHandler.RegisterItemGoal(GoalPrefix + techType.AsString(), Story.GoalType.Story, techType);
            StoryGoalHandler.RegisterCustomEvent(OnItemGoalUnlock);
        }

        private void OnCure()
        {
            _stats.achievements = _stats.achievements.Unlock(RunAchievements.Cured);
        }

        /// <summary>
        /// Called when an ItemGoal unlocks, i.e. the player crafts or otherwise acquires that item.
        /// </summary>
        private void OnItemGoalUnlock(string goalName)
        {
            if (!_goals.TryGetValue(goalName, out RunAchievements unlock))
                return;
            _stats.achievements = _stats.achievements.Unlock(unlock);
        }

        /// <summary>
        /// Track the breeding of specific fish.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CreatureEgg), nameof(CreatureEgg.Hatch))]
        private static void NotifyFishBreeding(CreatureEgg __instance)
        {
            if (__instance.creatureType == TechType.Cutefish)
                SaveData.Main.Stats.achievements = SaveData.Main.Stats.achievements.Unlock(RunAchievements.CuteFish);
        }

        /// <summary>
        /// Track the construction of water parks. Works for both regular and large room variants.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(WaterPark), nameof(WaterPark.Spawn))]
        private static void NotifyWaterParkConstructed()
        {
            SaveData.Main.Stats.achievements = SaveData.Main.Stats.achievements.Unlock(RunAchievements.WaterPark);
        }
    }
}