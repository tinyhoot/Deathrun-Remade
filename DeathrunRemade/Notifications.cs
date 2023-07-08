using HootLib.Interfaces;

namespace DeathrunRemade
{
    /// <summary>
    /// Everything that communicates directly with the player goes here.
    /// </summary>
    internal class Notifications
    {
        private ILogHandler _log;
        
        public Notifications(ILogHandler logger)
        {
            _log = logger;
        }

        /// <summary>
        /// TODO: Improve this later, for now just display a simple thing.
        /// </summary>
        public void Message(string message)
        {
            _log.InGameMessage(message);
        }
    }
}