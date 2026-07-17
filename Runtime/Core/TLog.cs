using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using UnityEngine;

namespace ToolKit
{

    public static partial class TLog
    {
        private static readonly ILogger UnityLogger = Debug.unityLogger;

        private const string DefaultTag = "ToolKit";
        private const string OrangeColorPrefix = "<color=orange>";
        private const string ColorSuffix = "</color>";

        // ──────────────────────────────────────────────
        // 公开配置
        // ──────────────────────────────────────────────

        /// <summary>是否在标签中显示当前帧号</summary>
        public static bool ShowFrameInTag = false;

        /// <summary>集合/字典最多显示的元素数量，超出部分截断并提示。默认 100</summary>
        public static int MaxCollectionItems = 100;

        /// <summary>对象嵌套递归最大深度，超出后直接调用 ToString()。默认 3</summary>
        public static int MaxDepth = 3;

        private static readonly ThreadLocal<StringBuilder> Builder =
            new ThreadLocal<StringBuilder>(() => new StringBuilder(256));

        #region Log

#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void Log(string message)
        {
            UnityLogger.Log(FormatTag(DefaultTag), message);
        }

#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void Log(object message)
        {
            UnityLogger.Log(FormatTag(DefaultTag), FormatObject(message));
        }

#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void Log(string tag, string message)
        {
            UnityLogger.Log(FormatTag(tag), message);
        }

#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void Log(string tag, object message)
        {
            UnityLogger.Log(FormatTag(tag), FormatObject(message));
        }
        #endregion


        #region Log Warning

#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void LogWarning(string message)
        {
            UnityLogger.LogWarning(FormatTag(DefaultTag), message);
        }

#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void LogWarning(object message)
        {
            UnityLogger.LogWarning(FormatTag(DefaultTag), FormatObject(message));
        }
#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void LogWarning(string tag, string message)
        {
            UnityLogger.LogWarning(FormatTag(tag), message);
        }

#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void LogWarning(string tag, object message)
        {
            UnityLogger.LogWarning(FormatTag(tag), FormatObject(message));
        }

        #endregion


        #region Log Error

#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void LogError(string message)
        {
            UnityLogger.LogError(FormatTag(DefaultTag), message);
        }

#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void LogError(object message)
        {
            UnityLogger.LogError(FormatTag(DefaultTag), FormatObject(message));
        }
#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void LogError(string tag, string message)
        {
            UnityLogger.LogError(FormatTag(tag), message);
        }

#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void LogError(string tag, object message)
        {
            UnityLogger.LogError(FormatTag(tag), FormatObject(message));
        }
        #endregion

        #region Log Exception

        /// <summary>输出异常日志，自动附加异常类型和堆栈信息</summary>
#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void LogException(Exception exception)
        {
            if (exception == null) return;
            UnityLogger.LogError(FormatTag(DefaultTag),
                $"<color=red>[{exception.GetType().Name}]</color> {exception.Message}\n{exception.StackTrace}");
        }

        /// <summary>输出异常日志（带标签），自动附加异常类型和堆栈信息</summary>
#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void LogException(string tag, Exception exception)
        {
            if (exception == null) return;
            UnityLogger.LogError(FormatTag(tag),
                $"<color=red>[{exception.GetType().Name}]</color> {exception.Message}\n{exception.StackTrace}");
        }

        /// <summary>输出异常日志（带标签和自定义消息）</summary>
#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void LogException(string tag, string message, Exception exception)
        {
            string errorMsg = message;
            if (exception != null)
            {
                errorMsg += $"\n<color=red>[{exception.GetType().Name}]</color> {exception.Message}\n{exception.StackTrace}";
            }
            UnityLogger.LogError(FormatTag(tag), errorMsg);
        }

        #endregion


        #region Formatting

        private static string FormatTag(string tag)
        {
            string baseTag = $"{OrangeColorPrefix}[{tag}]{ColorSuffix} ";

            return ShowFrameInTag
                ? $"{baseTag}[Frame: {Time.frameCount}]"
                : baseTag;
        }


        /// <summary>将任意对象格式化为带类型标签的可读字符串，支持集合和嵌套递归</summary>
        public static string FormatObject(object obj)
        {
            if (obj == null)
                return "null";

            StringBuilder sb = Builder.Value;
            sb.Clear();
            AppendObject(sb, obj, 0);
            return sb.ToString();
        }

        /// <summary>
        /// 递归格式化对象并追加到 StringBuilder。
        /// depth 从 0 开始，超过 MaxDepth 时降级为 ToString()。
        /// </summary>
        private static void AppendObject(StringBuilder sb, object obj, int depth)
        {
            if (obj == null)
            {
                sb.Append("null");
                return;
            }

            if (depth >= MaxDepth)
            {
                // 超过最大深度，直接输出 ToString 并标注截断
                sb.Append("<color=grey>[MaxDepth]</color> ");
                sb.Append(obj.ToString());
                return;
            }

            Type type = obj.GetType();

            // ── 类型标签 ──
            sb.Append("<color=lightblue>");
            sb.Append(type.Name);
            sb.Append("</color> : ");

            // ── 集合 / 字典 ──
            if (obj is IDictionary dict)
            {
                AppendDictionary(sb, dict, depth);
                return;
            }

            if (obj is IList list)
            {
                AppendList(sb, list, depth);
                return;
            }

            // ── Unity 内置类型 ──
            AppendUnityObject(sb, obj);
        }

        /// <summary>格式化 Unity 内置类型的关键属性</summary>
        private static void AppendUnityObject(StringBuilder sb, object obj)
        {
            switch (obj)
            {
                case GameObject go: 
                    sb.Append("Name: ");
                    sb.Append(go.name);
                    sb.Append(" | Active: ");
                    sb.Append(go.activeSelf);
                    sb.Append(" | Layer: ");
                    sb.Append(go.layer);
                    sb.Append(" | Tag: ");
                    sb.Append(go.tag);
                    break;

                case RectTransform rt: 
                    sb.Append("Name: ");
                    sb.Append(rt.name);
                    sb.Append(" | Pos: ");
                    sb.Append(rt.anchoredPosition);
                    sb.Append(" | Size: ");
                    sb.Append(rt.sizeDelta);
                    sb.Append(" | Pivot: ");
                    sb.Append(rt.pivot);
                    break;

                case Transform t: 
                    sb.Append("Name: ");
                    sb.Append(t.name);
                    sb.Append(" | P: ");
                    sb.Append(t.position);
                    sb.Append(" | R: ");
                    sb.Append(t.rotation.eulerAngles);
                    sb.Append(" | S: ");
                    sb.Append(t.localScale);
                    sb.Append(" | Child: ");
                    sb.Append(t.childCount);
                    break;

                case Camera cam: 
                    sb.Append("Name: ");
                    sb.Append(cam.name);
                    sb.Append(" | Depth: ");
                    sb.Append(cam.depth);
                    sb.Append(" | Fov: ");
                    sb.Append(cam.fieldOfView);
                    sb.Append(" | Clear: ");
                    sb.Append(cam.clearFlags);
                    break;

                case Renderer renderer: 
                    sb.Append("Name: ");
                    sb.Append(renderer.name);
                    sb.Append(" | Enabled: ");
                    sb.Append(renderer.enabled);
                    sb.Append(" | Bounds: ");
                    sb.Append(renderer.bounds.size);
                    sb.Append(" | Materials: ");
                    sb.Append(renderer.sharedMaterials.Length);
                    break;

                case Collider collider: 
                    sb.Append("Name: ");
                    sb.Append(collider.name);
                    sb.Append(" | Enabled: ");
                    sb.Append(collider.enabled);
                    sb.Append(" | Bounds: ");
                    sb.Append(collider.bounds.size);
                    break;

                case Rigidbody rb: 
                    sb.Append("Name: ");
                    sb.Append(rb.name);
                    sb.Append(" | Mass: ");
                    sb.Append(rb.mass);
                    sb.Append(" | Velocity: ");
                    sb.Append(rb.velocity);
                    sb.Append(" | Kinematic: ");
                    sb.Append(rb.isKinematic);
                    break;

                case Animator animator: 
                    sb.Append("Name: ");
                    sb.Append(animator.name);
                    sb.Append(" | Enabled: ");
                    sb.Append(animator.enabled);
                    sb.Append(" | Controller: ");
                    sb.Append(animator.runtimeAnimatorController?.name ?? "null");
                    break;

                case AudioSource audio: 
                    sb.Append("Name: ");
                    sb.Append(audio.name);
                    sb.Append(" | Clip: ");
                    sb.Append(audio.clip?.name ?? "null");
                    sb.Append(" | Volume: ");
                    sb.Append(audio.volume);
                    sb.Append(" | Playing: ");
                    sb.Append(audio.isPlaying);
                    break;

                case ParticleSystem ps: 
                    var main = ps.main;
                    sb.Append("Name: ");
                    sb.Append(ps.name);
                    sb.Append(" | Playing: ");
                    sb.Append(ps.isPlaying);
                    sb.Append(" | Particles: ");
                    sb.Append(ps.particleCount);
                    sb.Append(" | Duration: ");
                    sb.Append(main.duration);
                    break;

                case Material mat: 
                    sb.Append("Name: ");
                    sb.Append(mat.name);
                    sb.Append(" | Shader: ");
                    sb.Append(mat.shader?.name ?? "null");
                    sb.Append(" | Instancing: ");
                    sb.Append(mat.enableInstancing);
                    break;

                case Texture tex: 
                    sb.Append("Name: ");
                    sb.Append(tex.name);
                    sb.Append(" | Size: ");
                    sb.Append(tex.width);
                    sb.Append('x');
                    sb.Append(tex.height);
                    break;

                case Mesh mesh: 
                    sb.Append("Name: ");
                    sb.Append(mesh.name);
                    sb.Append(" | Vertices: ");
                    sb.Append(mesh.vertexCount);
                    sb.Append(" | Triangles: ");
                    sb.Append(mesh.triangles.Length / 3);
                    break;

                case ScriptableObject so: 
                    sb.Append("Name: ");
                    sb.Append(so.name);
                    break;

                case Component comp: 
                    sb.Append("GameObject: ");
                    sb.Append(comp.gameObject != null ? comp.gameObject.name :  "null");
                    break;

                default: 
                    sb.Append(obj);
                    break;
            }
        }

        #endregion

        #region Collection Formatting

        /// <summary>格式化值（不带类型标签）：简单类型直接输出，复杂类型委托给 AppendObject</summary>
        private static void AppendValue(StringBuilder sb, object obj, int depth)
        {
            if (obj == null)
            {
                sb.Append("null");
                return;
            }

            Type type = obj.GetType();

            // 简单类型（基元、string、decimal、枚举）直接输出，不套类型标签
            if (type.IsPrimitive || obj is string || obj is decimal)
            {
                sb.Append(obj);
                return;
            }

            // Unity 向量类型也直接输出（本身就有括号自描述）
            if (obj is Vector2 || obj is Vector3 || obj is Vector4 ||
                obj is Vector2Int || obj is Vector3Int ||
                obj is Quaternion || obj is Color || obj is Color32)
            {
                sb.Append(obj);
                return;
            }

            // 其他复杂类型递归处理
            AppendObject(sb, obj, depth);
        }

        /// <summary>格式化 List 及数组（实现 IList 接口）</summary>
        private static void AppendList(StringBuilder sb, IList list, int depth)
        {
            int count = list.Count;

            // ── 头部：Element 类型 + 数量 ──
            Type listType = list.GetType();
            string elementType = GetElementTypeName(listType);
            sb.Append("Element: ");
            sb.Append(elementType);
            sb.Append(" | Count: ");
            sb.Append(count);

            if (count == 0) return;

            int displayCount = Math.Min(count, MaxCollectionItems);
            for (int i = 0; i < displayCount; i++)
            {
                sb.Append('\n');
                AppendIndent(sb, depth + 1);
                sb.Append('[');
                sb.Append(i);
                sb.Append("] : ");
                AppendValue(sb, list[i], depth + 1);
            }

            if (count > MaxCollectionItems)
            {
                sb.Append('\n');
                AppendIndent(sb, depth + 1);
                sb.Append("<color=grey>... 还有 ");
                sb.Append(count - MaxCollectionItems);
                sb.Append(" 项未显示</color>");
            }
        }

        /// <summary>格式化 Dictionary 及 Hashtable（实现 IDictionary 接口）</summary>
        private static void AppendDictionary(StringBuilder sb, IDictionary dict, int depth)
        {
            int count = dict.Count;

            // ── 头部：Key/Value 类型 + 数量 ──
            Type dictType = dict.GetType();
            string keyType = "?";
            string valueType = "?";
            if (dictType.IsGenericType)
            {
                Type[] args = dictType.GetGenericArguments();
                if (args.Length == 2)
                {
                    keyType = args[0].Name;
                    valueType = args[1].Name;
                }
            }
            sb.Append("Key: ");
            sb.Append(keyType);
            sb.Append(" | Value: ");
            sb.Append(valueType);
            sb.Append(" | Count: ");
            sb.Append(count);

            if (count == 0) return;

            int i = 0;
            int displayCount = Math.Min(count, MaxCollectionItems);
            foreach (DictionaryEntry entry in dict)
            {
                if (i >= displayCount) break;

                sb.Append('\n');
                AppendIndent(sb, depth + 1);
                sb.Append("[ ");
                AppendValue(sb, entry.Key, depth + 1);
                sb.Append(" : ");
                AppendValue(sb, entry.Value, depth + 1);
                sb.Append(" ]");

                i++;
            }

            if (count > MaxCollectionItems)
            {
                sb.Append('\n');
                AppendIndent(sb, depth + 1);
                sb.Append("<color=grey>... 还有 ");
                sb.Append(count - MaxCollectionItems);
                sb.Append(" 项未显示</color>");
            }
        }

        /// <summary>格式化非 List/非 Dictionary 的 IEnumerable 类型</summary>
        private static void AppendEnumerable(StringBuilder sb, IEnumerable enumerable, int depth)
        {
            int count = 0;
            var items = new List<object>();

            foreach (object item in enumerable)
            {
                items.Add(item);
                if (items.Count >= MaxCollectionItems + 1)
                    break;
            }

            count = items.Count;
            bool hasMore = count > MaxCollectionItems;
            int displayCount = hasMore ? MaxCollectionItems : count;

            // ── 头部 ──
            Type enumType = enumerable.GetType();
            string elementType = GetElementTypeName(enumType);
            sb.Append("Element: ");
            sb.Append(elementType);
            sb.Append(" | Count: ");
            sb.Append(hasMore ? $">{MaxCollectionItems}" : count.ToString());

            if (count == 0) return;

            for (int i = 0; i < displayCount; i++)
            {
                sb.Append('\n');
                AppendIndent(sb, depth + 1);
                sb.Append('[');
                sb.Append(i);
                sb.Append("] ");
                AppendValue(sb, items[i], depth + 1);
            }

            if (hasMore)
            {
                sb.Append('\n');
                AppendIndent(sb, depth + 1);
                sb.Append("<color=grey>... 还有更多项未显示</color>");
            }
        }

        /// <summary>从集合类型中提取元素类型名称</summary>
        private static string GetElementTypeName(Type collectionType)
        {
            if (collectionType.IsArray)
                return collectionType.GetElementType()?.Name ?? "?";

            if (collectionType.IsGenericType)
            {
                Type[] args = collectionType.GetGenericArguments();
                if (args.Length > 0)
                    return args[0].Name;
            }

            return "?";
        }

        /// <summary>添加缩进空格</summary>
        private static void AppendIndent(StringBuilder sb, int depth)
        {
            int spaces = depth * 2;
            for (int i = 0; i < spaces; i++)
                sb.Append(' ');
        }

        #endregion

    }
}
