using System;
using System.Reflection;
using UnityEditor;

namespace ToolKitsEditor
{

    public static class EditorTool
    {
        private static Type consoleWindow;

        [UnityEditor.Callbacks.OnOpenAsset(0)] //1 : 使用OnOpenAsset属性 接管当有资源打开时的操作
        private static bool OnOpenAssetSoundLog(int instanceID)
        {
            var asset = AssetDatabase.GetAssetPath(instanceID);
            if (!asset.Contains("Packages/dragon.toolkits/Runtime/Core/TLog.cs"))
            {
                return false;
            }

            if (consoleWindow == null) //反射出ConsoleWindow类
            {
                consoleWindow = Type.GetType("UnityEditor.ConsoleWindow,UnityEditor");
            }

            if (EditorWindow.focusedWindow.GetType() != consoleWindow)
            {
                return false;
            }

            var activeText =
                consoleWindow?.GetField("m_ActiveText", BindingFlags.Instance | BindingFlags.NonPublic);//m_ActiveText包含了当前Log的全部信息
            var consoleWindowFiledInfo =
                consoleWindow?.GetField("ms_ConsoleWindow", BindingFlags.Static | BindingFlags.NonPublic);//ms_ConsoleWindow 是ConsoleWindow的对象字段
            var consoleWindowInstance = consoleWindowFiledInfo?.GetValue(null);//从对象字段中得到这个对象
            var str = activeText?.GetValue(consoleWindowInstance).ToString();//得到Log信息,用于后面解析

            var (path, lineIndex) = GetSubStringInStackStr(str, 1);//解析出对应的.cs文件全路径  和 行号  因为笔者的Log第二个cs文件即正确位置  所以参数传1
            if (lineIndex == -1)
                return false;

            UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(path, lineIndex);//跳转到正确行号
            return true;
        }
        private static (string, int) GetSubStringInStackStr(string stackStr, int needIndex)
        {
            var lines = stackStr.Split(new string[] { "\n" }, StringSplitOptions.None);

            var tempIndex = 0;
            var count = lines.Length;
            for (int i = 0; i < count; i++)
            {
                string textBeforeFilePath = ") (at ";
                int filePathIndex = lines[i].IndexOf(textBeforeFilePath, StringComparison.Ordinal);
                if (filePathIndex > 0)
                {
                    filePathIndex += textBeforeFilePath.Length;
                    if (lines[i][filePathIndex] != '<')
                    {
                        string filePathPart = lines[i].Substring(filePathIndex);
                        int lineIndex = filePathPart.LastIndexOf(":", StringComparison.Ordinal);
                        if (lineIndex > 0)
                        {
                            int endLineIndex = filePathPart.LastIndexOf(")", StringComparison.Ordinal);
                            if (endLineIndex > 0)
                            {
                                string lineString =
                                    filePathPart.Substring(lineIndex + 1, (endLineIndex) - (lineIndex + 1));
                                string filePath = filePathPart.Substring(0, lineIndex);
                                if (tempIndex++ >= needIndex)
                                {
                                    return (filePath, int.Parse(lineString));
                                }
                            }
                        }
                    }
                }
            }

            return ("", -1);
        }
    }
}
