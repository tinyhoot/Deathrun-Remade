using BepInEx.Configuration;
using DeathrunRemade.Handlers;
using DeathrunRemade.Objects;
using HootLib.Configuration;
using UnityEngine;

namespace DeathrunRemade.Configuration
{
    internal class ScoreMultPreviewText : TextDecorator
    {
        private Config _config;
        private string _textTemplate;
        
        public ScoreMultPreviewText(Config config, string formatTemplate, float fontSize = 30) : base(formatTemplate, fontSize)
        {
            _config = config;
            config.ConfigFile.SettingChanged += OnSettingChanged;
            // Keep a copy that won't change.
            _textTemplate = formatTemplate;
        }

        public override void AddToPanel(Transform panel)
        {
            base.AddToPanel(panel);
            UpdateScorePreview(_textObject.GetComponentInParent<uGUI_MainMenu>() != null);
        }

        private void OnSettingChanged(object sender, SettingChangedEventArgs args)
        {
            UpdateScorePreview(_textObject.GetComponentInParent<uGUI_MainMenu>() != null);
        }

        private void UpdateScorePreview(bool isMainMenu)
        {
            // If this is the in-game menu we need to use the settings that are locked in for this save rather than
            // whatever is set in the mod options menu.
            ConfigSave configSave = isMainMenu ? new ConfigSave(_config) : SaveData.Main.Config;
            
            // Recalculate the multiplier with the current settings.
            float mult = ScoreHandler.CalculateScoreMultiplier(configSave);
            SetText(string.Format(_textTemplate, $"{mult:F1}x"));
        }
    }
}