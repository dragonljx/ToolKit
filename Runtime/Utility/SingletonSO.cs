using UnityEditor;
using UnityEngine;

namespace ToolKits
{
    /// <summary>
    /// 创建用作数据传递的单例
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SingletonSO<T> : ScriptableObject where T : ScriptableObject
    {
        private static T _instance;
        private static object _lock = new object();

        public static T Instance
        {
            get
            {

                lock (_lock)
                {
                    try
                    {
                        if (_instance == null)
                        {
                            _instance = Resources.Load<T>($"ScriptableObject/{typeof(T).Name}");
                        }
                        if (_instance == null)
                        {
                            _instance = CreateInstance<T>();
#if UNITY_EDITOR
                            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                            {
                                AssetDatabase.CreateFolder("Assets", "Resources");
                            }

                            if (!AssetDatabase.IsValidFolder("Assets/Resources/ScriptableObject"))
                            {
                                AssetDatabase.CreateFolder("Assets/Resources", "ScriptableObject");
                            }

                            AssetDatabase.CreateAsset(_instance, $"Assets/Resources/ScriptableObject/{typeof(T).Name}.asset");
                            AssetDatabase.SaveAssets();
#endif
                        }

                    }
                    catch (System.Exception e)
                    {
                        TLog.LogError(e);
                    }

                    return _instance;
                }
            }

        }
    }
}
