namespace DeathrunRemade
{
    /// <summary>
    /// Any constant value that is needed by more than one class is put in here.
    /// </summary>
    public static class Constants
    {
        // Do not assign a prefix in debug builds, which makes the items easier to spawn in and test but more at risk
        // of clashes with other mods.
#if DEBUG
        public const string ClassIdPrefix = "";
#else
        public const string ClassIdPrefix = "deathrunremade_";
#endif
        public const string WorkbenchSuitTab = ClassIdPrefix + "specialsuits";
        public const string WorkbenchTankTab = ClassIdPrefix + "specialtanks";
        
        public const float InfiniteCrushDepth = 10000f;
        public const float MinTemperatureLimit = 49f;
    }
}