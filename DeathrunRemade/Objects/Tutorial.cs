using System.Collections.Generic;
using DeathrunRemade.Handlers;

namespace DeathrunRemade.Objects
{
    internal struct Tutorial
    {
        public string Key;
        public string SlotId;

        public Tutorial(string key, string slotId)
        {
            Key = key;
            SlotId = slotId;
        }

        /// <inheritdoc cref="Trigger(NotificationHandler, SaveData)"/>
        public bool Trigger()
        {
            return Trigger(NotificationHandler.Main, SaveData.Main);
        }

        /// <summary>
        /// Trigger the specified tutorial. Whether it actually executes depends on settings.
        /// </summary>
        /// <returns>True if the tutorial actually fired, false if it did not.</returns>
        public bool Trigger(NotificationHandler notifications, SaveData saveData)
        {
            // Don't do anything if tutorials are disabled.
            if (DeathrunInit._Config.ShowTutorials.Value)
                return false;
            // Do not trigger tutorials twice.
            if (saveData.Tutorials.completedTutorials.Contains(Key))
                return false;

            notifications.AddMessage(SlotId, Key);
            saveData.Tutorials.completedTutorials ??= new HashSet<string>();
            saveData.Tutorials.completedTutorials.Add(Key);
            return true;
        }

        public static Tutorial ExosuitVehicleExitPowerLoss => new Tutorial
        {
            Key = "dr_tut_exosuitVehicleExitPowerLoss",
            SlotId = NotificationHandler.Centre
        };
        
        public static Tutorial SeamothVehicleExitPowerLoss => new Tutorial
        {
            Key = "dr_tut_seamothVehicleExitPowerLoss",
            SlotId = NotificationHandler.Centre
        };
    }
}