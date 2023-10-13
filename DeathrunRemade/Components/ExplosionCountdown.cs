using System;
using DeathrunRemade.Handlers;
using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Enums;
using DeathrunRemade.Patches;
using Nautilus.Handlers;
using Nautilus.Options;
using Story;
using TMPro;
using UnityEngine;

namespace DeathrunRemade.Components
{
    internal class ExplosionCountdown : MonoBehaviour
    {
        private static ExplosionCountdown _Main;
        public GameObject contentHolder;
        public TextMeshProUGUI countdownTitle;
        public TextMeshProUGUI countdownTimer;
        public TextMeshProUGUI countdownWarning;
        // The time at which the Aurora is schedule to explode.
        private float _explosionTime;
        // The distance between the sceen edge and the left side of the countdown window.
        private float _xDelta;
        // The distance between the screen edge and the top end of the countdown window.
        private float _yDelta;
        
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
            // Add an extra text field at the bottom of the timer.
            var warningObject = Instantiate(countdown.countdownTitle.gameObject, countdown.contentHolder.transform, false);
            countdown.countdownWarning = warningObject.GetComponent<TextMeshProUGUI>();
            // Make sure the object holding all the content has its positional centre set to the upper left corner.
            countdown.contentHolder.GetComponent<RectTransform>().pivot = new Vector2(0f, 1f);
            // Do not let the cloned component run.
            DestroyImmediate(gameObject.GetComponent<uGUI_SunbeamCountdown>());
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
            Rect screen = gameObject.GetComponent<RectTransform>().rect;
            var localPosition = contentHolder.transform.localPosition;
            // Figure out the distance between the side of the screen and the countdown window.
            _xDelta = (screen.width / 2f) - localPosition.x;
            // Figure out the distance between the upper limit of the screen and the top of the countdown window.
            _yDelta = (screen.height / 2f) - localPosition.y;
            
            // Add an extra depth warning if explosion depth is enabled.
            SetPosition(DeathrunInit._Config.ExplosionWindowPosX.Value, DeathrunInit._Config.ExplosionWindowPosY.Value);
            SetTitle("Drive Core Explosion In:");
            SetWarning($"Shockwave up to {ExplosionPatcher.GetExplosionDepth(SaveData.Main.Config.ExplosionDepth)}m deep!");
            countdownWarning.gameObject.SetActive(SaveData.Main.Config.ExplosionDepth != Difficulty3.Normal);

            if (StoryGoalManager.main.IsGoalComplete("Story_AuroraWarning3"))
                EnableCountdown();
            else
                StoryGoalHandler.RegisterCustomEvent("Story_AuroraWarning3", EnableCountdown);
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
            _Main.SetPosition(args.Value, DeathrunInit._Config.ExplosionWindowPosY.Value);
        }
        
        /// <summary>
        /// Update the window position when the player changes the in-game settings for vertical position.
        /// </summary>
        public static void OnUpdateSettingsY(object sender, SliderChangedEventArgs args)
        {
            if (_Main == null)
                return;
            _Main.SetPosition(DeathrunInit._Config.ExplosionWindowPosX.Value, args.Value);
        }

        /// <summary>
        /// Set this countdown at the height of the usual sunbeam message but on the left side of the screen.
        /// </summary>
        /// <param name="widthDistanceMult">How far from the left edge of the screen the window should be placed,
        /// measured as a multiplier of the original distance.</param>
        /// <param name="heightDistanceMult">How far down from the top of the screen the window should be placed,
        /// measured as a multiplier of the original distance.</param>
        private void SetPosition(float widthDistanceMult, float heightDistanceMult)
        {
            Vector3 position = contentHolder.transform.localPosition;
            Rect screenRect = gameObject.GetComponent<RectTransform>().rect;
            // Ensure the window always ends up on the left side of the screen.
            float x = -(screenRect.width / 2f) + (_xDelta * widthDistanceMult);
            // Adjust the height to the desired position.
            float y = (screenRect.height / 2f) - (_yDelta * heightDistanceMult);
            position = new Vector3(x, y, position.z);
            contentHolder.transform.localPosition = position;
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
    }
}