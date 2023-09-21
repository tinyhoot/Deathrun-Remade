using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Enums;

namespace DeathrunRemade.Handlers
{
    internal class TutorialHandler
    {
        private NotificationHandler _notifications;
        private SaveData _saveData;
        private static TutorialHandler _instance;

        public TutorialHandler(NotificationHandler notifications, SaveData saveData)
        {
            _instance = this;
            
            _notifications = notifications;
            _saveData = saveData;
        }
        
        /// <summary>
        /// Trigger the specified tutorial hint. Will not fire if it has already been seen or tutorials are disabled.
        /// </summary>
        public static bool TriggerTutorial(Tutorial tutorial)
        {
            return _instance?.TriggerTutorialInternal(tutorial) ?? false;
        }
        
        private bool TriggerTutorialInternal(Tutorial tutorial)
        {
            // Don't do anything if tutorials are disabled.
            if (!_saveData.Config.ShowTutorials)
                return false;
            switch (tutorial)
            {
                case Tutorial.ExosuitVehicleExitPowerLoss:
                    return ExosuitVehicleExitTutorial();
                case Tutorial.SeamothVehicleExitPowerLoss:
                    return SeamothVehicleExitTutorial();
                default:
                    return false;
            }
        }
        
        private bool ExosuitVehicleExitTutorial()
        {
            if (_saveData.Tutorials.exosuitVehicleExitCosts)
                return false;

            _notifications.AddMessage(NotificationHandler.Centre, "Although more efficient than the Seamoth, "
                                                                  + "the Prawn suit\nstill draws power when exited at depth.");
            _saveData.Tutorials.exosuitVehicleExitCosts = true;
            return true;
        }

        private bool SeamothVehicleExitTutorial()
        {
            if (!_saveData.Config.ShowTutorials || _saveData.Tutorials.seamothVehicleExitCosts)
                return false;

            _notifications.AddMessage(NotificationHandler.Centre, "Exiting the Seamoth underwater causes "
                                                                  + "battery drain.\nExit at surface or Moonpool for "
                                                                  + "optimum power use.");
            _saveData.Tutorials.seamothVehicleExitCosts = true;
            return true;
        }
    }
}