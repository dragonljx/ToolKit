using UnityEngine;

namespace ToolKits
{
    /// <summary>
    /// 单例类
    /// </summary>
    /// <typeparam name="T">单例的类型</typeparam>
    public class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                try
                {
                    if (_instance == null)
                    {
                        _instance = (T)FindFirstObjectByType(typeof(T));

                        if (FindObjectsByType(typeof(T), FindObjectsSortMode.None).Length > 1)
                        {
                            Debug.LogError("单例出错了，出现了多个单例，重开场景解决问题！");
                            return _instance;
                        }

                        if (_instance == null)
                        {
                            GameObject singleton = new GameObject("(Singleton) " + typeof(T).ToString());
                            _instance = singleton.AddComponent<T>();

                            DontDestroyOnLoad(singleton);

                            //Debug.LogFormat("场景中没有脚本({0})，所以创建({1})脚本.", typeof(T), singleton);
                        }
                        else
                        {
                            DontDestroyOnLoad(_instance.gameObject);
                            //Debug.Log("[Singleton] 已经创建单例: " + _instance.GetType().Name);
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("不能在线程中初始化单例！" + e);
                }

                return _instance;
            }
            set
            {
                if (_instance != null)
                {
                    Destroy(value);
                    return;
                }

                _instance = value;
                DontDestroyOnLoad(_instance);
            }
        }
    }
}