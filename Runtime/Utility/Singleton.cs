using UnityEngine;

namespace ToolKit
{
    /// <summary>
    /// MonoBehaviour 泛型单例基类
    /// </summary>
    /// <typeparam name="T">单例类型</typeparam>
    public class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        internal static T _instance;

        private static readonly object _lock = new object();

        /// <summary>
        /// 单例实例
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            CreateInstance();
                        }
                    }
                }

                return _instance;
            }

            protected set
            {
                _instance = value;
            }
        }

        /// <summary>
        /// 创建单例
        /// </summary>
        private static void CreateInstance()
        {
#if UNITY_2022_2_OR_NEWER

            _instance = FindFirstObjectByType<T>();

            T[] instances = FindObjectsByType<T>(
                FindObjectsSortMode.None
            );

#else

            _instance = FindObjectOfType<T>();

            T[] instances = FindObjectsOfType<T>();

#endif

            // 检查多个实例
            if (instances.Length > 1)
            {
                Debug.LogError(
                    $"[Singleton] 场景存在多个 {typeof(T).Name} 单例实例!"
                );

                return;
            }

            // 场景没有实例，自动创建
            if (_instance == null)
            {
                GameObject obj = new GameObject(
                    "(Singleton)" + typeof(T).Name
                );

                _instance = obj.AddComponent<T>();

                DontDestroyOnLoad(obj);
            }
            else
            {
                DontDestroyOnLoad(_instance.gameObject);
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;

                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Debug.LogWarning(
                    $"[Singleton] 销毁重复实例 {typeof(T).Name}"
                );

                Destroy(gameObject);
            }
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