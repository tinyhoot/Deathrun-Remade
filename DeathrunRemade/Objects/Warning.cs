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
        public string Key;

        public static Warning AscentSpeed => new Warning
        {
            SaveField = AccessTools.Field(typeof(WarningSave), nameof(WarningSave.lastAscentWarningTime)),
            Interval = WarningHandler.MinDelay,
            NotificationSlot = NotificationHandler.TopMiddle,
            Key = "dr_warn_fastAscent",
        };

        public static Warning CrushDepth => new Warning
        {
            SaveField = AccessTools.Field(typeof(WarningSave), nameof(WarningSave.lastCrushDepthWarningTime)),
            Interval = 30f,
            NotificationSlot = NotificationHandler.TopMiddle,
            Key = "dr_warn_crushImminent",
        };

        public static Warning CrushDamage => new Warning
        {
            SaveField = AccessTools.Field(typeof(WarningSave), nameof(WarningSave.lastCrushDamageWarningTime)),
            Interval = WarningHandler.MinDelay,
            NotificationSlot = NotificationHandler.TopMiddle,
            Key = "dr_warn_crushDamage",
        };

        public static Warning Decompression => new Warning
        {
            SaveField = AccessTools.Field(typeof(WarningSave), nameof(WarningSave.lastDecompressionWarningTime)),
            Interval = WarningHandler.MinDelay,
            NotificationSlot = NotificationHandler.Centre,
            Key = "dr_warn_bendsImminent",
        };

        public static Warning DecompressionDamage => new Warning
        {
            SaveField = AccessTools.Field(typeof(WarningSave), nameof(WarningSave.lastDecoDamageWarningTime)),
            Interval = WarningHandler.MinDelay,
            NotificationSlot = NotificationHandler.Centre,
            Key = "dr_warn_bendsDamage",
        };

        public static Warning UnbreathableAir => new Warning
        {
            SaveField = AccessTools.Field(typeof(WarningSave), nameof(WarningSave.lastBreathWarningTime)),
            Interval = 30f,
            NotificationSlot = NotificationHandler.Vanilla,
            Key = "dr_warn_unbreathableAir"
        };
    }
}