using System;
using System.Collections.Generic;
using System.Linq;
using DeathrunRemade.Configuration;
using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Enums;
using UnityEngine;
using ILogHandler = HootLib.Interfaces.ILogHandler;

namespace DeathrunRemade.Handlers
{
    /// <summary>
    /// Responsible for dealing with the stats of individual runs and calculating and updating scores.
    ///
    /// I'm not 100% happy with how the calculation happens. Perhaps it might be worth it to subclass config entries
    /// and then associate each config entry <em>directly</em> with the resulting multiplier/bonus on registration?
    /// That would also ensure the scoring doesn't go forgotten if a new option is added in an update.
    ///
    /// Basic idea is that a perfect run should gain ~60k points before any multipliers or challenge bonuses.
    /// -> 5000 from depth
    /// -> 10000 from time
    /// -> 30000 from achievements
    /// -> 20000 from victory
    /// </summary>
    internal class ScoreHandler
    {
        // Score given for every "segment" of time survived.
        private const float TimeScoreBase = 2000f;
        // Score awarded for reaching the very bottom.
        private const float MaxDepthBonus = 5000f;
        // Best-case score awarded for the fastest possible victory.
        private const float VictoryBonus = 20000f;
        private const float VictoryGraceHours = 5f;
        private const float VictoryMaxHours = 15f;
        // The additive score multipliers for each difficulty level.
        private const float HardMult = 0.1f;
        private const float DeathrunMult = 0.2f;
        private const float KharaaMult = 0.3f;
        // The flat bonuses for challenge settings.
        private const float SmallBonus = 1000f;
        private const float BigBonus = 2000f;
        private const float HardcoreBonus = 10000f;
        // After this many deaths the score cannot decrease any further (i.e. lose "all" points).
        private const int DeathsForMaxMalus = 7;
        // The minimum portion of your score you always retain even when racking up hundreds of deaths.
        private const float DeathMultFloor = 0.3f;
        // The extra score given for obtaining or managing certain things.
        // Points are reduced compared to legacy, but they do get multiplied later. Adds up to ~30000 points.
        private readonly Dictionary<RunAchievements, float> _achievementRewards = new Dictionary<RunAchievements, float>
        {
            { RunAchievements.Seaglide, 500f },
            { RunAchievements.Seamoth, 1000f },
            { RunAchievements.Exosuit, 2500f },
            { RunAchievements.Cyclops, 5000f },
            { RunAchievements.HabitatBuilder, 1000f },
            { RunAchievements.Cured, 10000f },
            { RunAchievements.ReinforcedSuit, 500f },
            { RunAchievements.RadiationSuit, 250f },
            { RunAchievements.UltraglideFins, 250f },
            { RunAchievements.DoubleTank, 100f },
            { RunAchievements.PlasteelTank, 250f },
            { RunAchievements.HighCapacityTank, 250f },
            { RunAchievements.WaterPark, 500f },
            { RunAchievements.CuteFish, 2500f },
            { RunAchievements.PurpleTablet, 500f },
            { RunAchievements.OrangeTablet, 1000f },
            { RunAchievements.BlueTablet, 2500f },
        };
        private ILogHandler _log;
        
        public ScoreHandler(ILogHandler log)
        {
            _log = log;
        }

        /// <summary>
        /// Compare two runs against each other to figure out which one was better.
        /// </summary>
        /// <returns>Less than zero if run1 was worse than run2, zero if they were equal, greater than zero if run1
        /// was better than run2.</returns>
        /// <seealso cref="Comparison{T}"/>
        public static int CompareRuns(RunStats run1, RunStats run2)
        {
            // Score is the primary deciding factor.
            if ((int)run1.scoreTotal != (int)run2.scoreTotal)
                return (int)(run1.scoreTotal - run2.scoreTotal);
            
            // If scores are equal, use the difficulty multiplier.
            if (!Mathf.Approximately(run1.scoreMult, run2.scoreMult))
                return (int)(run1.scoreMult - run2.scoreMult);
            // Or maybe victory?
            if (run1.victory != run2.victory)
                return (int)(run1.victory.ToFloat() - run2.victory.ToFloat());
            // Deaths??
            if (run1.deaths != run2.deaths)
                return run2.deaths - run1.deaths;
            // Depth or just give up.
            return (int)(run1.depthReached - run2.depthReached);
        }

        /// <summary>
        /// Recalculate and update the scoring for the given run.
        /// </summary>
        /// <returns>The total score for the run.</returns>
        public void UpdateScore(ref RunStats stats)
        {
            _log.Debug($"Updating score for run with id {stats.id}");
            stats.scoreBase = CalculateScoreBase(stats);
            // Offset vehicle achievements for no vehicle runs.
            if (stats.victory && stats.achievements.IsUnlocked(RunAchievements.AllVehicles))
                stats.scoreBase += GetNoVehicleChallengeBonus();
            _log.Debug($"Base score: {stats.scoreBase}");
            
            stats.scoreMult = stats.isLegacy ? CalculateLegacyScoreMult(stats.legacySettingsCount) : CalculateScoreMultiplier(stats.config);
            _log.Debug($"Multiplier: {stats.scoreMult}");
            
            stats.scoreBonus = CalculateScoreBonus(stats.config);
            _log.Debug($"Bonus: {stats.scoreBonus}");
            // Extra bonus for finishing the game.
            stats.scoreBonus += CalculateVictoryScore(stats.victory, (float)stats.time);
            // Extra bonus for playing on hardcore.
            if ((stats.gameMode & GameModeOption.Hardcore) == GameModeOption.Hardcore)
                stats.scoreBonus += HardcoreBonus;
            _log.Debug($"With victory and hardcore: {stats.scoreBonus}");

            float total = (stats.scoreBase * stats.scoreMult) + stats.scoreBonus;
            total *= GetDeathMultiplier(stats.deaths);
            _log.Debug($"Total: {total}");
            
            // The formatting on the highscore window hates too many digits. This high a score should be unreachable
            // anyway but it's good to make sure.
            stats.scoreTotal = Mathf.Min(total, 999999f);
        }

        /// <summary>
        /// Get the baseline score earned by the player on the given run.
        /// </summary>
        public float CalculateScoreBase(RunStats stats)
        {
            // Time score. Diminishes greatly as time goes on. Points for every log-base 2 of hours lived.
            // E.g. 1 hour = 1, 2 hours = 2, 4 hours = 3, 8 hours = 4, etc.
            float hours = (float)(stats.time / 3600);
            float adjustedHours = Mathf.Log(hours + 1, 2f);
            float timeScore = adjustedHours * TimeScoreBase;
            _log.Debug($"--Time lived: {timeScore}");
            float achievements = CalculateAchievementScore(stats.achievements);
            _log.Debug($"--Achievements: {achievements}");
            // Depth score is linear function of how deep the player managed to get out of the total possible.
            float depthMult = Mathf.Clamp(stats.depthReached, 0f, 1600f) / 1600f;
            float depthScore = depthMult * MaxDepthBonus;
            _log.Debug($"--Depth: {depthScore}");
            
            return timeScore + achievements + depthScore;
        }

        /// <summary>
        /// Get the flat bonus the player earns for their config settings.
        /// </summary>
        public float CalculateScoreBonus(ConfigSave config)
        {
            float bonus = 0f;

            bonus += config.NoVehicleChallenge ? BigBonus : 0f;
            bonus += config.FarmingChallenge switch
            {
                Difficulty3.Hard => SmallBonus,
                Difficulty3.Deathrun => BigBonus,
                _ => 0f
            };
            bonus += config.FilterPumpChallenge switch
            {
                Difficulty3.Hard => SmallBonus,
                Difficulty3.Deathrun => BigBonus,
                _ => 0f
            };
            bonus += config.FoodChallenge switch
            {
                DietPreference.Pescatarian => SmallBonus,
                DietPreference.Vegetarian => SmallBonus,
                DietPreference.Vegan => BigBonus,
                _ => 0f
            };
            bonus += config.IslandFoodChallenge switch
            {
                RelativeToExplosion.BeforeAndAfter => SmallBonus,
                RelativeToExplosion.After => SmallBonus,
                RelativeToExplosion.Never => BigBonus,
                _ => 0f
            };
            bonus += config.PacifistChallenge ? BigBonus : 0f;

            return bonus;
        }

        /// <summary>
        /// Get the overall multiplier the player earns for their config settings.
        /// <br /><br />
        /// <list type="bullet">
        /// <listheader>Some settings were intentionally ignored: </listheader>
        /// <item>Special Air Tanks</item>
        /// <item>Topple Lifepod</item>
        /// <item>All challenges (those get flat bonuses)</item>
        /// <item>All UI options</item>
        /// </list>
        /// </summary>
        public float CalculateScoreMultiplier(ConfigSave config)
        {
            // The absolute worst the player can get is 1. No sticks, only carrots.
            float total = 1f;

            total += GetStandardMult(config.PersonalCrushDepth);
            // No bonus at all for "LoveTaps".
            total += GetStandardMult(config.DamageTaken);
            // The bends are super impactful. Reflect that in the scoring.
            total += GetStandardMult(config.NitrogenBends) * 2f;
            total += GetStandardMult(config.SurfaceAir);
            total += config.StartLocation.Equals("Vanilla") ? 0f : DeathrunMult;
            total += config.SinkLifepod ? DeathrunMult : 0f;
            total += GetStandardMult(config.CreatureAggression);
            total += config.WaterMurkiness switch
            {
                Murkiness.Dark => HardMult,
                Murkiness.Darker => DeathrunMult,
                Murkiness.Darkest => KharaaMult,
                _ => 0f
            };
            total += GetStandardMult(config.ExplosionDepth);
            total += config.ExplosionTime switch
            {
                Timer.Medium => HardMult,
                Timer.Short => DeathrunMult,
                _ => 0f
            };
            total += GetStandardMult(config.RadiationDepth);
            total += config.RadiationFX switch
            {
                RadiationVisuals.Reminder => HardMult,
                RadiationVisuals.Chernobyl => DeathrunMult,
                _ => 0f
            };
            total += GetStandardMult(config.BatteryCapacity) / 2f;
            total += GetStandardMult(config.BatteryCosts) / 2f;
            total += GetStandardMult(config.ToolCosts);
            total += GetStandardMult(config.PowerCosts);
            total += GetStandardMult(config.ScansRequired);
            total += GetStandardMult(config.VehicleCosts);
            // Super difficult, so extra score.
            if (config.NoVehicleChallenge)
                total += DeathrunMult * 2f;
            total += GetStandardMult(config.VehicleExitPowerLoss);

            return total;
        }

        /// <summary>
        /// Get a rough approximation of what the modern-day multiplier would have been in a legacy run.
        ///
        /// This is not accurate at all and will tend to give legacy runs lower scores than modern ones, but that's
        /// okay so long as it feels fair/believable. Beating your old runs should be the goal.
        /// </summary>
        private float CalculateLegacyScoreMult(int deathRunSettingCount)
        {
            return 1 + (deathRunSettingCount * HardMult);
        }

        /// <summary>
        /// Calculate the score awarded for all the things the player has achieved in this run.
        /// </summary>
        private float CalculateAchievementScore(RunAchievements achievements)
        {
            return _achievementRewards
                .Where(kvpair => achievements.IsUnlocked(kvpair.Key))
                .Sum(kvpair => kvpair.Value);
        }

        /// <summary>
        /// Calculate the victory score awarded based on how long it took to achieve. Shorter runs are worth more.
        /// </summary>
        private float CalculateVictoryScore(bool victory, float time)
        {
            if (!victory)
                return 0f;

            float hoursTaken = time / 3600f;
            // There is a grace period before the score starts to taper off.
            hoursTaken -= VictoryGraceHours;
            float malus = hoursTaken / (VictoryMaxHours - VictoryGraceHours);
            malus = Mathf.Clamp01(malus);
            // The player can "lose" up to half the maximum victory score reward based on time.
            return VictoryBonus - (VictoryBonus * 0.5f * malus);
        }

        /// <summary>
        /// Get the (score-reducing) multiplier for the number of times the player died. Punishing at first but
        /// increases more slowly with more and more deaths. The first death is free!
        /// </summary>
        private float GetDeathMultiplier(int deaths)
        {
            // Reaches 0 after $constant number of deaths.
            float malus = 1 - Mathf.Log(deaths, DeathsForMaxMalus);
            return Mathf.Clamp(malus, DeathMultFloor, 1f);
        }

        private float GetStandardMult<TEnum>(TEnum setting) where TEnum : Enum
        {
            return setting.ToString() switch
            {
                "Hard" => HardMult,
                "Deathrun" => DeathrunMult,
                "Kharaa" => KharaaMult,
                _ => 0f
            };
        }

        /// <summary>
        /// Convenience method to get the score needed for the no vehicle challenge to offset vehicle achievements.
        /// </summary>
        private float GetNoVehicleChallengeBonus()
        {
            float score = _achievementRewards[RunAchievements.Seamoth];
            score += _achievementRewards[RunAchievements.Exosuit];
            score += _achievementRewards[RunAchievements.Cyclops];
            score += BigBonus;
            return score;
        }
    }
}