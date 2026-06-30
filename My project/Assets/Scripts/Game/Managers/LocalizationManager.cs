using System;
using System.Linq;
using Practice.Base;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Practice.Game
{
    public class LocalizationManager : ManagerBase<LocalizationManager>
    {
        public enum Language
        {
            En,
            Ko
        }

        [SerializeField] private Language currentLanguage = Language.Ko;
        [SerializeField] private string defaultTableCollectionName = "Default";

        public Language CurrentLanguage { get; private set; } = Language.Ko;
        public Action<Language> OnLanguageChanged;

        protected override void Awake()
        {
            base.Awake();
            InitLocalization();
        }

        private void InitLocalization()
        {
            if (PlayerPrefs.HasKey("Language"))
            {
                CurrentLanguage = (Language)PlayerPrefs.GetInt("Language");
            }
            else
            {
                CurrentLanguage = DetectedCurrentLanguage();
            }

            SetLocale(CurrentLanguage);
        }

        public void ChangeLanguage(Language newLanguage)
        {
            if (CurrentLanguage == newLanguage)
                return;

            CurrentLanguage = newLanguage;
            PlayerPrefs.SetInt("Language", (int)CurrentLanguage);
            PlayerPrefs.Save();

            SetLocale(CurrentLanguage);
            OnLanguageChanged?.Invoke(CurrentLanguage);
        }

        public string GetLocalizedString(string key)
        {
            var localizedString = new LocalizedString
            {
                TableReference = defaultTableCollectionName,
                TableEntryReference = key
            };

            return localizedString.GetLocalizedString();
        }

        private void SetLocale(Language language)
        {
            if (LocalizationSettings.AvailableLocales == null || LocalizationSettings.AvailableLocales.Locales.Count == 0)
            {
                Debug.LogWarning("Localization package is not initialized or no locales are registered.");
                return;
            }

            string localeCode = language == Language.Ko ? "ko" : "en";
            var locale = LocalizationSettings.AvailableLocales.Locales
                .FirstOrDefault(l => l.Identifier.Code.Equals(localeCode, StringComparison.OrdinalIgnoreCase));

            if (locale == null)
            {
                Debug.LogWarning($"Locale '{localeCode}' not found in available locales.");
                return;
            }

            LocalizationSettings.SelectedLocale = locale;
        }

        public Language DetectedCurrentLanguage()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Korean:
                    return Language.Ko;
                default:
                    return Language.En;
            }
        }
    }
}

