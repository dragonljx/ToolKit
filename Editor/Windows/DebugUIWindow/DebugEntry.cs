using UnityEngine;

namespace ToolKit.Editor
{
    /// <summary>
    /// 场景中某个带 <c>[DebugData]</c> 成员的具体实例，即「哪个对象上的哪个成员」
    /// <para>用法：由 <c>DebugSceneScanner</c> 扫描产生，供 <c>DebugWindow</c> 绘制与读写值</para>
    /// <para>把「成员定义」（<see cref="DebugMemberInfo"/>，按类型缓存）与「成员实例」（本类，按对象产生）分开，
    /// 是为了避免每个对象都重复一次反射开销</para>
    /// </summary>
    /// <remarks>
    /// <para>关于引用有效性：本类持有的是 <see cref="Component"/> 引用，
    /// 场景中的对象被销毁后该引用会变成「假 null」。</para>
    /// <para>绘制前必须检查 <c>Component != null</c> 以及 <c>Component.gameObject != null</c>，
    /// <c>DebugWindow</c> 在过滤与分组时都做了这层检查。</para>
    /// <para>本类是不可变的，创建后字段不再变化，但被引用的组件状态会随场景变化。</para>
    /// </remarks>
    public class DebugEntry
    {
        /// <summary>
        /// 成员所属的组件实例
        /// <para>读取或写入值时的目标对象</para>
        /// </summary>
        public Component Component { get; }

        /// <summary>
        /// 成员的反射信息（字段名、显示名、类型、读写方法）
        /// <para>同一类型的所有实例共享同一个 <c>DebugMemberInfo</c></para>
        /// </summary>
        public DebugMemberInfo Member { get; }

        /// <summary>
        /// 所属 GameObject 的名字，用于搜索过滤
        /// </summary>
        public string GameObjectName => Component.gameObject.name;

        /// <summary>
        /// 所属组件的类型名，用于搜索过滤与分组
        /// </summary>
        public string ComponentTypeName => Component.GetType().Name;

        /// <summary>
        /// 成员的显示名（<c>[DebugData]</c> 指定的名称，未指定时取字段名）
        /// </summary>
        public string MemberName => Member.Name;

        /// <summary>
        /// 创建一个成员条目
        /// </summary>
        /// <param name="component">成员所属的组件实例</param>
        /// <param name="member">成员的反射信息</param>
        public DebugEntry(Component component, DebugMemberInfo member)
        {
            Component = component;
            Member = member;
        }
    }

    /// <summary>
    /// 场景中某个带 <c>[DebugButton]</c> 方法的具体实例，即「哪个对象上的哪个方法」
    /// <para>用法：由 <c>DebugSceneScanner</c> 扫描产生，供 <c>DebugWindow</c> 绘制按钮并调用</para>
    /// <para>与 <see cref="DebugEntry"/> 是同一套「定义与实例分离」的思路</para>
    /// </summary>
    /// <remarks>
    /// <para>关于引用有效性：与 <see cref="DebugEntry"/> 相同，组件被销毁后引用会变成「假 null」，
    /// 绘制前需要判空。</para>
    /// <para>点击按钮时若目标对象已销毁，调用会失败，<c>DebugMethodInfo.Invoke</c> 会捕获异常并记录日志。</para>
    /// </remarks>
    public class DebugButtonEntry
    {
        /// <summary>
        /// 方法所属的组件实例，调用时的目标对象
        /// </summary>
        public Component Component { get; }

        /// <summary>
        /// 方法的反射信息（方法本体、特性参数、参数列表）
        /// <para>同一类型的所有实例共享同一个 <c>DebugMethodInfo</c></para>
        /// </summary>
        public DebugMethodInfo Method { get; }

        /// <summary>
        /// 所属 GameObject 的名字，用于搜索过滤
        /// </summary>
        public string GameObjectName => Component.gameObject.name;

        /// <summary>
        /// 所属组件的类型名，用于搜索过滤与分组
        /// </summary>
        public string ComponentTypeName => Component.GetType().Name;

        /// <summary>
        /// 按钮显示文本（<c>[DebugButton]</c> 指定的名称，未指定时取方法名）
        /// </summary>
        public string MethodName => Method.Name;

        /// <summary>
        /// 创建一个方法按钮条目
        /// </summary>
        /// <param name="component">方法所属的组件实例</param>
        /// <param name="method">方法的反射信息</param>
        public DebugButtonEntry(Component component, DebugMethodInfo method)
        {
            Component = component;
            Method = method;
        }
    }
}
