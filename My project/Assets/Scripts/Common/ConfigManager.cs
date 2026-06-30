using System;
using UnityEngine;
using Practice.Base;
using Practice.Data;
using Practice.Event;
using Practice.Game;

namespace Practice.Common
{
    public class ConfigManager : ManagerBase<ConfigManager>
    {
        [SerializeField] private GameConfigSO defaultConfig;
        [SerializeField] private GameEvent configLoadedEvent;
        [SerializeField] private LanguageChangedEvent languageChangedEvent;

        public long LastPlayTime { get; private set; }
        public int TotalPlaycount { get; private set; }
        public bool IsFirstPlay { get; private set; }
        public LocalizationManager.Language CurrentLanguage { get; private set; }

        public event Action OnConfigLoaded;
        public event Action<LocalizationManager.Language> OnLanguageChanged;
        public event Action OnPlayTimeChanged;

        protected override void Awake()
    {
        base.Awake();
        LoadDefaultConfig();
        LoadSavedConfig();
        Debug.Log($"ConfigManager initialized: Language={CurrentLanguage}, LastPlayTime={LastPlayTime}, TotalPlaycount={TotalPlaycount}, IsFirstPlay={IsFirstPlay}");
        OnConfigLoaded?.Invoke();
        configLoadedEvent?.Raise();
    }

    private void LoadDefaultConfig()
    {
        LastPlayTime = (long)defaultConfig.lastPlayTime;
        TotalPlaycount = defaultConfig.totalPlaycount;
        IsFirstPlay = defaultConfig.isFirstPlay;
        CurrentLanguage = defaultConfig.currentLanguage;
    }

    private void LoadSavedConfig()
    {
        if (PlayerPrefs.HasKey("LastPlayTime"))
        {
            var stringValue = PlayerPrefs.GetString("LastPlayTime", string.Empty);
            if (!string.IsNullOrEmpty(stringValue) && long.TryParse(stringValue, out var savedTime))
            {
                LastPlayTime = savedTime;
            }
            else
            {
                LastPlayTime = (long)PlayerPrefs.GetFloat("LastPlayTime");
            }
        }

        if (PlayerPrefs.HasKey("TotalPlaycount"))
            TotalPlaycount = PlayerPrefs.GetInt("TotalPlaycount");

        if (PlayerPrefs.HasKey("IsFirstPlay"))
            IsFirstPlay = PlayerPrefs.GetInt("IsFirstPlay") == 1;

        if (PlayerPrefs.HasKey("CurrentLanguage"))
            CurrentLanguage = (LocalizationManager.Language)PlayerPrefs.GetInt("CurrentLanguage");
    }
    public void ChangeLanguage(LocalizationManager.Language newLanguage)
    {
        if (CurrentLanguage == newLanguage)
        {
            Debug.Log($"ConfigManager.ChangeLanguage called with same language: {newLanguage}");
            return;
        }

        Debug.Log($"ConfigManager.ChangeLanguage: {CurrentLanguage} -> {newLanguage}");
        CurrentLanguage = newLanguage;
        PlayerPrefs.SetInt("CurrentLanguage", (int)newLanguage);
        PlayerPrefs.Save();

        LocalizationManager.Instance.ChangeLanguage(newLanguage);
        OnLanguageChanged?.Invoke(newLanguage);
        languageChangedEvent?.Raise(newLanguage);
    }
    public void UpdatePlayTime(long unixTimestamp)
    {
        LastPlayTime = unixTimestamp;
        PlayerPrefs.SetString("LastPlayTime", LastPlayTime.ToString());
        PlayerPrefs.Save();
        Debug.Log($"ConfigManager: LastPlayTime updated to {LastPlayTime}");
        OnPlayTimeChanged?.Invoke();
    }

    public void IncrementPlaycount(int seconds)
    {
        TotalPlaycount += seconds;
        PlayerPrefs.SetInt("TotalPlaycount", TotalPlaycount);
        PlayerPrefs.Save();
        Debug.Log($"ConfigManager: TotalPlaycount incremented to {TotalPlaycount}");
        OnPlayTimeChanged?.Invoke();
    }

    public void SetFirstPlayDone()
    {
        IsFirstPlay = false;
        PlayerPrefs.SetInt("IsFirstPlay", 0);
        PlayerPrefs.Save();
    }
}
}