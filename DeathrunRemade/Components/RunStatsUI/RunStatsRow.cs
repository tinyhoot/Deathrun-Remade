using System;
using DeathrunRemade.Objects;
using TMPro;
using UnityEngine;

namespace DeathrunRemade.Components.RunStatsUI
{
    /// <summary>
    /// A component used in unity editor to represent data related to a run in one standardised row.
    /// </summary>
    internal class RunStatsRow : MonoBehaviour
    {
        // This is set in the editor, the compiler is helpful but wrong.
#pragma warning disable CS0649
        public TextMeshProUGUI rank;
        public TextMeshProUGUI startPoint;
        public TextMeshProUGUI time;
        public TextMeshProUGUI causeOfDeath;
        public TextMeshProUGUI deaths;
        public TextMeshProUGUI scoreMult;
        public TextMeshProUGUI score;
        public GameObject deleteButton;
#pragma warning restore CS0649
        [NonSerialized] public RunStats Stats;

        public void SetRank(int ranking)
        {
            rank.text = $"{ranking}";
        }

        /// <summary>
        /// Delete this row from the window and its associated run data from the overall statistics.
        /// </summary>
        public void DeleteRow()
        {
            Destroy(gameObject);
            DeathrunInit._RunHandler.DeleteRun(Stats);
        }

        /// <summary>
        /// Every row except for the descriptive header should have a delete button.
        /// </summary>
        public void ShowDeleteButton(bool visible)
        {
            deleteButton.SetActive(visible);
        }

        /// <summary>
        /// Update all fields of this row with data from the run.
        /// </summary>
        public void UpdateRow()
        {
            if (string.IsNullOrEmpty(rank.text))
                rank.text = "-1";
            startPoint.text = Stats.startPoint;
            time.text = $"{DeathrunUtils.TimeToGameDays(Stats.time):F0} Days";
            causeOfDeath.text = Stats.causeOfDeath;
            deaths.text = $"{Stats.deaths}";
            scoreMult.text = $"{Stats.scoreMult:F1}x";
            score.text = $"{Stats.scoreTotal:F0}";
        }
    }
}