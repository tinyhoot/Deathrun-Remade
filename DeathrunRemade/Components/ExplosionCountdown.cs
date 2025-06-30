using System;
using DeathrunRemade.Handlers;
using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Enums;
using DeathrunRemade.Patches;
using HootLib.Components;
using Nautilus.Options;
using Story;
using TMPro;
using UnityEngine;

namespace DeathrunRemade.Components
{
    internal class ExplosionCountdown : MonoBehaviour, IStoryGoalListener
    {
        private static ExplosionCountdown _Main;
        public GameObject contentHolder;
        public TextMeshProUGUI countdownTitle;
        public TextMeshProUGUI countdownTimer;
        public TextMeshProUGUI countdownWarning;
        // The time at which the Aurora is schedule to explode.
        private float _explosionTime;
        // The key for the storygoal that enables the countdown.
        private const string EnablingStoryGoal = "Story_AuroraWarning3";
        
        /// <summary>
        /// Create the Aurora explosion countdown from components belonging to the Sunbeam Arrival countdown.
        /// </summary>
        /// <param name="gameObject">The GameObject holding the countdown.</param>
        /// <returns>The ExplosionCountdown component.</returns>
        public static ExplosionCountdown Create(out GameObject gameObject)
        {
            // Clone the sunbeam arrival message.
            var original = uGUI.main.hud.transform.GetComponentInChildren<uGUI_SunbeamCountdown>();
            gameObject = Instantiate(original.gameObject, original.transform.parent, true);
            gameObject.SetActive(false);
            gameObject.name = nameof(ExplosionCountdown);
            ExplosionCountdown countdown = gameObject.AddComponent<ExplosionCountdown>();
            
            // Copy the references to existing text components.
            var sunbeam = gameObject.GetComponent<uGUI_SunbeamCountdown>();
            countdown.contentHolder = sunbeam.countdownHolder;
            countdown.contentHolder.SetActive(false);
            countdown.countdownTitle = sunbeam.countdownTitle;
            countdown.countdownTimer = sunbeam.countdownText;
            // Do not let the cloned component run.
            DestroyImmediate(sunbeam);
            // Add an extra text field at the bottom of the timer.
            var warningObject = Instantiate(countdown.countdownTitle.gameObject, countdown.contentHolder.transform, false);
            warningObject.name = "Warning";
            countdown.countdownWarning = warningObject.GetComponent<TextMeshProUGUI>();
            
            // Make the text localisable.
            countdown.countdownTitle.AddDynamicTranslation("dr_explosion_ui_title");
            countdown.countdownWarning.AddDynamicTranslation("dr_explosion_ui_warning",
                () => ExplosionPatcher.GetExplosionDepth(SaveData.Main.Config.ExplosionDepth));
            
            // Make sure the object holding all the content has its positional centre set to the upper left corner.
            countdown.contentHolder.GetComponent<RectTransform>().pivot = new Vector2(0f, 1f);
            // Ensure this component does not survive a return to menu.
            GameEventHandler.OnMainMenuLoaded += countdown.OnMainMenuLoaded;
            
            _Main = countdown;
            return countdown;
        }

        private void Awake()
        {
            // If the Aurora has already exploded there is no point in running any of this.
            if (CrashedShipExploder.main == null || CrashedShipExploder.main.IsExploded())
            {
                Destroy(gameObject);
                return;
            }
            
            _explosionTime = CrashedShipExploder.main.timeToStartCountdown;
            
            DeathrunUtils.SetCountdownWindowPosition(transform, DeathrunInit._Config.ExplosionWindowPosX.Value,
                DeathrunInit._Config.ExplosionWindowPosY.Value);
            // Add an extra depth warning if explosion depth is enabled.
            countdownWarning.gameObject.SetActive(SaveData.Main.Config.ExplosionDepth != Difficulty3.Normal);

            // It would be easier to have this registered through Nautilus, but there is no way to undo such a
            // registration which leads to a nullref on repeat loads.
            if (StoryGoalManager.main.IsGoalComplete("Story_AuroraWarning3"))
                EnableCountdown();
            else
                StoryGoalManager.main.AddListener(this);
        }

        /// <summary>
        /// Visually enable the explosion countdown.
        /// </summary>
        private void EnableCountdown()
        {
            contentHolder.SetActive(true);
            // Run the timer update method once per second.
            InvokeRepeating(nameof(UpdateTimer), 0f, 1f);
        }

        /// <summary>
        /// Update the time to explosion every so often.
        /// </summary>
        private void UpdateTimer()
        {
            float timeRemaining = Mathf.Max(_explosionTime - DayNightCycle.main.timePassedAsFloat, 0f);
            SetTime(timeRemaining);
            if (CrashedShipExploder.main.IsExploded())
                Destroy(gameObject);
        }

        private void OnDestroy()
        {
            // Unregister the event to avoid forever-references.
            GameEventHandler.OnMainMenuLoaded -= OnMainMenuLoaded;
            // This is a hashset. Removing is safe even if the listener was never added.
            StoryGoalManager.main.RemoveListener(this);
        }

        /// <summary>
        /// Ensure this component does not survive a return to the main menu.
        /// </summary>
        public void OnMainMenuLoaded()
        {
            Destroy(gameObject);
        }

        /// <summary>
        /// Update the window position when the player changes the in-game settings for horizontal position.
        /// </summary>
        public static void OnUpdateSettingsX(object sender, SliderChangedEventArgs args)
        {
            if (_Main == null)
                return;
            DeathrunUtils.SetCountdownWindowPosition(_Main.transform, args.Value,
                DeathrunInit._Config.ExplosionWindowPosY.Value);
        }
        
        /// <summary>
        /// Update the window position when the player changes the in-game settings for vertical position.
        /// </summary>
        public static void OnUpdateSettingsY(object sender, SliderChangedEventArgs args)
        {
            if (_Main == null)
                return;
            DeathrunUtils.SetCountdownWindowPosition(_Main.transform, DeathrunInit._Config.ExplosionWindowPosX.Value,
                args.Value);
        }

        public void SetTime(float seconds)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
            countdownTimer.text = $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }

        public void SetTitle(string title)
        {
            countdownTitle.text = title;
        }

        public void SetWarning(string warning)
        {
            countdownWarning.text = warning;
        }

        /// <summary>
        /// Get notified when the story has progressed far enough to enable this timer.
        /// </summary>
        public void NotifyGoalComplete(string key)
        {
            if (key == EnablingStoryGoal)
                EnableCountdown();
        }
    }
}