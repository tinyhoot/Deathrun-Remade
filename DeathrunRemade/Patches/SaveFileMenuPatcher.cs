using System.Collections;
using System.Collections.Generic;
using DeathrunRemade.Handlers;
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
        private static Dictionary<string, bool> _slotSaveData = new();

        /// <summary>
        /// Check each save game for existing Deathrun save data.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveLoadManager), nameof(SaveLoadManager.LoadSlotsAsync))]
        [HarmonyPatch(new [] {typeof(UserStorage), typeof(Dictionary<string, SaveLoadManager.GameInfo>), typeof(IOut<SaveLoadManager.LoadResult>)})]
        private static IEnumerator CheckForExistingSaveData(IEnumerator passthrough, UserStorage userStorage, Dictionary<string, SaveLoadManager.GameInfo> infoCache)
        {
            // Make sure we don't carry over any old, outdated data.
            _slotSaveData.Clear();
            yield return passthrough;
            
            DeathrunInit._Log.Debug("Checking for existing Deathrun save data.");
            foreach (string slotName in infoCache.Keys)
            {
                var result = new TaskResult<bool>();
                // This is essentially another load operation like the SaveLoadManager just did. It is possible that this
                // increases main menu load times substantially on weaker machines with many save games.
                yield return Hootils.HasExistingSaveData(userStorage, slotName, DeathrunInit.NAME, result);
                DeathrunInit._Log.Debug($"{slotName}: {result.Get()}");
                _slotSaveData.Add(slotName, result.Get());
            }
        }
        
        /// <summary>
        /// Disable the buttons for loading a saved game if we have previously determined they were not made with
        /// Deathrun.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MainMenuLoadPanel), nameof(MainMenuLoadPanel.UpdateLoadButtonState))]
        private static void AddDeathrunSaveSlotInfo(MainMenuLoadButton lb)
        {
            if (!_slotSaveData.TryGetValue(lb.saveGame, out bool hasSaveData))
            {
                DeathrunInit._Log.Warn($"Slot {lb.saveGame} was not in cached slot data!");
                return;
            }
            
            if (!hasSaveData)
                DisableSaveSlot(lb, "dr_savefile_invalid");
        }

        private static void DisableSaveSlot(MainMenuLoadButton slotButton, string tooltip)
        {
            // Make the save file look a little muted in colour.
            slotButton.load.GetComponent<Image>().color = _disabledSaveTint;
            // Clicking it no longer does anything.
            slotButton.loadButton.SetActive(false);

            // Finally, add a tooltip while keeping the deletion dialog tooltip-free.
            var menuTooltip = slotButton.load.EnsureComponent<MenuTooltip>();
            menuTooltip.key = LocalisationHandler.Get(tooltip);
        }
    }
}