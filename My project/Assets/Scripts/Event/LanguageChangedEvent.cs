using System.Collections.Generic;
using UnityEngine;
using Practice.Play;

namespace Practice.Event
{
    [CreateAssetMenu(menuName = "Practice/Event/Language Changed Event")]
    public class LanguageChangedEvent : ScriptableObject
    {
        private readonly List<LanguageChangedEventListener> listeners = new List<LanguageChangedEventListener>();

        public void Raise(LocalizationManager.Language language)
        {
            Debug.Log($"LanguageChangedEvent raised: {language} with {listeners.Count} listener(s)");
            for (int i = listeners.Count - 1; i >= 0; i--)
            {
                listeners[i].OnEventRaised(language);
            }
        }

        public void RegisterListener(LanguageChangedEventListener listener)
        {
            if (!listeners.Contains(listener))
                listeners.Add(listener);
        }

        public void UnregisterListener(LanguageChangedEventListener listener)
        {
            if (listeners.Contains(listener))
                listeners.Remove(listener);
        }
    }
}
