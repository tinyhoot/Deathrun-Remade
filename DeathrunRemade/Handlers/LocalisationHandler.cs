using DeathrunRemade.Objects;
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
            // TODO: Also use this for tooltiphandler
        }

        private static void OnLanguageChanged()
        {
            _currentLanguage = Language.main.currentLanguage;
        }
        
        public static void FormatExistingLine(string key, params object[] formatArgs)
        {
            LanguageHandler.SetLanguageLine(key, _language.GetFormat(key, formatArgs), _currentLanguage);
        }

        public static void OnReset()
        {
            _languageCache.UndoChanges();
        }
    }
}