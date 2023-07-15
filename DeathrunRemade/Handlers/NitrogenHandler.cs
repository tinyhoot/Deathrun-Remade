using System;
using DeathrunRemade.Objects.Enums;

namespace DeathrunRemade.Handlers
{
    /// <summary>
    /// Responsible for the math behind nitrogen and the bends.
    /// </summary>
    internal class NitrogenHandler
    {
        public event Action OnSafeDepthEnabled;
        public event Action OnSafeDepthDisabled;

        public NitrogenHandler()
        {
            
        }

        public void Update()
        {
            
        }
        
        /// <summary>
        /// Check whether the given depth is close enough to the safe depth to warrant emitting a warning.
        /// </summary>
        public static bool IsApproachingSafeDepth(float depth, float safeDepth)
        {
            // Extremely naive for now.
            return depth - safeDepth < 10;
        }

        /// <summary>
        /// Calculate the player's current standing when it comes to safe depth.
        /// </summary>
        public static SafeDepthStatus CalculateDepthStatus(float depth, float safeDepth)
        {
            if (depth < safeDepth)
                return SafeDepthStatus.Exceeded;
            if (IsApproachingSafeDepth(depth, safeDepth))
                return SafeDepthStatus.Approaching;
            return SafeDepthStatus.Safe;
        }
    }
}