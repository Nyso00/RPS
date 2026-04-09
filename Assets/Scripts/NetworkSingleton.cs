using UnityEngine;
using Unity.Netcode;

public abstract class NetworkSingleton<T> : NetworkBehaviour where T : NetworkBehaviour
{
    private static T _instance;
    private static bool _isApplicationQuitting = false;

    public static T Instance
    {
        get
        {
            if (_isApplicationQuitting)
            {
                Debug.LogWarning($"[NetworkSingleton] Instance of {typeof(T)} already destroyed on application quit. Returning null.");
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
            Debug.LogWarning($"[NetworkSingleton] Duplicate instance of {typeof(T)} found. Destroying {gameObject.name}");
            Destroy(gameObject);
        }
    }

    protected virtual void OnApplicationQuit()
    {
        _isApplicationQuitting = true;
    }

    public override void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }

        base.OnDestroy();
    }
}
