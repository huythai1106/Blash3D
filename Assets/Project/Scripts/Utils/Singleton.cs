using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    protected static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<T>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject();
                    DontDestroyOnLoad(obj);
                    _instance = obj.AddComponent<T>();
                }
            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        _instance = this as T;
    }
}
