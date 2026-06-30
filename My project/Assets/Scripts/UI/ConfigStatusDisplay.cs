using System;
using UnityEngine;
using TMPro;
using Practice.Game;
using Practice.Common;

namespace Practice.UI
{
    public class ConfigStatusDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text statusText;

        private void OnEnable()
        {
            if (ConfigManager.Instance != null)
            {
                ConfigManager.Instance.OnConfigLoaded += UpdateStatus;
                ConfigManager.Instance.OnLanguageChanged += OnLanguageChanged;
                ConfigManager.Instance.OnPlayTimeChanged += UpdateStatus;
            }
        }

        private void OnDisable()
        {
            if (ConfigManager.Instance != null)
            {
                ConfigManager.Instance.OnConfigLoaded -= UpdateStatus;
                ConfigManager.Instance.OnLanguageChanged -= OnLanguageChanged;
                ConfigManager.Instance.OnPlayTimeChanged -= UpdateStatus;
            }
        }

        private void Start()
        {
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            if (statusText == null || ConfigManager.Instance == null)
                return;

            var manager = ConfigManager.Instance;
            var lastPlayTimeText = manager.LastPlayTime > 0
                ? manager.LastPlayTime.ToString()
                : "N/A";

            statusText.text = $"Language: {manager.CurrentLanguage}\n" +
                              $"LastPlayTime: {lastPlayTimeText}\n" +
                              $"Playcount: {manager.TotalPlaycount}\n" +
                              $"FirstPlay: {manager.IsFirstPlay}";
        }

        private void OnLanguageChanged(LocalizationManager.Language language)
        {
            UpdateStatus();
        }
    }
}
