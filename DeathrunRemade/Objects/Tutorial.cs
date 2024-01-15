using DeathrunRemade.Handlers;
using Nautilus.Handlers;

namespace DeathrunRemade.Objects
{
    internal struct Tutorial
    {
        public string Key;
        public string SlotId;
        public string Text;

        public Tutorial(string key, string slotId, string text)
        {
            Key = key;
            SlotId = slotId;
            Text = text;
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
            if (saveData.Config.ShowTutorials)
                return false;
            // Do not trigger tutorials twice.
            if (saveData.Tutorials.completedTutorials.Contains(Key))
                return false;

            notifications.AddMessage(SlotId, Text);
            saveData.Tutorials.completedTutorials.Add(Key);
            return true;
        }

        /// <summary>
        /// Register events for tutorials which do not need to be triggered under overly complex conditions.
        /// </summary>
        public static void RegisterEvents()
        {
            StoryGoalHandler.RegisterCustomEvent(LeakingRadiation.main.leaksFixedGoal.key,
                () => AuroraRepairedBreathable.Trigger());
        }

        public static Tutorial AuroraRepairedBreathable => new Tutorial
        {
            Key = "AuroraRepairedBreathable",
            SlotId = NotificationHandler.Centre,
            Text = "The air filtration system roars to life!"
        };

        public static Tutorial ExosuitVehicleExitPowerLoss => new Tutorial
        {
            Key = "ExosuitVehicleExitPowerLoss",
            SlotId = NotificationHandler.Centre,
            Text = "Although more efficient than the Seamoth, the Prawn suit\n"
                   + "still draws power when exited at depth."
        };
        
        public static Tutorial SeamothVehicleExitPowerLoss => new Tutorial
        {
            Key = "SeamothVehicleExitPowerLoss",
            SlotId = NotificationHandler.Centre,
            Text = "Exiting the Seamoth underwater causes battery drain.\n"
                   + "Exit at surface or Moonpool for optimal power use."
        };
    }
}