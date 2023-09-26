using DeathrunRemade.Handlers;
using DeathrunRemade.Objects;
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

        private void Start()
        {
            // testing
            RunStatsHandler r = new RunStatsHandler(DeathrunInit._Log);
            foreach (var stats in r.TryLoadLegacyStats())
            {
                var modern = stats.ToModernStats();
                r.UpdateScore(ref modern);
                AddRun(modern);
            }
        }

        /// <summary>
        /// Add a new run to the highscore window 
        /// </summary>
        public void AddRun(RunStats stats)
        {
            var row = Instantiate(statsRow, scorePanel.transform, false);
            row.GetComponent<RunStatsRow>().UpdateRow(stats);
        }

        /// <summary>
        /// Recalculate rankings and put the runs into the right order.
        /// </summary>
        public void SortRuns()
        {
            
        }
    }
}