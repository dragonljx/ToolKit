using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace ToolKitsEditor
{
    public class PackageContextMenu
    {
        // Validate：控制菜单显示
        [MenuItem("Assets/卸载插件包", true)]
        private static bool ValidatePackage()
        {
            Object[] selectedObjects = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
            if (selectedObjects.Length != 1) return false;
            string path = AssetDatabase.GetAssetPath(selectedObjects[0]);
            return path.StartsWith("Packages/");
            ;
        }

        // 点击菜单执行
        [MenuItem("Assets/卸载插件包", false)]
        private static void DoSomethingWithPackage()
        {
            Object[] selectedObjects = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
            if (selectedObjects.Length != 1) return;
            string path = AssetDatabase.GetAssetPath(selectedObjects[0]);

            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                Debug.LogError("请选择一个有效的文件夹");
                return;
            }

            string[] parts = path.Split('/');
            if (parts.Length > 1)
            {
                string packageName = parts[1];

                RemoveRequest removeRequest = Client.Remove(packageName);
                // 等待卸载请求完成
                while (!removeRequest.IsCompleted)
                {
                   
                }

                // 检查卸载请求的状态
                if (removeRequest.Status == StatusCode.Success)
                {
                    Debug.Log("包已成功卸载: " + packageName);
                }
                else
                {
                    Debug.LogError("卸载包失败: " + removeRequest.Error.message);
                }
            }
        }
    }
}