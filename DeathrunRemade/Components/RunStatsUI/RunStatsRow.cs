using System.IO;
using TMPro;
using UnityEngine;

namespace DeathrunRemade.Components.RunStatsUI
{
    public class RunStatsRow : MonoBehaviour
    {
        private TextMeshProUGUI _rank;
        private TextMeshProUGUI _startPoint;
        private TextMeshProUGUI _time;
        private TextMeshProUGUI _causeOfDeath;
        private TextMeshProUGUI _deaths;
        private TextMeshProUGUI _scoreMult;
        private TextMeshProUGUI _score;

        private void Awake()
        {
            Init();
            _score.text = $"{Random.value:F3}";
        }

        /// <summary>
        /// There's a lot of child objects with text components already set in the editor. Grab them once so we have
        /// a reference to all of them later.
        /// </summary>
        /// <exception cref="InvalidDataException">Thrown if there's a child object that does not correspond to a
        /// field in this class. Just a safety measure in case we forget to update something here.</exception>
        private void Init()
        {
            // Grab all the objects that were already set up in the editor.
            foreach (Transform child in transform)
            {
                switch (child.name)
                {
                    case "Rank":
                        _rank = child.GetComponent<TextMeshProUGUI>();
                        break;
                    case "Start":
                        _startPoint = child.GetComponent<TextMeshProUGUI>();
                        break;
                    case "Time":
                        _time = child.GetComponent<TextMeshProUGUI>();
                        break;
                    case "CauseOfDeath":
                        _causeOfDeath = child.GetComponent<TextMeshProUGUI>();
                        break;
                    case "Deaths":
                        _deaths = child.GetComponent<TextMeshProUGUI>();
                        break;
                    case "ScoreMult":
                        _scoreMult = child.GetComponent<TextMeshProUGUI>();
                        break;
                    case "Score":
                        _score = child.GetComponent<TextMeshProUGUI>();
                        break;
                    default:
                        throw new InvalidDataException($"Unexpected child in run stats object: '{child.name}'");
                }
            }
        }
    }
}