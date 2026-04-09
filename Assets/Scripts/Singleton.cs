using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static bool _isApplicationQuitting = false;

    public static T Instance
    {
        get
        {
            if (_isApplicationQuitting)
            {
                Debug.LogWarning($"[Singleton] Instance of {typeof(T)} already destroyed on application quit. Returning null.");
                return null;
            }

            if (_instance == null)
            {
                _instance = FindAnyObjectByType<T>() ?? new GameObject(typeof(T).Name).AddComponent<T>();
            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
        }
        else if (_instance != this)
        {
            // 이미 인스턴스가 있는데 다른 오브젝트가 존재한다면 파괴
            Debug.LogWarning($"[Singleton] Duplicate instance of {typeof(T)} found. Destroying {gameObject.name}");
            Destroy(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _isApplicationQuitting = true;
            _instance = null;
        }
    }
}
