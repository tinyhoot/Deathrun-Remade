using System.Reflection;
using DeathrunRemade.Handlers;
using HarmonyLib;

namespace DeathrunRemade.Objects
{
    internal struct Warning
    {
        public FieldInfo SaveField;
        public float Interval;
        public string NotificationSlot;
        public string Text;

        public static Warning AscentSpeed => new Warning
        {
            SaveField = AccessTools.Field(typeof(WarningSave), nameof(WarningSave.lastAscentWarningTime)),
            Interval = WarningHandler.MinDelay,
            NotificationSlot = NotificationHandler.TopMiddle,
            Text = "Ascending too quickly!",
        };

        public static Warning CrushDepth => new Warning
        {
            SaveField = AccessTools.Field(typeof(WarningSave), nameof(WarningSave.lastCrushDepthWarningTime)),
            Interval = 30f,
            NotificationSlot = NotificationHandler.TopMiddle,
            Text = "Personal crush depth exceeded. Return to safe depth!",
        };

        public static Warning Decompression => new Warning
        {
            SaveField = AccessTools.Field(typeof(WarningSave), nameof(WarningSave.lastDecompressionWarningTime)),
            Interval = WarningHandler.MinDelay,
            NotificationSlot = NotificationHandler.Centre,
            Text = "Decompression Warning\nDive to Safe Depth!",
        };

        public static Warning DecompressionDamage => new Warning
        {
            SaveField = AccessTools.Field(typeof(WarningSave), nameof(WarningSave.lastDecoDamageWarningTime)),
            Interval = WarningHandler.MinDelay,
            NotificationSlot = NotificationHandler.Centre,
            Text = "You have the bends! Slow your ascent!",
        };

        // public static Warning UnbreathableAir => new Warning
        // {
        //     SaveField = AccessTools.Field(typeof(WarningSave), nameof(WarningSave.lastBreathWarningTime)),
        //     Interval = 3f,
        //     Text = "",
        // };
    }
}