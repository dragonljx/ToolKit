using System;
using System.Reflection;
using ToolKit;
using UnityEngine;

namespace ToolKit.Editor
{
    /// <summary>
    /// 封装一个带 <c>[DebugButton]</c> 特性的方法，含参数信息与带异常保护的调用入口
    /// <para>用法：由 <c>DebugReflectionCache</c> 按类型反射产生并缓存，<c>DebugWindow</c> 通过它绘制按钮并调用</para>
    /// <code>
    /// // DebugWindow 中的典型用法
    /// if (entry.Method.HasParameters)
    /// {
    ///     object[] args = GetOrCreateArgs(entry);   // 参数值跨刷新保留
    ///     entry.Method.Invoke(entry.Component, args);
    /// }
    /// else
    /// {
    ///     entry.Method.Invoke(entry.Component, null);
    /// }
    /// </code>
    /// <para>参数列表在构造时一次性取出并缓存，绘制时不需要反复调用 <c>MethodInfo.GetParameters</c></para>
    /// </summary>
    /// <remarks>
    /// <para>关于 <see cref="Invoke"/> 的异常处理：方法内部抛出的异常会被捕获并记录到 Console，
    /// <strong>不会传播给调用方</strong>。</para>
    /// <para>这是有意为之：调试按钮调用的往往是业务代码，抛异常不应该让整个 Debug Window 的绘制中断。</para>
    /// <para>代价是调用方无法感知失败，需要确认结果时只能看 Console 或方法自身的输出。</para>
    /// <para>关于参数值：本类只负责「有哪些参数」，参数值的保存与复用由 <c>DebugWindow</c> 负责，
    /// 它按「组件实例 ID + 方法」缓存参数数组，因此刷新窗口不会丢失已填写的值。</para>
    /// <para>关于返回值：调试按钮不展示返回值，调用结果被丢弃。</para>
    /// </remarks>
    public class DebugMethodInfo
    {
        /// <summary>
        /// 底层方法的反射信息
        /// </summary>
        public MethodInfo Method { get; }

        /// <summary>
        /// 方法上的 <c>[DebugButton]</c> 特性实例，用于取显示名称、颜色、分组与排序
        /// </summary>
        public DebugButtonAttribute Attribute { get; }

        /// <summary>
        /// 方法的参数列表，构造时缓存
        /// <para>绘制参数输入区与创建默认参数数组时都用它</para>
        /// </summary>
        public ParameterInfo[] Parameters { get; }

        /// <summary>
        /// 按钮显示文本
        /// <para><c>[DebugButton]</c> 指定了名称时用它，否则回退到方法自身的名字</para>
        /// </summary>
        public string Name =>
            !string.IsNullOrEmpty(Attribute?.Name) ? Attribute.Name : Method.Name;

        /// <summary>
        /// 方法是否带参数
        /// <para>带参数的方法在布局中会独占一行，因为它需要额外的参数输入区</para>
        /// </summary>
        public bool HasParameters => Parameters.Length > 0;

        /// <summary>
        /// 创建一个方法条目并缓存其参数列表
        /// </summary>
        /// <param name="method">方法的反射信息</param>
        /// <param name="attribute">方法上的 <c>[DebugButton]</c> 特性</param>
        public DebugMethodInfo(MethodInfo method, DebugButtonAttribute attribute)
        {
            Method = method;
            Attribute = attribute;
            Parameters = method.GetParameters();
        }

        /// <summary>
        /// 调用目标对象上的方法，异常会被捕获并记录
        /// </summary>
        /// <param name="target">方法所属的组件实例，调用静态方法时可为 null</param>
        /// <param name="args">参数数组，元素个数与类型需与 <see cref="Parameters"/> 对应；无参方法传 null 即可</param>
        /// <remarks>
        /// <para>参数类型不匹配时反射会抛 <c>ArgumentException</c>，参数个数不符会抛
        /// <c>TargetParameterCountException</c>，两者都会被捕获并记录，不会中断窗口绘制。</para>
        /// <para>目标对象已被销毁时调用会失败，同样被捕获；对应地，<c>DebugWindow</c> 在绘制前
        /// 已经过滤掉失效的组件，正常情况下不会走到这里。</para>
        /// </remarks>
        public void Invoke(object target, object[] args)
        {
            try
            {
                Method.Invoke(target, args);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
