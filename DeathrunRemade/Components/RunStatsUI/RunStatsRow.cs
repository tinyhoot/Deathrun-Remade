using TMPro;
using UnityEngine;

namespace DeathrunRemade.Components.RunStatsUI
{
    public class RunStatsRow : MonoBehaviour
    {
        public TextMeshProUGUI rank;
        public TextMeshProUGUI startPoint;
        public TextMeshProUGUI time;
        public TextMeshProUGUI causeOfDeath;
        public TextMeshProUGUI deaths;
        public TextMeshProUGUI scoreMult;
        public TextMeshProUGUI score;

        private void Awake()
        {
            // score.text = $"{Random.value:F3}";
        }
    }
}