using System.IO;
using DeathrunRemade.Configuration;
using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Enums;
using HootLib.Interfaces;

namespace DeathrunRemade.Handlers
{
    internal class WarningHandler
    {
        // Warnings are always spaced out by at least this many seconds.
        public const float MinDelay = 3f;
        public const float DisplayTime = 3f;
        public static WarningHandler Main;
        
        private Config _config;
        private ILogHandler _log;
        private NotificationHandler _notifications;
        private SaveData _save;

        public WarningHandler(Config config, ILogHandler log, NotificationHandler notificationHandler, SaveData saveData)
        {
            Main = this;
            _config = config;
            _log = log;
            _notifications = notificationHandler;
            _save = saveData;
        }

        /// <summary>
        /// Try to show the specified warning on screen.
        /// </summary>
        /// <param name="warning">The warning to show.</param>
        /// <returns>True if the warning was shown, false if not.</returns>
        public static bool ShowWarning(Warning warning)
        {
            if (Main is null)
                return false;

            return Main.ShowWarningInternal(warning);
        }

        private bool ShowWarningInternal(Warning warning)
        {
            if (_config.ShowWarnings.Value == Hints.Never)
                return false;
            _save.Warnings ??= new WarningSave();

            if (!EnoughTimePassed(warning))
                return false;
            
            _notifications.AddMessage(warning.NotificationSlot, warning.Text).SetDuration(DisplayTime);
            warning.SaveField.SetValue(_save.Warnings, DayNightCycle.main.timePassed);
            _log.Debug($"Field: {warning.SaveField.GetValue(_save.Warnings)}, time: {DayNightCycle.main.timePassed}");
            return true;
        }

        /// <summary>
        /// Check whether enough time has passed since the last time we've shown this warning to show it again.
        /// </summary>
        private bool EnoughTimePassed(Warning warning)
        {
            double lastShown = (double)warning.SaveField.GetValue(_save.Warnings);
            double now = DayNightCycle.main.timePassed;
            double delta = now - lastShown;
            
            // Always wait for at least the minimum delay.
            if (delta < MinDelay)
                return false;
            return _config.ShowWarnings.Value switch
            {
                Hints.Always => delta > warning.Interval,
                Hints.Occasional => delta > warning.Interval * 10f,
                Hints.Introductory => lastShown == 0f,
                Hints.Never => false,
                _ => throw new InvalidDataException()
            };
        }
    }
}