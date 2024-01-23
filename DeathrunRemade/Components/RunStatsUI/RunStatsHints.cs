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

        // The total number of hints/tips in the language file.
        private const int _numHints = 25;

        private void OnEnable()
        {
            // Choose a new hint every time the stats window is opened.
            ChooseNewHint();
        }

        public void ChooseNewHint()
        {
            // Inclusive lower bound, exclusive upper bound.
            int hint = Random.Range(1, _numHints + 1);
            textMesh.text = "Tip: " + Language.main.Get($"dr_hint{hint}");
        }
    }
}