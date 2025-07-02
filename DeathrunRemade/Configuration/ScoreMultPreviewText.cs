using DeathrunRemade.Handlers;
using DeathrunRemade.Objects;
using HootLib.Configuration;
using UnityEngine;

namespace DeathrunRemade.Configuration
{
    internal class ScoreMultPreviewText : TextDecorator
    {
        private Config _config;
        private string _languageKey;
        
        public ScoreMultPreviewText(Config config, string languageKey, float fontSize = 30) : base(languageKey, fontSize)
        {
            _config = config;
            // Ensure the multiplier updates whenever a setting is changed.
            config.ConfigFile.SettingChanged += (_, _) => UpdateText();
            _languageKey = languageKey;
        }

        public override void AddToPanel(Transform panel)
        {
            base.AddToPanel(panel);
            SetText(_languageKey, GetScoreMult);
        }

        private object GetScoreMult()
        {
            var isMainMenu = _textObject.GetComponentInParent<uGUI_MainMenu>() != null;
            ConfigSave configSave = isMainMenu ? new ConfigSave(_config) : SaveData.Main.Config;
            return $"{ScoreHandler.CalculateScoreMultiplier(configSave):F1}";
        }
    }
}