using System.Linq;
using HootLib.Objects;
using Story;
using UnityEngine;

namespace DeathrunRemade.Components
{
    /// <summary>
    /// Responsible for changing how quickly the pod regenerates.
    /// </summary>
    internal class EscapePodRecharge : MonoBehaviour
    {
        private EscapePod _pod;
        private Hootimer _timer;
        
        private void Awake()
        {
            _pod = GetComponent<EscapePod>();
            _timer = new Hootimer(PDA.GetDeltaTime, 3f);
        }

        private void Update()
        {
            // Do not run this every frame, only every few seconds or so.
            if (!_timer.Tick())
                return;
            
            UpdatePowerCells();
        }

        private void SetCellChargeInterval(float interval)
        {
            foreach (var cell in gameObject.GetAllComponentsInChildren<RegeneratePowerSource>() ?? Enumerable.Empty<RegeneratePowerSource>())
            {
                cell.regenerationInterval = interval;
            }
        }

        /// <summary>
        /// Speed up the recharge rate of the lifepod's power cells after it has been repaired.
        /// </summary>
        private void UpdatePowerCells()
        {
            if (_pod.damageEffectsShowing)
            {
                SetCellChargeInterval(20f);
                return;
            }
            // After repairing, speed up the recharge rate.
            SetCellChargeInterval(7.5f);

            // ...but only for some time. Once the explosion approaches, reduce the rate again.
            // This is the "explosion within less than 2 hours" voiceover warning.
            if (StoryGoalManager.main.IsGoalComplete("Story_AuroraWarning3"))
            {
                SetCellChargeInterval(12.5f);
                // Job's done, no more changes necessary.
                Destroy(this);
            }
        }
    }
}