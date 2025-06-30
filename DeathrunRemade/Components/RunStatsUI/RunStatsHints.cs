using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DeathrunRemade.Components.RunStatsUI
{
    /// <summary>
    /// A component used in unity editor to control the hints shown in the highscores window.
    /// Enables/disables hints and swaps in a different one every so often.
    /// </summary>
    internal class RunStatsHints : MonoBehaviour
    {
        // Set in the editor.
#pragma warning disable CS0649
        public TextMeshProUGUI textMesh;
#pragma warning restore CS0649

        private const string HintKey = "dr_hint";
        private int _maxNumHints = 0;
        private Language _language;

        private void Awake()
        {
            _language = Language.main;
            UpdateMaxNumHints();
            Language.OnLanguageChanged += UpdateMaxNumHints;
        }

        private void OnEnable()
        {
            // Do not show these if the user does not want them.
            textMesh.gameObject.SetActive(DeathrunInit._Config.ShowHints.Value);
            // Choose a new hint every time the stats window is opened.
            ChooseNewHint();
        }

        public void ChooseNewHint()
        {
            // Inclusive lower bound, exclusive upper bound.
            int hint = Random.Range(1, _maxNumHints + 1);
            textMesh.text = _language.Get("dr_scoresui_hint") + _language.Get($"{HintKey}{hint}");
        }

        /// <summary>
        /// Get the maximum number of available hints in the currently active language.
        /// Not too happy with this, but it's better than hard coding a constant somewhere.
        /// </summary>
        private void UpdateMaxNumHints()
        {
            int hints = 1;
            while (_language.Contains($"{HintKey}{hints}"))
            {
                hints++;
            }

            if (hints <= 1)
                throw new KeyNotFoundException($"There are no hint messages in the currently loaded language '{_language.currentLanguage}'");

            _maxNumHints = hints - 1;
        }
    }
}