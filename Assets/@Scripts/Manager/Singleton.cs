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
                
            }
            return _instance;
        }
    }

    /// <summary>
    /// 씬 전환 시 파괴되어야 하는지 여부를 반환합니다.
    /// true를 반환하면 DontDestroyOnLoad가 적용되지 않습니다.
    /// </summary>
    protected virtual bool ShouldDestroyOnLoad()
    {
        return false; // 기본값: DontDestroyOnLoad 적용
    }
}