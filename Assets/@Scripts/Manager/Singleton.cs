using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
   private static T _instance;
    public static T Instance
    {
        get
        {
            // ó�� ����� ����
            if (_instance == null)
            {
                _instance = (T)GameObject.FindAnyObjectByType(typeof(T));

                if (_instance == null)
                {
                    GameObject go = new GameObject(); // typeof(T).Name, typeof(T)
                    T t = go.AddComponent<T>();
                    t.name = $"@{typeof(T).Name}";

                    _instance = t;
                }
                DontDestroyOnLoad(_instance.gameObject);

            }
            return _instance;
        }
    }
}