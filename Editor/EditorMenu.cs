using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ToolKit.Editor
{
    public static class EditorMenu
    {
        [MenuItem(ToolGlobal.ToolKitMenuName + "创建新插件", priority = -1003)]
        public static void ShowExample()
        {
            CreatePackage wnd = EditorWindow.GetWindow<CreatePackage>();
            wnd.titleContent = new GUIContent("CreatePackage");
        }

        [MenuItem(ToolGlobal.ToolKitMenuName + "数值调试窗口", priority = -1002)]
        public static void ShowDebugValueWindow()
        {
            var wnd = EditorWindow.GetWindow<DebugValueWindow>();
            wnd.titleContent = new GUIContent("Debug Value Window");
        }

        [MenuItem(ToolGlobal.ToolKitMenuName + "图标合集", priority = -1001)]
        public static void EditorIconsOpen()
        {
#if UNITY_2018
            var w = GetWindow<EditorIcons>("Editor Icons");
#else
            var w = EditorWindow.CreateWindow<EditorIcons>("Editor Icons");
#endif
            w.ShowUtility();
            w.minSize = new Vector2(320, 450);
        }

        [MenuItem(ToolGlobal.ToolKitMenuName + "打开数据存储目录")]
        public static void OpenPersistentDataPath()
        {
            string path = Application.persistentDataPath;
            ToolKit.DLogger.Log("打开路径: " + path);

            System.Diagnostics.Process.Start(path);
        }

        private struct ReferencesInfo
        {
            public GameObject obj;
            public Component component;
            public SerializedProperty property;
        }

        [MenuItem(ToolGlobal.GameObjectMenuName + "查找引用该对象的脚本")]
        private static void FindReferencesFromContext()
        {
            GameObject target = Selection.activeGameObject;
            if (target == null)
            {
                EditorUtility.DisplayDialog("提示", "请先在 Hierarchy 中选择一个对象", "确定");
                return;
            }

            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            List<ReferencesInfo> infos = new List<ReferencesInfo>();
            bool hasFoundReference = true;

            foreach (var obj in allObjects)
            {
                if (obj == target) continue;
                Component[] components = obj.GetComponents<Component>();
                foreach (var component in components)
                {
                    if (component == null)
                        continue;

                    // 使用 SerializedObject 检查序列化字段
                    SerializedObject serializedObject = new SerializedObject(component);
                    SerializedProperty property = serializedObject.GetIterator();

                    while (property.NextVisible(true))
                    {
                        // 检查是否为 GameObject 或 Component 引用
                        if (property.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            Object reference = property.objectReferenceValue;

                            if (reference == null)
                                continue;

                            // 检查是否引用了目标对象或其组件
                            if (IsReferenceToTarget(reference, target))
                            {
                                ReferencesInfo info = new ReferencesInfo() { component = component, obj = obj, property = property };
                                infos.Add(info);

                                hasFoundReference = false;
                                break;
                            }
                        }
                    }
                }
            }
            if (hasFoundReference)
            {
                Debug.Log($"没有找到任何引用{target.name}的对象");
            }
            else
            {

                if (infos.Count == 1)
                {
                    EditorGUIUtility.PingObject(infos[0].obj);
                }
                foreach (var item in infos)
                {
                    Debug.Log(
                        $"<color=#7CFF6B><b>{target.name}</b></color> 被 " +
                        $"<color=#FFD166>{item.obj.name}</color> 上的 " +
                        $"<color=#66D9FF>{item.component.GetType().Name}</color> 组件中的 " +
                        $"<color=#FF7EB6>{item.property.propertyPath}</color> 字段引用",
                        item.obj
                    );
                }
            }
        }

        private static bool IsReferenceToTarget(Object reference, GameObject target)
        {
            // 直接引用 GameObject
            if (reference is GameObject go && go == target)
                return true;

            // 引用的是目标对象上的组件
            if (reference is Component comp && comp.gameObject == target)
                return true;

            // 引用的 Transform
            if (reference is Transform transform && transform.gameObject == target)
                return true;

            // 引用的 RectTransform
            if (reference is RectTransform rectTransform && rectTransform.gameObject == target)
                return true;

            return false;
        }
    }
}