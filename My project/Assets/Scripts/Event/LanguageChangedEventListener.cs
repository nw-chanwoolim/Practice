using UnityEngine;
using UnityEngine.Events;
using Practice.Play;

namespace Practice.Event
{
    [System.Serializable]
    public class LanguageChangedUnityEvent : UnityEvent<LocalizationManager.Language> { }

    public class LanguageChangedEventListener : MonoBehaviour
    {
        [SerializeField] private LanguageChangedEvent languageChangedEvent;
        [SerializeField] private LanguageChangedUnityEvent response;

        private void OnEnable()
        {
            if (languageChangedEvent != null)
                languageChangedEvent.RegisterListener(this);
        }

        private void OnDisable()
        {
            if (languageChangedEvent != null)
                languageChangedEvent.UnregisterListener(this);
        }

        public void OnEventRaised(LocalizationManager.Language language)
        {
            response?.Invoke(language);
        }
    }
}
