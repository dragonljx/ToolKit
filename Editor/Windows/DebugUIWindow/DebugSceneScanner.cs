using System.Collections.Generic;
using UnityEngine;

namespace ToolKit.Editor
{
    /// <summary>
    /// 扫描当前已加载的所有场景，收集带调试特性的组件实例
    /// <para>用法：由 <c>DebugWindow.Refresh</c> 调用，把结果填进两个列表供窗口绘制</para>
    /// <code>
    /// var members = new List&lt;DebugEntry&gt;();
    /// var buttons = new List&lt;DebugButtonEntry&gt;();
    ///
    /// DebugSceneScanner.Scan(members, buttons);
    /// // 之后遍历这两个列表绘制字段与按钮
    /// </code>
    /// <para>扫描粒度是「实例」：反射结果按类型缓存，本类负责把它与场景中的每个组件实例配对</para>
    /// </summary>
    /// <remarks>
    /// <para>关于扫描范围：</para>
    /// <list type="bullet">
    /// <item>用 <c>Resources.FindObjectsOfTypeAll</c> 而非 <c>FindObjectsOfType</c>，
    /// 好处是<strong>未激活的对象也能被扫到</strong>（2021.3 没有 <c>FindObjectsByType</c> 的对应重载）</item>
    /// <item>代价是该 API 会把 Project 里的 Prefab、ScriptableObject 等资源也一并返回，
    /// 因此需要额外用 <c>gameObject.scene.IsValid()</c> 过滤，只保留场景中的对象</item>
    /// <item>已加载的多个场景（含 <c>DontDestroyOnLoad</c> 场景）都会被扫到，不只是当前激活场景</item>
    /// </list>
    /// <para>关于性能：<c>FindObjectsOfTypeAll</c> 会遍历全部已加载对象，是相对昂贵的调用，
    /// 本类又在每次刷新时都重新执行一遍。</para>
    /// <para><c>DebugWindow</c> 只在打开、层级变化或手动点击 Refresh 时调用它，
    /// 没有放在 <c>OnGUI</c> 里每帧执行，因此正常使用不会有明显的卡顿；
    /// 但如果场景对象极多，仅选中某个物体也会触发一次全量扫描。</para>
    /// <para>关于失效对象：扫描结果中可能包含在两次刷新之间被销毁的组件，
    /// 使用方（<c>DebugWindow</c>）在绘制前需要判空，本类不做二次校验。</para>
    /// </remarks>
    public static class DebugSceneScanner
    {
        /// <summary>
        /// 扫描所有已加载场景，把发现的调试成员与调试按钮填入传入的列表
        /// </summary>
        /// <param name="members">接收 <c>[DebugData]</c> 成员条目的列表，调用时会被<strong>先清空</strong></param>
        /// <param name="methods">接收 <c>[DebugButton]</c> 方法条目的列表，调用时会被<strong>先清空</strong></param>
        /// <remarks>
        /// <para>两个列表都是传入即清空再填充，调用方无须自行清理，但也不要指望列表里的旧内容会被保留。</para>
        /// <para>同一类型的多个实例会生成多条条目，每条都指向各自的组件实例，
        /// 但共享同一份反射信息（来自 <c>DebugReflectionCache</c>）。</para>
        /// <para>不含任何调试特性的类型会被整体跳过，不会产生空条目。</para>
        /// </remarks>
        public static void Scan(List<DebugEntry> members, List<DebugButtonEntry> methods)
        {
            members.Clear();
            methods.Clear();

            // 2021.3 无 FindObjectsByType，改用 FindObjectsOfTypeAll（包含非激活对象）
            MonoBehaviour[] behaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>();

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null || behaviour.gameObject == null)
                    continue;

                // 只保留场景内的对象，排除 Project 中的 prefab/asset
                if (!behaviour.gameObject.scene.IsValid())
                    continue;

                TypeDebugInfo typeInfo = DebugReflectionCache.Get(behaviour.GetType());

                if (typeInfo == null || !typeInfo.HasAnyDebugInfo)
                    continue;

                // 成员条目
                if (typeInfo.HasDebugData)
                {
                    foreach (DebugMemberInfo member in typeInfo.Members)
                    {
                        members.Add(new DebugEntry(behaviour, member));
                    }
                }

                // 方法条目
                foreach (DebugMethodInfo method in typeInfo.Methods)
                {
                    methods.Add(new DebugButtonEntry(behaviour, method));
                }
            }
        }
    }
}
