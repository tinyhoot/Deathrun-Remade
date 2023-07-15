namespace DeathrunRemade
{
    /// <summary>
    /// General utilities which just didn't feel like they fit anywhere else.
    /// </summary>
    public static class DeathrunUtils
    {
        /// <summary>
        /// Check whether the player is inside any kind of player-made base, escape pod or vehicle.
        /// </summary>
        public static bool IsInsideSubOrVehicle(this Player player)
        {
            return player.IsInSub() || player.GetVehicle() != null || player.currentEscapePod != null;
        }

        /// <summary>
        /// Check whether the player is inside any kind of player-made base, escape pod or vehicle
        /// and whether that place is also powered.
        /// </summary>
        public static bool IsInsidePoweredSubOrVehicle(this Player player)
        {
            if (player.currentEscapePod)
                return true;
            if (player.IsInSub())
                return player.currentSub.powerRelay != null && player.currentSub.powerRelay.IsPowered();
            return player.IsInsidePoweredVehicle();
        }
    }
}