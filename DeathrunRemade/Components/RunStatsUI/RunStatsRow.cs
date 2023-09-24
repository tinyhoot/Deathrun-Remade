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

        public void UpdateRow(RunStats stats)
        {
            rank.text = "-1";
            startPoint.text = stats.startPoint;
            time.text = $"{stats.time:F0}";
            causeOfDeath.text = stats.causeOfDeath;
            deaths.text = $"{stats.deaths}";
            scoreMult.text = $"{stats.scoreMult:F1}x";
            score.text = $"{stats.scoreBase * stats.scoreMult}";
        }
    }
}