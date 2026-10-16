using System;
using System.Reflection;
using ToolKit;

namespace ToolKit.Editor
{
    /// <summary>
    /// 封装一个带 <c>[DebugData]</c> 特性的字段或属性，把字段与属性两种成员的读写差异统一掉
    /// <para>用法：由 <c>DebugReflectionCache</c> 按类型反射产生并缓存，<c>DebugWindow</c> 通过它读写值</para>
    /// <code>
    /// // DebugWindow 中的典型用法
    /// object current = entry.Member.GetValue(entry.Component);
    /// object edited  = DrawValueField(current, entry.Member.ValueType);
    ///
    /// if (!Equals(edited, current))
    /// {
    ///     entry.Member.SetValue(entry.Component, edited);
    /// }
    /// </code>
    /// <para>构造时会判断成员是字段还是属性，并记下对应的 <c>FieldInfo</c> / <c>PropertyInfo</c>，
    /// 后续读写不需要再做类型判断</para>
    /// </summary>
    /// <remarks>
    /// <para>关于属性的限制：只有<strong>同时可读且可写</strong>的属性才会被缓存收录
    /// （过滤发生在 <c>DebugReflectionCache</c> 里）。</para>
    /// <para>只读属性会在扫描阶段就跳过，不会走到本类；但 <see cref="SetValue"/> 内部仍做了一次
    /// <c>CanWrite</c> 判断作为兜底，条件不满足时静默不做任何事。</para>
    /// <para>关于静态成员：反射用的绑定标志不含 <c>Static</c>，因此本类只会处理实例成员。</para>
    /// <para>关于性能：读写的反射开销都在实际调用时产生，本类不做缓存；
    /// <c>DebugWindow</c> 每帧重绘都会读取全部成员的值，成员数量多时会有一定开销。</para>
    /// </remarks>
    public class DebugMemberInfo
    {
        /// <summary>
        /// 底层成员信息，字段或属性皆可
        /// </summary>
        public MemberInfo Member { get; }

        /// <summary>
        /// 成员上的 <c>[DebugData]</c> 特性实例，用于取显示名称
        /// </summary>
        public DebugDataAttribute Attribute { get; }

        /// <summary>
        /// 成员的真实类型
        /// <para>字段取 <c>FieldType</c>，属性取 <c>PropertyType</c>；绘制控件时按该类型分支</para>
        /// </summary>
        public Type ValueType { get; }

        /// <summary>成员为字段时的反射信息，属性成员时为 null</summary>
        private readonly FieldInfo _fieldInfo;

        /// <summary>成员为属性时的反射信息，字段成员时为 null</summary>
        private readonly PropertyInfo _propertyInfo;

        /// <summary>
        /// 条目的显示名称
        /// <para><c>[DebugData]</c> 指定了名称时用它，否则回退到成员自身的名字</para>
        /// </summary>
        public string Name =>
            !string.IsNullOrEmpty(Attribute?.Name) ? Attribute.Name : Member.Name;

        /// <summary>
        /// 根据成员的实际种类创建条目，并记录对应的反射信息
        /// </summary>
        /// <param name="member">字段或属性的反射信息</param>
        /// <param name="attribute">成员上的 <c>[DebugData]</c> 特性</param>
        /// <remarks>
        /// <para>传入既不是字段也不是属性的 <c>MemberInfo</c> 时（例如方法），
        /// 两个私有字段都保持 null，<see cref="ValueType"/> 也会是 null，
        /// 后续读写会静默返回 null 或什么都不做。</para>
        /// </remarks>
        public DebugMemberInfo(MemberInfo member, DebugDataAttribute attribute)
        {
            Member = member;
            Attribute = attribute;

            if (member is FieldInfo fieldInfo)
            {
                _fieldInfo = fieldInfo;
                ValueType = fieldInfo.FieldType;
            }
            else if (member is PropertyInfo propertyInfo)
            {
                _propertyInfo = propertyInfo;
                ValueType = propertyInfo.PropertyType;
            }
        }

        /// <summary>
        /// 读取成员在指定对象上的当前值
        /// </summary>
        /// <param name="target">要读取的组件实例</param>
        /// <returns>成员的当前值；既非字段也非属性时返回 null</returns>
        /// <remarks>
        /// <para>属性成员的 getter 若抛出异常，异常会原样传播给调用方，本方法不做捕获。</para>
        /// </remarks>
        public object GetValue(object target)
        {
            if (_fieldInfo != null)
                return _fieldInfo.GetValue(target);

            if (_propertyInfo != null)
                return _propertyInfo.GetValue(target);

            return null;
        }

        /// <summary>
        /// 把值写回成员
        /// </summary>
        /// <param name="target">要写入的组件实例</param>
        /// <param name="value">要写入的值，类型需与 <see cref="ValueType"/> 兼容</param>
        /// <remarks>
        /// <para>属性成员如果不可写（<c>CanWrite</c> 为 false），本方法<strong>静默跳过</strong>，不报错也不写入。</para>
        /// <para>值类型不匹配时由反射抛出 <c>ArgumentException</c>，本方法不做类型转换，
        /// 转换逻辑由 <c>ButtonRenderUtility.CoerceParameterValue</c> 在上层完成。</para>
        /// <para>注意调用方需要自行处理 Undo 与场景标记脏：<c>DebugWindow</c> 是在调用本方法前
        /// 执行 <c>Undo.RecordObject</c>、调用后执行 <c>EditorUtility.SetDirty</c>。</para>
        /// </remarks>
        public void SetValue(object target, object value)
        {
            if (_fieldInfo != null)
            {
                _fieldInfo.SetValue(target, value);
            }
            else if (_propertyInfo != null && _propertyInfo.CanWrite)
            {
                _propertyInfo.SetValue(target, value);
            }
        }
    }
}
