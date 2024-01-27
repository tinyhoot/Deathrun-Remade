using DeathrunRemade.Objects.Attributes;
using DeathrunRemade.Objects.Enums;
using HarmonyLib;

namespace DeathrunRemade.Patches
{
    [HarmonyPatch]
    [PatchCategory(ApplyPatch.Always)]
    internal static class CountdownPatcher
    {
        /// <summary>
        /// Move the sunbeam countdown window when it is first shown on screen.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(uGUI_SunbeamCountdown), nameof(uGUI_SunbeamCountdown.ShowInterface))]
        private static void MoveWindowOnInit()
        {
            MoveSunbeamCountdownWindow();
        }

        /// <summary>
        /// Move the sunbeam countdown window to its desired position. Also called by any changes to the positional
        /// config options.
        /// </summary>
        public static void MoveSunbeamCountdownWindow()
        {
            // Don't do anything if the countdown does not exist or the user does not want it moved.
            if (!uGUI_SunbeamCountdown.main || !DeathrunInit._Config.MoveSunbeamWindow.Value)
                return;
            
            DeathrunUtils.SetCountdownWindowPosition(uGUI_SunbeamCountdown.main.transform,
                DeathrunInit._Config.ExplosionWindowPosX.Value, DeathrunInit._Config.ExplosionWindowPosY.Value);
        }
    }
}