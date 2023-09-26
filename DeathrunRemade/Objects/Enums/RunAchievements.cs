using System;

namespace DeathrunRemade.Objects.Enums
{
    /// <summary>
    /// Represents things the player acquires or accomplishes during a run.
    /// Some of these values are present but unused purely for easy parity with legacy deathrun.
    /// </summary>
    [Flags]
    internal enum RunAchievements
    {
        None = 0,
        Seaglide = 0x1,
        Seamoth = 0x2,
        Exosuit = 0x4,
        Cyclops = 0x8,
        AllVehicles = 0xe,
        HabitatBuilder = 0x10,
        Cured = 0x20,
        Beacon = 0x40,
        Divereel = 0x80,
        ReinforcedSuit = 0x100,
        RadiationSuit = 0x200,
        LaserCutter = 0x400,
        UltraglideFins = 0x800,
        DoubleTank = 0x1000,
        PlasteelTank = 0x2000,
        HighCapacityTank = 0x4000,
        
        WaterPark = 0x10000,
        CuteFish = 0x20000,
        
        PurpleTablet = 0x100000,
        OrangeTablet = 0x200000,
        BlueTablet = 0x400000,
    }

    internal static class RunAchievementsExtensions
    {
        /// <summary>
        /// Check whether the player has managed to unlock a specific flag.
        /// </summary>
        public static bool IsUnlocked(this RunAchievements achievements, RunAchievements flag)
        {
            return (achievements & flag) == flag;
        }
        
        /// <summary>
        /// Return a new achievement enum with the specified flag unlocked.
        /// </summary>
        public static RunAchievements Unlock(this RunAchievements achievements, RunAchievements unlock)
        {
            return achievements | unlock;
        }
    }
}