using System;
using UnityEngine;
using UnityEngine.UI;
using Practice.Game;
using Practice.Common;

namespace Practice.UI
{
    public class ConfigButtonBinder : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button updatePlayTimeButton;
        [SerializeField] private Button changeLanguageButton;

        [Header("Language")]
        [SerializeField] private LocalizationManager.Language targetLanguage = LocalizationManager.Language.En;

        private void OnEnable()
        {
            if (updatePlayTimeButton != null)
                updatePlayTimeButton.onClick.AddListener(OnUpdatePlayTimeButtonClicked);

            if (changeLanguageButton != null)
                changeLanguageButton.onClick.AddListener(OnChangeLanguageButtonClicked);
        }

        private void OnDisable()
        {
            if (updatePlayTimeButton != null)
                updatePlayTimeButton.onClick.RemoveListener(OnUpdatePlayTimeButtonClicked);

            if (changeLanguageButton != null)
                changeLanguageButton.onClick.RemoveListener(OnChangeLanguageButtonClicked);
        }

        private void OnUpdatePlayTimeButtonClicked()
        {
            var manager = ConfigManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("ConfigButtonBinder: ConfigManager instance is not available.");
                return;
            }

            long unixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            manager.UpdatePlayTime(unixSeconds);
            Debug.Log($"ConfigButtonBinder: UpdatePlayTime button clicked. Value={unixSeconds}");
        }

        private void OnChangeLanguageButtonClicked()
        {
            var manager = ConfigManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("ConfigButtonBinder: ConfigManager instance is not available.");
                return;
            }

            var nextLanguage = manager.CurrentLanguage == targetLanguage ? ToggleLanguage(targetLanguage) : targetLanguage;
            Debug.Log($"ConfigButtonBinder: Change language button clicked. Target={nextLanguage}");
            manager.ChangeLanguage(nextLanguage);
        }

        private LocalizationManager.Language ToggleLanguage(LocalizationManager.Language current)
        {
            return current == LocalizationManager.Language.Ko ? LocalizationManager.Language.En : LocalizationManager.Language.Ko;
        }
    }
}
