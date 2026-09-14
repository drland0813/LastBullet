using UnityEngine;

public abstract class Singleton<T> : StaticSingleton<T> where T : MonoBehaviour
{
    protected override void Awake()
    {
        if (Instance != null && Instance.gameObject != null)
        {
            Destroy(gameObject);
            return;
        }
        base.Awake();
    }
}


public abstract class StaticSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance == null || Instance.gameObject == null)
        {
            Instance = this as T;
        }
    }

    protected virtual void OnDestroy()
    {
        if (Instance == this as T)
        {
            Instance = null;
        }
    }

    protected virtual void OnApplicationQuit()
    {
        Instance = null;	
        Destroy(gameObject);
    }
}

public abstract class PersistentSingleton<T> : StaticSingleton<T> where T : MonoBehaviour
{
    protected override void Awake()
    {
        if (Instance != null && Instance.gameObject != null && Instance.gameObject != gameObject)
        {
            Destroy(gameObject);
            return;
        }
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }
}