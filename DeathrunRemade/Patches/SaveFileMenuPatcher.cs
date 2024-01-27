using DeathrunRemade.Objects.Attributes;
using DeathrunRemade.Objects.Enums;
using HarmonyLib;
using HootLib;
using UnityEngine;
using UnityEngine.UI;

namespace DeathrunRemade.Patches
{
    /// <summary>
    /// Responsible for hooking into the appearance of the save files visible in the main menu.
    /// </summary>
    [HarmonyPatch]
    [PatchCategory(ApplyPatch.Always)]
    internal static class SaveFileMenuPatcher
    {
        private static Color _disabledSaveTint = new Color(0.7f, 0.45f, 0.45f, 0.45f);
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MainMenuLoadPanel), nameof(MainMenuLoadPanel.UpdateLoadButtonState))]
        private static void AddDeathrunSaveSlotInfo(MainMenuLoadButton lb)
        {
            DeathrunInit._Log.Debug($"Looking for existing Deathrun data in slot {lb.saveGame}");
            if (Hootils.HasExistingSaveData(lb.saveGame, DeathrunInit.NAME.Replace(" ", "")))
            {
                // lb.saveGameModeText.SetText("Deathrun");
            }
            else
            {
                DeathrunInit._Log.Debug($"{lb.saveGame} does not have Deathrun save data!");
                // Make the save file look a little muted in colour.
                lb.load.GetComponent<Image>().color = _disabledSaveTint;
                // Clicking it no longer does anything.
                lb.loadButton.SetActive(false);
                // Finally, add a tooltip while keeping the deletion dialog tooltip-free.
                var tooltip = lb.load.EnsureComponent<MenuTooltip>();
                tooltip.key = "Non-Deathrun saves cannot be loaded while Deathrun is active.";
            }
        }
    }
}