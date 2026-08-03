using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using UnityEngine;

namespace ToolKit
{
    public static partial class DLogger
    {
        private static readonly ILogger UnityLogger = Debug.unityLogger;

        private const string OrangeColorPrefix = "<color=orange>";
        private const string ColorSuffix = "</color>";
        private const string LightBlueColor = "<color=lightblue>";
        private const string DimColor = "<color=grey>";
        private const string ErrorColor = "<color=red>";

        /// <summary>仅在 Editor 中追加颜色标签，Build 中整个调用被编译器移除</summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private static void AppendColor(StringBuilder sb, string colorTag)
        {
            sb.Append(colorTag);
        }

        private static readonly Dictionary<Type, string> FriendlyTypeNames = new()
        {
            { typeof(int), "Int" },
            { typeof(float), "Float" },
            { typeof(double), "Double" },
            { typeof(bool), "Bool" },
            { typeof(string), "String" },
            { typeof(object), "Object" },
            { typeof(long), "Long" },
            { typeof(short), "Short" },
            { typeof(byte), "Byte" }
        };
        // ──────────────────────────────────────────────
        // 公开配置
        // ──────────────────────────────────────────────

        /// <summary>是否在标签中显示当前帧号</summary>
        public static bool ShowFrameInTag = false;

        /// <summary> 在标签中显示当前时间戳，格式为 [HH:mm:ss.fff]，默认 false/// </summary>
        public static bool ShowTimeInTag = false;

        /// <summary>集合/字典最多显示的元素数量，超出部分截断并提示。默认 100</summary>
        public static int MaxCollectionItems = 100;

        /// <summary>对象嵌套递归最大深度，超出后直接调用 ToString()。默认 3</summary>
        public static int MaxDepth = 3;

        private static readonly ThreadLocal<StringBuilder> Builder =
            new ThreadLocal<StringBuilder>(() => new StringBuilder(256));

        private static readonly ThreadLocal<HashSet<object>> VisitedObjects =
            new ThreadLocal<HashSet<object>>(() => new HashSet<object>(ReferenceEqualityComparer.Instance));

        private static int mainThreadId;
        private static bool isInitialized = false;
        private static bool isMainThread => Environment.CurrentManagedThreadId == mainThreadId;


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (!isInitialized)
            {
                InitializeOnMainThread();
            }
        }
        // 提供显式初始化方法，供需要精确控制的场景使用
        public static void Init()
        {
            if (!isInitialized)
            {
                InitializeOnMainThread();
            }
        }
        private static void InitializeOnMainThread()
        {
            mainThreadId = Environment.CurrentManagedThreadId;
            isInitialized = true;
        }

        #region Log

#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void Log(string message)
        {
            UnityLogger.Log(FormatMessage(message));
        }


#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void Log(object message)
        {
            UnityLogger.Log(FormatMessage(message));
        }


#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void Log(string tag, string message)
        {
            UnityLogger.Log(FormatTagAndMessage(tag, message));
        }


#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void Log(string tag, object message)
        {
            UnityLogger.Log(FormatTagAndMessage(tag, message));
        }




        #endregion

        #region Log Warning


#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void LogWarning(string message)
        {
            UnityLogger.Log(LogType.Warning, FormatMessage(message));
        }


#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void LogWarning(object message)
        {
            UnityLogger.Log(LogType.Warning, FormatMessage(message));
        }


#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void LogWarning(string tag, string message)
        {
            UnityLogger.Log(LogType.Warning, FormatTagAndMessage(tag, message));
        }


#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void LogWarning(string tag, object message)
        {
            UnityLogger.Log(LogType.Warning, FormatTagAndMessage(tag, message));
        }





        #endregion

        #region Log Error


#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void LogError(string message)
        {
            UnityLogger.Log(LogType.Error, FormatMessage(message));
        }


#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void LogError(object message)
        {
            UnityLogger.Log(LogType.Error, FormatMessage(message));
        }


#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void LogError(string tag, string message)
        {
            UnityLogger.Log(LogType.Error, FormatTagAndMessage(tag, message));
        }

#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void LogError(string tag, object message)
        {
            UnityLogger.Log(LogType.Error, FormatTagAndMessage(tag, message));
        }

        #endregion

        #region Log Exception


        /// <summary>
        /// 输出异常日志，自动附加异常类型和堆栈信息
        /// </summary>
#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void LogException(System.Exception exception)
        {
            if (exception == null)
                return;

            UnityLogger.Log(LogType.Exception, FormatMessage(exception));
        }


        /// <summary>
        /// 输出异常日志（带标签）
        /// </summary>
#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void LogException(string tag, Exception exception)
        {
            if (exception == null)
                return;

            UnityLogger.Log(LogType.Exception, FormatTagAndMessage(tag, exception));
        }

        #endregion

        #region Formatting

        public static string FormatMessage(string message)
        {
            StringBuilder sb = Builder.Value;
            sb.Clear();

            AppendTimeStamp(sb);
            AppendFrameInfo(sb);

            sb.Append(' ');
            sb.Append(message);

            return sb.ToString();
        }
        public static string FormatMessage(object obj)
        {
            HashSet<object> visited = VisitedObjects.Value;
            visited.Clear();

            StringBuilder sb = Builder.Value;
            sb.Clear();

            AppendTimeStamp(sb);
            AppendFrameInfo(sb);

            sb.Append(' ');

            AppendObject(sb, obj, 0, visited);

            return sb.ToString();
        }

        /// <summary>
        /// 格式化日志标签和字符串消息
        /// </summary>
        public static string FormatTagAndMessage(string tag, string message)
        {
            StringBuilder sb = Builder.Value;
            sb.Clear();

            AppendTimeStamp(sb);
            AppendFrameInfo(sb);

            FormatTag(sb, tag);

            sb.Append(' ');
            sb.Append(message);

            return sb.ToString();
        }


        /// <summary>
        /// 格式化日志标签和对象
        /// </summary>
        public static string FormatTagAndMessage(string tag, object obj)
        {
            HashSet<object> visited = VisitedObjects.Value;
            visited.Clear();

            StringBuilder sb = Builder.Value;
            sb.Clear();

            AppendTimeStamp(sb);
            AppendFrameInfo(sb);

            FormatTag(sb, tag);

            sb.Append(' ');

            AppendObject(sb, obj, 0, visited);

            return sb.ToString();
        }


        public static string FormatTagAndMessage(string tag, Exception exception)
        {

            StringBuilder sb = Builder.Value;
            sb.Clear();

            AppendTimeStamp(sb);
            AppendFrameInfo(sb);

            FormatTag(sb, tag);

            AppendColor(sb, ErrorColor);
            sb.Append(exception.GetType().Name);
            AppendColor(sb, ColorSuffix);

            sb.AppendLine(exception.Message);
            sb.Append(exception.StackTrace);

            return sb.ToString();
        }


        private static void FormatTag(StringBuilder sb, string tag)
        {
            AppendColor(sb, OrangeColorPrefix);
            sb.Append(" [");
            sb.Append(tag);
            sb.Append("] ");
            AppendColor(sb, ColorSuffix);
        }

        /// <summary>
        /// 递归格式化对象并追加到 StringBuilder。
        /// depth 从 0 开始，超过 MaxDepth 时降级为 ToString()。
        /// </summary>
        private static void AppendObject(StringBuilder sb, object obj, int depth, HashSet<object> visited)
        {
            if (obj == null)
            {
                sb.Append("null");
                return;
            }

            if (obj is string s)
            {
                sb.Append(s);
                return;
            }

            // ── 循环引用检测 ──
            Type type = obj.GetType();
            if (!type.IsValueType && obj is not string)
            {
                if (!visited.Add(obj))
                {
                    AppendColor(sb, DimColor);
                    sb.Append("[Circular Reference]");
                    AppendColor(sb, ColorSuffix);
                    return;
                }
            }

            if (depth >= MaxDepth)
            {
                // 超过最大深度，直接输出 ToString 并标注截断
                AppendColor(sb, DimColor);
                sb.Append("[MaxDepth]");
                AppendColor(sb, ColorSuffix);
                sb.Append(" ");
                sb.Append(obj.ToString());
                return;
            }

            // ── 类型标签 ──
            AppendColor(sb, LightBlueColor);
            GetFriendlyName(sb, type);
            AppendColor(sb, ColorSuffix);
            sb.Append(" : ");

            if (obj is UnityEngine.Object)
            {
                AppendUnityObject(sb, obj);
                return;
            }

            // ── 集合 / 字典 ──
            if (obj is IDictionary dict)
            {
                AppendDictionary(sb, dict, depth, visited);
                return;
            }

            if (obj is IList list)
            {
                AppendList(sb, list, depth, visited);
                return;
            }

            if (obj is IEnumerable enumerable)
            {
                AppendEnumerable(sb, enumerable, depth, visited);
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
                    sb.Append(mesh.GetIndexCount(0) / 3);
                    break;

                case ScriptableObject so:
                    sb.Append("Name: ");
                    sb.Append(so.name);
                    break;

                case Component comp:
                    sb.Append("GameObject: ");
                    sb.Append(comp.gameObject != null ? comp.gameObject.name : "null");
                    break;

                default:
                    sb.Append(obj.ToString());
                    break;
            }
        }

        #endregion Formatting

        #region Collection Formatting

        /// <summary>格式化值（不带类型标签）：简单类型直接输出，复杂类型委托给 AppendObject</summary>
        private static void AppendValue(StringBuilder sb, object obj, int depth, HashSet<object> visited)
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
            AppendObject(sb, obj, depth, visited);
        }

        /// <summary>格式化 List 及数组（实现 IList 接口）</summary>
        private static void AppendList(StringBuilder sb, IList list, int depth, HashSet<object> visited)
        {
            int count = list.Count;

            // ── 数量 ──
            sb.Append(" Count: ");
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
                AppendValue(sb, list[i], depth + 1, visited);
            }

            if (count > MaxCollectionItems)
            {
                sb.Append('\n');
                AppendIndent(sb, depth + 1);

                sb.Append("... and ");
                sb.Append(count - MaxCollectionItems);
                sb.Append(" more items not shown");

            }
        }

        /// <summary>格式化 Dictionary 及 Hashtable（实现 IDictionary 接口）</summary>
        private static void AppendDictionary(StringBuilder sb, IDictionary dict, int depth, HashSet<object> visited)
        {
            int count = dict.Count;

            // ── 数量 ──

            sb.Append(" Count: ");
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
                AppendValue(sb, entry.Key, depth + 1, visited);
                sb.Append(" : ");
                AppendValue(sb, entry.Value, depth + 1, visited);
                sb.Append(" ]");

                i++;
            }

            if (count > MaxCollectionItems)
            {
                sb.Append('\n');
                AppendIndent(sb, depth + 1);

                sb.Append("... and ");
                sb.Append(count - MaxCollectionItems);
                sb.Append(" more items not shown");

            }
        }

        /// <summary>格式化非 List/非 Dictionary 的 IEnumerable 类型</summary>
        private static void AppendEnumerable(StringBuilder sb, IEnumerable enumerable, int depth, HashSet<object> visited)
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
            sb.Append(" Count: ");
            sb.Append(hasMore ? $">{MaxCollectionItems}" : count.ToString());

            if (count == 0) return;

            for (int i = 0; i < displayCount; i++)
            {
                sb.Append('\n');
                AppendIndent(sb, depth + 1);
                sb.Append('[');
                sb.Append(i);
                sb.Append("] ");
                AppendValue(sb, items[i], depth + 1, visited);
            }

            if (hasMore)
            {
                sb.Append('\n');
                AppendIndent(sb, depth + 1);

                sb.Append("... and more not shown");

            }
        }

        /// <summary>添加缩进空格</summary>
        private static void AppendIndent(StringBuilder sb, int depth)
        {
            int spaces = depth * 2;
            for (int i = 0; i < spaces; i++)
                sb.Append(' ');
        }

        #endregion Collection Formatting

        /// <summary>引用相等比较器，用于循环引用检测</summary>
        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Instance =
                new ReferenceEqualityComparer();

            public new bool Equals(object x, object y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(object obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
        private static void AppendTimeStamp(StringBuilder sb)
        {
            if (ShowTimeInTag)
            {
                string absTime = DateTime.Now.ToString("HH:mm:ss.fff");
                sb.Append(absTime);
            }
        }
        private static void AppendFrameInfo(StringBuilder sb)
        {
            if (ShowFrameInTag && isMainThread)
            {
                sb.Append(" F:");
                sb.Append(Time.frameCount);

            }
        }

        private static void GetFriendlyName(StringBuilder sb, Type type)
        {
            if (type == null)
                return;

            if (FriendlyTypeNames.TryGetValue(type, out var name))
            {
                sb.Append(name);
                return;
            }

            if (type.IsArray)
            {
                GetFriendlyName(sb, type.GetElementType());
                sb.Append("[]");
                return;
            }

            if (!type.IsGenericType)
            {
                sb.Append(type.Name);
                return;
            }

            name = type.Name;

            var index = name.IndexOf('`');

            if (index > 0)
                name = name.Substring(0, index);

            sb.Append(name);
            sb.Append('<');

            var args = type.GetGenericArguments();

            for (int i = 0; i < args.Length; i++)
            {
                if (i > 0)
                    sb.Append(", ");

                GetFriendlyName(sb, args[i]);
            }

            sb.Append('>');
        }
    }
}