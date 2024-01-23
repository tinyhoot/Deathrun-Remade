using TMPro;
using UnityEngine;

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
    }
}