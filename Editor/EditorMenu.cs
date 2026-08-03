using System.Diagnostics;
using ToolKit;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace ToolKitsEditor
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

            Process.Start(path);
        }

        [MenuItem(ToolGlobal.ToolKitMenuName + "卸载不常用插件", priority = 1000)]
        public static void RemovePackages()
        {
            Client.Remove("com.unity.collab-proxy");
            Client.Remove("com.unity.feature.development");
        }
    }
}