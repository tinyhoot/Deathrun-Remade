using System.Collections.Generic;
using System.Linq;
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
        private List<RunStatsRow> _runRows = new List<RunStatsRow>();

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
            ScoreHandler r = new ScoreHandler(DeathrunInit._Log);
            foreach (var stats in r.TryLoadLegacyStats())
            {
                var modern = stats.ToModernStats();
                r.UpdateScore(ref modern);
                AddRun(modern);
            }
        }

        /// <summary>
        /// Add a new run to the highscore window.
        /// </summary>
        public void AddRun(RunStats stats)
        {
            GameObject rowObject = Instantiate(statsRow, scorePanel.transform, false);
            var row = rowObject.GetComponent<RunStatsRow>();
            row.Stats = stats;
            row.UpdateRow();
            _runRows.Add(row);
            // Make sure the new run shows up at the right place.
            SortRuns();
        }

        /// <summary>
        /// Recalculate rankings and put the runs into the right order.
        /// </summary>
        public void SortRuns()
        {
            // Sort the list descending, best runs first.
            _runRows.Sort((first, second) => ScoreHandler.CompareRuns(first.Stats, second.Stats));
            _runRows.Reverse();
            // Make sure any headers stay where they are.
            int firstRow = _runRows.Select(row => row.transform.GetSiblingIndex()).Min();
            for (int i = 0; i < _runRows.Count; i++)
            {
                _runRows[i].transform.SetSiblingIndex(firstRow + i);
                _runRows[i].SetRank(i + 1);
            }
        }
    }
}