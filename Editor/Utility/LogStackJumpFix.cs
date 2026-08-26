#if !UNITY_2022_2_OR_NEWER
// Unity 2022.2 及以上版本请直接在 DLogger 的方法上使用 [HideInCallstack] 特性,
// 原生支持双击跳转到真正调用者,不再需要这套反射方案。
using System;
using System.Reflection;
using UnityEditor;

namespace ToolKit.Editor
{
    /// <summary>
    /// 双击 Console 面板中由 DLogger 打印的日志时,自动跳转到真正的调用者代码行,
    /// 而不是跳转到 DLogger.cs 内部。
    /// 原理:拦截 OnOpenAsset 回调,反射读取 ConsoleWindow 当前选中日志的完整堆栈文本,
    /// 解析出第一个不属于日志包装文件的堆栈帧并跳转。
    /// 仅用于 Unity 2022.2 以下版本,新版本请改用 [HideInCallstack] 特性。
    /// </summary>
    public static class LogStackJumpFix
    {
        // 所有被视为"日志包装文件"的文件名,双击这些文件会尝试跳转到真正调用者
        private static readonly string[] LogWrapperFileNames =
        {
            "DLogger.cs",
        };

        private static Type _consoleWindowType;
        private static FieldInfo _activeTextField;
        private static FieldInfo _consoleWindowInstanceField;
        private static bool _reflectionInitFailed;

        [UnityEditor.Callbacks.OnOpenAsset(0)]
        private static bool OnOpenAssetSoundLog(int instanceID)
        {
            var asset = AssetDatabase.GetAssetPath(instanceID);
            if (string.IsNullOrEmpty(asset) || !IsLogWrapperFile(asset))
                return false;

            if (!TryInitReflection())
                return false;

            if (EditorWindow.focusedWindow == null ||
                EditorWindow.focusedWindow.GetType() != _consoleWindowType)
                return false;

            string stackStr;
            try
            {
                var consoleWindowInstance = _consoleWindowInstanceField.GetValue(null);
                stackStr = _activeTextField.GetValue(consoleWindowInstance)?.ToString();
            }
            catch
            {
                // 反射字段在当前 Unity 版本下拿不到,静默回退,交给默认行为
                return false;
            }

            if (string.IsNullOrEmpty(stackStr))
                return false;

            var (path, lineIndex) = GetFirstNonWrapperFrame(stackStr);
            if (lineIndex == -1 || string.IsNullOrEmpty(path))
                return false;

            UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(path, lineIndex);
            return true;
        }

        private static bool IsLogWrapperFile(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;

            foreach (var fileName in LogWrapperFileNames)
            {
                // 用文件名匹配而非完整路径匹配:
                // AssetDatabase 返回的是 Packages/... 虚拟路径,
                // 而堆栈文本里记录的可能是 Library/PackageCache/...@hash/... 物理路径,
                // 两者前缀不同,只有文件名是一致的。
                if (assetPath.EndsWith("/" + fileName, StringComparison.OrdinalIgnoreCase)
                    || assetPath.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static bool TryInitReflection()
        {
            if (_reflectionInitFailed)
                return false;
            if (_consoleWindowType != null)
                return true;

            try
            {
                _consoleWindowType = Type.GetType("UnityEditor.ConsoleWindow,UnityEditor");
                if (_consoleWindowType == null)
                {
                    _reflectionInitFailed = true;
                    return false;
                }

                _activeTextField = _consoleWindowType.GetField("m_ActiveText",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                _consoleWindowInstanceField = _consoleWindowType.GetField("ms_ConsoleWindow",
                    BindingFlags.Static | BindingFlags.NonPublic);

                if (_activeTextField == null || _consoleWindowInstanceField == null)
                {
                    // 当前 Unity 版本内部字段名变了,标记失败,后续不再重复尝试
                    _reflectionInitFailed = true;
                    return false;
                }

                return true;
            }
            catch
            {
                _reflectionInitFailed = true;
                return false;
            }
        }

        /// <summary>
        /// 从堆栈文本中跳过所有属于"日志包装文件"的帧,
        /// 返回第一个真正调用者所在的文件路径和行号。
        /// </summary>
        private static (string path, int line) GetFirstNonWrapperFrame(string stackStr)
        {
            var lines = stackStr.Split(new[] { "\n" }, StringSplitOptions.None);
            const string textBeforeFilePath = ") (at ";

            foreach (var rawLine in lines)
            {
                int filePathIndex = rawLine.IndexOf(textBeforeFilePath, StringComparison.Ordinal);
                if (filePathIndex <= 0)
                    continue;

                filePathIndex += textBeforeFilePath.Length;
                if (filePathIndex >= rawLine.Length || rawLine[filePathIndex] == '<')
                    continue; // 内部帧(无源码路径),跳过

                string filePathPart = rawLine.Substring(filePathIndex);
                int lineSepIndex = filePathPart.LastIndexOf(":", StringComparison.Ordinal);
                if (lineSepIndex <= 0)
                    continue;

                int endIndex = filePathPart.LastIndexOf(")", StringComparison.Ordinal);
                if (endIndex <= lineSepIndex)
                    continue;

                string lineString = filePathPart.Substring(lineSepIndex + 1, endIndex - (lineSepIndex + 1));
                string filePath = filePathPart.Substring(0, lineSepIndex);

                if (!int.TryParse(lineString, out int lineNumber))
                    continue;

                if (IsLogWrapperFile(filePath))
                    continue; // 属于日志包装文件本身,继续找下一帧

                return (filePath, lineNumber);
            }

            return ("", -1);
        }
    }
}
#endif