using System;
using DeathrunRemade.Configuration;
using DeathrunRemade.Objects;
using DeathrunRemade.Objects.Enums;
using DeathrunRemade.Patches;
using HootLib.Objects;
using UnityEngine;

namespace DeathrunRemade.Components
{
    /// <summary>
    /// Responsible for dynamically changing the displayed text of the escape pod status screen.
    /// </summary>
    internal class EscapePodStatusScreen : MonoBehaviour
    {
        private const float BlinkInterval = 0.75f;
        // Slightly different shades of yellow.
        private readonly Color _blinkOnColor = new Color(0.95f, 0.95f, 0.25f);
        private readonly Color _blinkOffColor = new Color(0.9f, 0.9f, 0.25f);
        
        private ConfigSave _config;
        private EscapePod _pod;
        private uGUI_EscapePod _screen;
        private string _intro3Content;
        private string[] _intro4Content;
        private Hootimer _timer;
        private bool _blinkOn;
        
        private void Awake()
        {
            _config = SaveData.Main.Config;
            _pod = GetComponent<EscapePod>();
            _screen = uGUI_EscapePod.main;
            _timer = new Hootimer(PDA.GetDeltaTime, BlinkInterval);

            // None of this works if we're not in the right language.
            if (Language.main.currentLanguage != "English")
                Destroy(this);
            
            _intro3Content = Language.main.Get("IntroEscapePod3Content")
                .Split(new[] { " - Flotation" }, StringSplitOptions.None)[0];
            _intro4Content = Language.main.Get("IntroEscapePod4Content")
                .Split(new[] { " - Flotation Devices: DEPLOYED\n - Hull Integrity: OK\n" }, StringSplitOptions.None);
        }

        private void Update()
        {
            // Only do updates when actually necessary.
            if (!_timer.Tick())
                return;
            
            _blinkOn = !_blinkOn;
            UpdateText();
        }

        private static string BlinkText(string text, bool doBlink)
        {
            return doBlink ? text : "";
        }
        
        /// <summary>
        /// Get the text to display when the pod is still damaged.
        /// </summary>
        private string GetDamagedText(bool doBlink)
        {
            return _intro3Content + GetFlotationLine(doBlink) + GetAtmosphereLine(doBlink);
        }

        /// <summary>
        /// Get the text to display once the pod has been repaired.
        /// </summary>
        private string GetRepairedText(bool doBlink)
        {
            string extra = GetFlotationLine(doBlink) + GetAtmosphereLine(doBlink);
            return _intro4Content[0] + extra + _intro4Content[1];
        }

        /// <summary>
        /// Check whether the pod has already been repaired. This way is more reliable than checking for damaged
        /// effects since those don't start until after the cutscene ends.
        /// </summary>
        private bool IsRepaired()
        {
            return _pod.liveMixin.IsFullHealth();
        }

        /// <summary>
        /// Make the text blink in two different colours for extra emphasis.
        /// </summary>
        private void SetTextColour(bool doBlink)
        {
            _screen.content.color = doBlink ? _blinkOnColor : _blinkOffColor;
        }

        /// <summary>
        /// Update the screen inside the pod with our custom content.
        /// </summary>
        private void UpdateText()
        {
            if (IsRepaired())
                SetTextColour(_blinkOn);
            _screen.content.text = IsRepaired() ? GetRepairedText(_blinkOn) : GetDamagedText(_blinkOn);
        }

        private string GetAtmosphereLine(bool doBlink)
        {
            string start = " - Atmosphere: ";
            string badAir = "\n    - Deploy filter pumps for oxygen\n";
            if (_config.SurfaceAir == Difficulty3.Deathrun)
                return start + BlinkText("POISONED", doBlink) + badAir;
            if (_config.SurfaceAir == Difficulty3.Hard && RadiationPatcher.IsSurfaceIrradiated())
                return start + BlinkText("IRRADIATED", doBlink) + badAir;
            return start + "Breathable\n";
        }

        private string GetFlotationLine(bool doBlink)
        {
            string start = " - Flotation Devices: ";
            return start + (_config.SinkLifepod ? BlinkText("FAILED", doBlink) : "DEPLOYED") + "\n";
        }
    }
}