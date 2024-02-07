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