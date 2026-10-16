using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using UnityEngine;

namespace ToolKit
{
    /// <summary>
    /// 带标签、时间戳和对象深度展开的日志封装
    /// <para>用法：直接调用静态方法即可，无需初始化，首次访问前会自动完成主线程登记</para>
    /// <para>传入 object 重载时会递归展开对象的字段和属性，而不是只调用 ToString()</para>
    /// <code>
    /// // 不带标签
    /// DLogger.Log("初始化完成");
    ///
    /// // 带标签，标签会以橙色 [Tag] 形式显示在消息前面
    /// DLogger.Log("Game", "初始化完成");
    /// DLogger.LogWarning("Network", "连接超时");
    /// DLogger.LogError("Save", "保存失败");
    ///
    /// // 传对象：递归展开，而不是只打印类型名
    /// DLogger.Log("Player", playerData);
    ///
    /// // 异常：自动附加异常类型名和堆栈
    /// DLogger.LogException("Network", exception);
    /// </code>
    /// <para>输出格式为 <c>[时间戳] F:帧号 [标签] 消息</c>，时间戳和帧号默认关闭，</para>
    /// <para>分别由 <see cref="ShowTimeInTag"/> 和 <see cref="ShowFrameInTag"/> 控制</para>
    /// </summary>
    /// <remarks>
    /// <para>关于颜色标签：</para>
    /// <list type="bullet">
    /// <item>所有 &lt;color&gt; 标签都由 <c>[Conditional("UNITY_EDITOR")]</c> 方法追加，打包后整个调用被编译器移除</item>
    /// <item>因此 Build 中的日志是纯文本，不会有额外的字符串拼接开销，也不需要手动剥离富文本</item>
    /// <item>这也意味着 Build 里日志长度会比 Editor 中短，排查线上问题时不要依赖颜色做区分</item>
    /// </list>
    /// <para>关于线程安全：</para>
    /// <para>格式化用的 <c>StringBuilder</c> 和循环引用检测用的 <c>HashSet</c> 都是 <c>ThreadLocal</c>，</para>
    /// <para>所以可以从后台线程直接调用；但帧号只在主线程输出（后台线程取 <c>Time.frameCount</c> 无意义）。</para>
    /// <para>展开对象时会做循环引用检测，并按 <see cref="MaxDepth"/> 限制递归深度，</para>
    /// <para>集合和字典按 <see cref="MaxCollectionItems"/> 截断，避免日志本身导致卡顿。</para>
    /// <para>关于不可重入：</para>
    /// <para>每次格式化都复用当前线程的同一个 <c>StringBuilder</c>。所以不要在被展开对象的 <c>ToString()</c> 里</para>
    /// <para>再调用 <c>DLogger</c> 的日志方法或 <c>Format*</c> 方法，否则缓冲区会在外层格式化进行到一半时被清空，</para>
    /// <para>导致输出错乱。需要自定义展开格式时，请提前算好字符串再作为 <c>string</c> 传入（string 重载不展开对象）。</para>
    /// </remarks>
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

        /// <summary>
        /// 是否在标签中显示当前帧号
        /// <para>开启后输出 <c>F:123</c> 形式的前缀，默认 false</para>
        /// <para>仅在主线程生效，后台线程调用时不输出，因为 <c>Time.frameCount</c> 对它们没有意义</para>
        /// </summary>
        public static bool ShowFrameInTag = false;

        /// <summary>
        /// 是否在标签中显示当前时间戳
        /// <para>开启后输出 <c>[HH:mm:ss.fff]</c> 形式的前缀，默认 false</para>
        /// <para>取的是本机墙钟时间而非游戏运行时长，跨线程调用同样有效</para>
        /// </summary>
        public static bool ShowTimeInTag = false;

        /// <summary>
        /// 集合/字典最多显示的元素数量，超出部分截断并提示。默认 100
        /// <para>调大可以看全数据，但超大集合会显著拖慢日志格式化，排查完建议还原</para>
        /// </summary>
        public static int MaxCollectionItems = 100;

        /// <summary>
        /// 对象嵌套递归最大深度，超出后直接调用 ToString()。默认 3
        /// <para>Unity 的组件引用层级通常较深，深度越大越可能触发大段输出</para>
        /// </summary>
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
        /// <summary>
        /// 显式初始化日志系统，登记当前线程为主线程
        /// <para>正常情况下不需要调用：<c>AutoInitialize</c> 会在场景加载前（<c>BeforeSceneLoad</c>）自动完成</para>
        /// <para>仅在需要精确控制初始化时机的场景下手动调用，例如在更早的静态构造或编辑器脚本中打日志</para>
        /// <para>重复调用是安全的，已初始化时直接返回</para>
        /// </summary>
        /// <remarks>
        /// <para>关于主线程登记：</para>
        /// <para>登记的是「第一次初始化时所在的线程」，之后只有该线程调用才会输出帧号。</para>
        /// <para>如果日志系统是在后台线程首次触发的，帧号会一直缺失，此时应显式在主线程调用一次 <see cref="Init"/>。</para>
        /// </remarks>
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

        /// <summary>
        /// 输出普通日志
        /// <para>配合 <see cref="ShowTimeInTag"/> / <see cref="ShowFrameInTag"/> 输出时间戳与帧号前缀</para>
        /// </summary>
        /// <param name="message">日志内容，作为纯文本原样输出，不参与对象展开</param>
#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void Log(string message)
        {
            UnityLogger.Log(FormatMessage(message));
        }


        /// <summary>
        /// 输出普通日志，并递归展开对象内容
        /// </summary>
        /// <param name="message">任意对象，会展开其字段与属性，而非仅调用 ToString()</param>
#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void Log(object message)
        {
            UnityLogger.Log(FormatMessage(message));
        }


        /// <summary>
        /// 输出带标签的普通日志
        /// <para>标签会渲染为橙色的 <c>[Tag]</c> 前缀，Editor 中用于快速扫读和过滤</para>
        /// </summary>
        /// <param name="tag">标签文本，建议使用固定短词（如模块名）便于 Console 过滤</param>
        /// <param name="message">日志内容，作为纯文本原样输出</param>
#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void Log(string tag, string message)
        {
            UnityLogger.Log(FormatTagAndMessage(tag, message));
        }


        /// <summary>
        /// 输出带标签的普通日志，并递归展开对象内容
        /// </summary>
        /// <param name="tag">标签文本，渲染为橙色 <c>[Tag]</c> 前缀</param>
        /// <param name="message">任意对象，会展开其字段与属性</param>
#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void Log(string tag, object message)
        {
            UnityLogger.Log(FormatTagAndMessage(tag, message));
        }




        #endregion

        #region Log Warning


        /// <summary>
        /// 输出警告日志，在 Console 中显示为黄色三角图标
        /// </summary>
        /// <param name="message">日志内容，作为纯文本原样输出</param>
#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void LogWarning(string message)
        {
            UnityLogger.Log(LogType.Warning, FormatMessage(message));
        }


        /// <summary>
        /// 输出警告日志，并递归展开对象内容
        /// </summary>
        /// <param name="message">任意对象，会展开其字段与属性</param>
#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void LogWarning(object message)
        {
            UnityLogger.Log(LogType.Warning, FormatMessage(message));
        }


        /// <summary>
        /// 输出带标签的警告日志
        /// </summary>
        /// <param name="tag">标签文本，渲染为橙色 <c>[Tag]</c> 前缀</param>
        /// <param name="message">日志内容，作为纯文本原样输出</param>
#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void LogWarning(string tag, string message)
        {
            UnityLogger.Log(LogType.Warning, FormatTagAndMessage(tag, message));
        }


        /// <summary>
        /// 输出带标签的警告日志，并递归展开对象内容
        /// </summary>
        /// <param name="tag">标签文本，渲染为橙色 <c>[Tag]</c> 前缀</param>
        /// <param name="message">任意对象，会展开其字段与属性</param>
#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void LogWarning(string tag, object message)
        {
            UnityLogger.Log(LogType.Warning, FormatTagAndMessage(tag, message));
        }





        #endregion

        #region Log Error


        /// <summary>
        /// 输出错误日志，在 Console 中显示为红色图标
        /// </summary>
        /// <param name="message">日志内容，作为纯文本原样输出</param>
#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void LogError(string message)
        {
            UnityLogger.Log(LogType.Error, FormatMessage(message));
        }


        /// <summary>
        /// 输出错误日志，并递归展开对象内容
        /// </summary>
        /// <param name="message">任意对象，会展开其字段与属性</param>
#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void LogError(object message)
        {
            UnityLogger.Log(LogType.Error, FormatMessage(message));
        }


        /// <summary>
        /// 输出带标签的错误日志
        /// </summary>
        /// <param name="tag">标签文本，渲染为橙色 <c>[Tag]</c> 前缀</param>
        /// <param name="message">日志内容，作为纯文本原样输出</param>
#if UNITY_2022_2_OR_NEWER
        [HideInCallstack]
#endif
        public static void LogError(string tag, string message)
        {
            UnityLogger.Log(LogType.Error, FormatTagAndMessage(tag, message));
        }

        /// <summary>
        /// 输出带标签的错误日志，并递归展开对象内容
        /// </summary>
        /// <param name="tag">标签文本，渲染为橙色 <c>[Tag]</c> 前缀</param>
        /// <param name="message">任意对象，会展开其字段与属性</param>
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
        /// 输出异常日志，自动附加异常类型名和堆栈信息
        /// <para>输出格式为 <c>[异常类型名]异常消息</c> 换行后接完整堆栈，类型名在 Editor 中显示为红色</para>
        /// <code>
        /// try
        /// {
        ///     LoadSaveFile(path);
        /// }
        /// catch (Exception exception)
        /// {
        ///     DLogger.LogException("Save", exception);
        /// }
        /// </code>
        /// </summary>
        /// <param name="exception">要记录的异常，传 null 时直接返回，不产生任何输出</param>
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
        /// 输出带标签的异常日志，自动附加异常类型名和堆栈信息
        /// <para>与 <see cref="LogException(System.Exception)"/> 的唯一区别是多一个橙色 <c>[Tag]</c> 前缀</para>
        /// </summary>
        /// <param name="tag">标签文本，建议使用抛出异常所在的模块名</param>
        /// <param name="exception">要记录的异常，传 null 时直接返回，不产生任何输出</param>
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

        /// <summary>
        /// 按当前配置格式化一段文本，不输出日志
        /// <para>用于先把日志内容拼装好再统一输出，或在别处复用同一套时间戳/帧号前缀</para>
        /// </summary>
        /// <param name="message">日志内容，作为纯文本原样追加</param>
        /// <returns>带时间戳与帧号前缀的完整文本（前缀是否出现取决于公开配置开关）</returns>
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
        /// <summary>
        /// 按当前配置格式化一个对象，不输出日志
        /// <para>会递归展开对象的字段与属性，并做循环引用检测与深度截断</para>
        /// </summary>
        /// <param name="obj">任意对象，传 null 时输出字面量 <c>null</c></param>
        /// <returns>带时间戳与帧号前缀的完整文本</returns>
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
        /// 按当前配置格式化标签与文本消息，不输出日志
        /// </summary>
        /// <param name="tag">标签文本，渲染为橙色 <c>[Tag]</c> 前缀</param>
        /// <param name="message">日志内容，作为纯文本原样追加</param>
        /// <returns>形如 <c>[时间戳] F:帧号 [标签] 消息</c> 的完整文本</returns>
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
        /// 按当前配置格式化标签与对象，不输出日志
        /// <para>会递归展开对象的字段与属性，并做循环引用检测与深度截断</para>
        /// </summary>
        /// <param name="tag">标签文本，渲染为橙色 <c>[Tag]</c> 前缀</param>
        /// <param name="obj">任意对象，传 null 时输出字面量 <c>null</c></param>
        /// <returns>形如 <c>[时间戳] F:帧号 [标签] {展开后的对象}</c> 的完整文本</returns>
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


        /// <summary>
        /// 按当前配置格式化标签与异常，不输出日志
        /// <para>输出为 <c>[标签]异常类型名</c> 换行后接异常消息与堆栈，而非对象展开形式</para>
        /// </summary>
        /// <param name="tag">标签文本，渲染为橙色 <c>[Tag]</c> 前缀</param>
        /// <param name="exception">要格式化的异常，本重载不做 null 检查，传 null 会抛 <c>NullReferenceException</c></param>
        /// <returns>形如 <c>[时间戳] F:帧号 [标签]异常类型名\n消息\n堆栈</c> 的完整文本</returns>
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