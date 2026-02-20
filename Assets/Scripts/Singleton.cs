using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<T>();
                if (instance == null)
                {
                    instance = new GameObject(typeof(T).Name).AddComponent<T>();
                }
            }
            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
        }
        else if (instance != this)
        {
            // 이미 인스턴스가 있는데 다른 오브젝트가 존재한다면 파괴
            Debug.LogWarning($"[Singleton] Duplicate instance of {typeof(T)} found. Destroying {gameObject.name}");
            Destroy(gameObject);
        }
    }
}
