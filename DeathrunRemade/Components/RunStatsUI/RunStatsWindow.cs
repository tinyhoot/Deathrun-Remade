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
        // This is set in the editor, the compiler is freaking out over nothing.
#pragma warning disable CS0649
        public GameObject scorePanel;
        public GameObject statsRow;
        public Image background;
#pragma warning restore CS0649
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
            // Because this runs the first time the window is set active, we can be sure that it will happen only once
            // the window is actually opened for the first time. All underlying data is ready by that point.
            AddRuns(DeathrunInit._RunHandler.ModStats.bestRuns.ToArray());
        }

        /// <summary>
        /// Add a new run to the highscore window.
        /// </summary>
        public void AddRun(RunStats stats)
        {
            AddNewRow(stats);
            // Make sure the new run shows up at the right place.
            SortRuns();
        }

        /// <summary>
        /// Add several runs to the highscore window.
        /// </summary>
        public void AddRuns(params RunStats[] stats)
        {
            foreach (var run in stats)
            {
                AddNewRow(run);
            }
            SortRuns();
        }

        /// <summary>
        /// Instantiate a new row object for the given run.
        /// </summary>
        private void AddNewRow(RunStats stats)
        {
            GameObject rowObject = Instantiate(statsRow, scorePanel.transform, false);
            var row = rowObject.GetComponent<RunStatsRow>();
            row.Stats = stats;
            row.UpdateRow();
            _runRows.Add(row);
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