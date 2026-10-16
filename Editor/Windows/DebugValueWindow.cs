using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace ToolKit.Editor
{
    /// <summary>
    /// 运行时数值监视窗口，把代码里的关键变量实时显示在编辑器的独立面板上
    /// <para>用法：代码中调用静态方法推送数值，窗口打开时自动显示、自动去重</para>
    /// <code>
    /// // 每帧或按需推送，同名条目会被覆盖而不是重复添加
    /// void Update()
    /// {
    ///     DebugValueWindow.ShowValue("Player Position", transform.position);
    ///     DebugValueWindow.ShowValue("FPS", fps.Fps);
    /// }
    ///
    /// // 不再需要时移除
    /// DebugValueWindow.RemoveValue("Player Position");
    /// </code>
    /// <para>菜单入口：<c>Tool Kit/数值调试窗口</c>。窗口基于 UI Toolkit 实现，每个条目是一个 <c>Label</c>，
    /// 显示格式固定为 <c>名称 -&gt; 值</c></para>
    /// </summary>
    /// <remarks>
    /// <para>关于窗口必须先打开（重要）：<see cref="ShowValue"/> <strong>不会自动打开窗口</strong>，
    /// 窗口未打开时调用是静默的空操作，数值不会被缓存，窗口打开后也看不到之前推送的内容。</para>
    /// <para>也就是说，需要监视哪个场景，就得先手动打开一次窗口再进入播放模式。</para>
    /// <para>关于条目的生命周期：</para>
    /// <list type="bullet">
    /// <item>按名称去重，同名条目复用同一个 <c>Label</c>，只更新文本，因此每帧调用也不会堆积节点</item>
    /// <item>条目不会被自动清理，不再推送就会一直停留在最后一次的值上</item>
    /// <item>进入播放模式时会整体清空（<c>container.Clear()</c> 加字典清空），避免上一轮的残留值造成误读</item>
    /// <item>但<strong>退出播放模式时不会清空</strong>，所以停止运行后面板上仍然是最后一次运行的值</item>
    /// </list>
    /// <para>关于值的显示：直接走 <c>object.ToString()</c>，<strong>不做对象展开</strong>，
    /// 因此传自定义类需要自己重写 <c>ToString</c>，否则只会看到类型名。</para>
    /// <para>关于窗口关闭后的调用安全性：静态 <c>instance</c> 在窗口关闭后仍指向已销毁的窗口对象，
    /// 但 Unity 的 <c>Object</c> 相等运算符会把已销毁对象判为 null，
    /// 因此 <c>instance == null</c> 的守卫能拦住后续访问，不会抛异常。</para>
    /// <para>关于清理时机：<c>OnDisable</c> 里没有把 <c>instance</c> 置空，依赖上面提到的「假 null」机制兜底。</para>
    /// </remarks>
    public class DebugValueWindow : EditorWindow
    {
        /// <summary>
        /// 窗口实例的静态引用，由 <see cref="CreateGUI"/> 赋值
        /// <para>静态方法需要通过它转发到具体窗口</para>
        /// </summary>
        private static DebugValueWindow instance;

        /// <summary>
        /// 所有调试条目挂载的容器节点，负责统一的内边距与纵向排列
        /// </summary>
        private VisualElement container;

        /// <summary>名称 → Label 的映射，用于按名去重与快速定位</summary>
        private Dictionary<string, Label> debugLabels = new();

        /// <summary>
        /// 创建 UI Toolkit 界面树，构建容器并登记静态实例
        /// <para>由 Unity 在窗口首次显示时调用，窗口重新打开时会再次执行</para>
        /// </summary>
        /// <remarks>
        /// <para>这里把 <c>instance</c> 重新赋值，是「窗口关闭再打开后静态引用仍然有效」的关键。</para>
        /// <para>注意本方法重建了 <c>container</c> 但没有清空 <c>debugLabels</c>，
        /// 若同一个窗口实例被重复创建界面，字典里的旧 <c>Label</c> 会指向已被移除的旧节点。</para>
        /// </remarks>
        public void CreateGUI()
        {
            container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;
            container.style.paddingLeft = new StyleLength(20);
            container.style.paddingTop = new StyleLength(20);
            container.style.paddingRight = new StyleLength(20);

            rootVisualElement.Add(container);


            instance = this; // 保证 CreateGUI 时刷新实例

        }

        /// <summary>
        /// 订阅播放模式变化事件
        /// </summary>
        private void OnEnable()
        {
            // 订阅 Play Mode 状态改变事件
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        /// <summary>
        /// 退订播放模式变化事件
        /// </summary>
        /// <remarks>
        /// <para><c>EditorApplication</c> 的静态事件持有窗口引用，不退订会导致窗口无法被回收。</para>
        /// </remarks>
        private void OnDisable()
        {
            // 取消订阅，避免内存泄漏
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        /// <summary>
        /// 进入播放模式时清空所有条目，避免看到上一轮的残留值
        /// </summary>
        /// <param name="state">播放模式状态</param>
        /// <remarks>
        /// <para>只处理 <c>EnteredPlayMode</c>，退出播放模式的清理<strong>没有做</strong>，
        /// 因此停止运行后面板上保留的是最后一次的值。</para>
        /// <para><c>container</c> 若尚未创建（事件早于 <c>CreateGUI</c> 触发）会抛 <c>NullReferenceException</c>，
        /// 实际使用中窗口打开后才会进入播放模式，一般不会触发。</para>
        /// </remarks>
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                container.Clear();
                debugLabels.Clear();
            }
        }

        /// <summary>
        /// 显示或更新一个调试数值
        /// <code>
        /// DebugValueWindow.ShowValue("Player Position", transform.position);
        /// </code>
        /// </summary>
        /// <param name="name">条目标识与显示名称，同名条目会被更新而不是新增</param>
        /// <param name="value">要显示的值，直接调用其 <c>ToString()</c></param>
        /// <remarks>
        /// <para>窗口未打开时静默返回，不做任何缓存，因此该方法可以安全地在每帧调用。</para>
        /// </remarks>
        public static void ShowValue(string name, object value)
        {
            if (instance == null) return;
            instance.InternalShowValue(name, value);
        }

        /// <summary>
        /// 按名称查找或新建条目，并刷新其显示文本
        /// </summary>
        /// <param name="name">条目标识与显示名称</param>
        /// <param name="value">要显示的值</param>
        private void InternalShowValue(string name, object value)
        {
            if (!debugLabels.TryGetValue(name, out var label))
            {
                label = new Label();
                container.Add(label);

                debugLabels[name] = label;
            }
            label.text = $"{name} -> {value}";
        }

        /// <summary>
        /// 移除一个调试条目
        /// <code>
        /// // 对象销毁时记得移除，否则面板上会一直留着最后的值
        /// void OnDestroy()
        /// {
        ///     DebugValueWindow.RemoveValue("Player Position");
        /// }
        /// </code>
        /// </summary>
        /// <param name="name">要移除的条目标识，不存在时是安全的空操作</param>
        public static void RemoveValue(string name)
        {
            if (instance == null) return;
            if (instance.debugLabels.TryGetValue(name, out var label))
            {
                instance.container.Remove(label);
                instance.debugLabels.Remove(name);
            }
        }
    }
}
