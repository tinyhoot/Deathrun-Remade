using System;
using DeathrunRemade.Objects;
using TMPro;
using UnityEngine;

namespace DeathrunRemade.Components.RunStatsUI
{
    internal class RunStatsRow : MonoBehaviour
    {
        public TextMeshProUGUI rank;
        public TextMeshProUGUI startPoint;
        public TextMeshProUGUI time;
        public TextMeshProUGUI causeOfDeath;
        public TextMeshProUGUI deaths;
        public TextMeshProUGUI scoreMult;
        public TextMeshProUGUI score;
        [NonSerialized] public RunStats Stats;

        public void SetRank(int ranking)
        {
            rank.text = $"{ranking}";
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
            score.text = $"{Stats.scoreBase * Stats.scoreMult:F0}";
        }
    }
}