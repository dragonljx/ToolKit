using System;
using System.Collections.Generic;

namespace ToolKit.Editor
{
    /// <summary>
    /// 某个组件类型的反射扫描结果，汇总该类型上所有带调试特性的成员与方法
    /// <para>用法：由 <c>DebugReflectionCache</c> 按类型产生并缓存，<c>DebugSceneScanner</c> 读取它来生成实例条目</para>
    /// <para>同一类型在场景中有多少个实例，本对象都只会存在一份，这正是把反射结果按类型缓存的目的</para>
    /// </summary>
    /// <remarks>
    /// <para>关于两个列表的填充时机：由 <c>DebugReflectionCache.BuildTypeDebugInfo</c> 在构造之后逐项填充，
    /// 因此刚构造出来的 <c>TypeDebugInfo</c> 两个列表都是空的。</para>
    /// <para>本类不含失效判断，类型一旦被缓存就始终有效（类型本身不会随场景变化）。</para>
    /// <para>关于 <see cref="HasDebugData"/> 与 <see cref="Members"/>：两者在实现上是同步的，
    /// <c>Members</c> 非空时 <c>HasDebugData</c> 必为 true，保留该标志只是为了调用处读起来更直观。</para>
    /// </remarks>
    public class TypeDebugInfo
    {
        /// <summary>
        /// 被扫描的组件类型，即该类型的缓存键
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// 该类型是否含有带 <c>[DebugData]</c> 的成员
        /// <para>由反射扫描过程写入，等价于 <see cref="Members"/> 非空</para>
        /// </summary>
        public bool HasDebugData { get; set; }

        /// <summary>
        /// 该类型上所有带 <c>[DebugData]</c> 的字段与属性
        /// <para>只包含实例成员，且属性需同时可读可写；顺序为反射返回顺序，不保证稳定</para>
        /// </summary>
        public List<DebugMemberInfo> Members { get; } = new List<DebugMemberInfo>();

        /// <summary>
        /// 该类型上所有带 <c>[DebugButton]</c> 的方法
        /// <para>顺序为反射返回顺序，实际显示顺序由各方法自己的 <c>Order</c> 决定</para>
        /// </summary>
        public List<DebugMethodInfo> Methods { get; } = new List<DebugMethodInfo>();

        /// <summary>
        /// 该类型是否含有任何调试信息，即有待展示的成员或按钮
        /// <para><c>DebugSceneScanner</c> 用它来跳过与调试无关的类型，避免为它们创建条目</para>
        /// </summary>
        public bool HasAnyDebugInfo => HasDebugData || Methods.Count > 0;

        /// <summary>
        /// 创建一个类型扫描结果（两个列表为空，需由反射流程填充）
        /// </summary>
        /// <param name="type">被扫描的组件类型</param>
        public TypeDebugInfo(Type type)
        {
            Type = type;
        }
    }
}
