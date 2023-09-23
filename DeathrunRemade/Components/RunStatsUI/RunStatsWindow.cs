using UnityEngine;
using UnityEngine.UI;

namespace DeathrunRemade.Components.RunStatsUI
{
    internal class RunStatsWindow : MonoBehaviour
    {
        public GameObject scorePanel;
        public GameObject statsRow;
        public Image background;

        private void Awake()
        {
            background ??= GetComponent<Image>();
            // Reset the colour because I set that to a rough approximation of the sprite colour for better workflow
            // in the editor.
            background.color = Color.white;
        }

        public void AddRun()
        {
            Instantiate(statsRow, scorePanel.transform, false);
        }
    }
}