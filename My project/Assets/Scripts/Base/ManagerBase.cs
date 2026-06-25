using UnityEngine;

namespace Practice.Base
{
    public class ManagerBase<T> : MonoBehaviour where T : ManagerBase<T>
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _isQuitting = false;

    public static T Instance
        {
            get
            {
                if (_isQuitting)
                {
                    Debug.LogWarning($"[Singleton] Instance '{typeof(T)}' already destroyed on application quit. Won't create again - returning null.");
                    return null;
                }
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindAnyObjectByType<T>();
                        if (_instance == null)
                        {
                            GameObject obj = new GameObject(typeof(T).Name);
                            _instance = obj.AddComponent<T>();
                            obj.name = $"{typeof(T).Name} (Singleton)";

                            DontDestroyOnLoad(obj);
                        }
                    }
                    return _instance;
                }
            }
        }

    protected virtual void Awake()
    {
        lock (_lock)
        {
            if (_instance == null)
                {
                        _instance = this as T;
                        DontDestroyOnLoad(gameObject);
                    }
                    else if (_instance != this)
                    {
                        Destroy(gameObject);
                    }
        }
    }

    protected virtual void OnApplicationQuit()
    {
        _isQuitting = true;
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }


    }
}
}
