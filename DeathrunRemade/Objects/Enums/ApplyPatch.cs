using System;

namespace DeathrunRemade.Objects.Enums
{
    public enum ApplyPatch
    {
        /// <summary>
        /// Never apply this patch. Unused but provides a default value for the enum.
        /// </summary>
        Never,

        /// <summary>
        /// Always patch this method. Do not unpatch it on reaching the main menu.
        /// </summary>
        Always,

        /// <summary>
        /// Only patch this method when needed. Unpatch as soon as we leave an active game.
        /// </summary>
        Config
    }

    public static class PatchTimingExtensions
    {
        public static string AsString(this ApplyPatch timing)
        {
            return timing switch
            {
                ApplyPatch.Never => "Never",
                ApplyPatch.Always => "Always",
                ApplyPatch.Config => "Config",
                _ => throw new ArgumentOutOfRangeException(nameof(timing), timing, null)
            };
        }
    }
}