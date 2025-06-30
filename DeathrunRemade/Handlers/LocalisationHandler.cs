using System;
using HootLib.Objects;
using Nautilus.Handlers;

namespace DeathrunRemade.Handlers
{
    /// <summary>
    /// Responsible for properly updating localised lines displayed in game.
    /// </summary>
    internal static class LocalisationHandler
    {
        private static Language _language;
        private static NautilusShell<string, string> _languageCache;
        private static string _currentLanguage = "English";

        public static void Init()
        {
            // This invokes Language's getter, which ensures main is initialised on first use and therefore never null.
            _language = Language.main;
            _languageCache = new NautilusShell<string, string>(
                (key, text) => LanguageHandler.SetLanguageLine(key, text, _currentLanguage), 
                _language.Get);
            Language.OnLanguageChanged += OnLanguageChanged;
            DeathrunInit.OnReset += OnReset;
        }

        private static void OnLanguageChanged()
        {
            _currentLanguage = Language.main.currentLanguage;
        }
        
        public static void FormatExistingLine(string key, params object[] formatArgs)
        {
            _languageCache.SendChanges(key, _language.GetFormat(key, formatArgs));
        }

        public static string Get(string key)
        {
            return _language.Get(key);
        }

        public static string GetFormatted(string key, params object[] formatArgs)
        {
            return Language.main.GetFormat(key, formatArgs);
        }

        /// <summary>
        /// Get a sanitised version of the input string for use as a runtime-generated language key. Primarily useful
        /// for content that can be edited by users like file names or custom starts.
        /// </summary>
        public static string GetSanitisedDynamicKey(string key)
        {
            // Turns something like "Bulb Zone" into "bulb_zone".
            return key.ToLower().Trim().Replace(" ", "_");
        }

        public static string GetDeathKey(string causeOfDeath)
        {
            return "dr_death_cause_" + GetSanitisedDynamicKey(causeOfDeath);
        }

        public static string GetLocalisedCauseOfDeath(string causeOfDeath)
        {
            // Special handling for creatures, which can all bite the player individually.
            if (Enum.TryParse(causeOfDeath, true, out TechType _))
            {
                if (_language.TryGet(causeOfDeath, out string line))
                    return line;
            }

            // This should resolve to one of the localised entries in the mod's language files.
            if (_language.TryGet(GetDeathKey(causeOfDeath), out string line2))
                return line2;

            // Return the cause directly as a fallback.
            DeathrunInit._Log.Warn($"Failed to find language key for cause of death '{causeOfDeath}'");
            return causeOfDeath;
        }

        public static string GetStartKey(string startLocation)
        {
            return "dr_start_" + GetSanitisedDynamicKey(startLocation);
        }

        public static void SetTechTypeName(TechType techType, string name)
        {
            _languageCache.SendChanges(techType.AsString(), name);
        }

        public static void SetTooltip(TechType techType, string text)
        {
            _languageCache.SendChanges($"Tooltip_{techType}", text);
        }

        public static void OnReset()
        {
            _languageCache.UndoChanges();
        }
    }
}