using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ToolKit
{
    /// <summary>
    /// 标记一个字段或属性，使其在 Debug Window 中显示为可实时编辑的条目
    /// <para>用法：在字段或属性上添加此特性，打开 Tool Kit/Debug Window 即可看到并修改其值</para>
    /// <para>值按成员的真实类型绘制对应控件，修改走 Undo 记录，可在编辑器中撤销</para>
    /// <code>
    /// public class PlayerDebug : MonoBehaviour
    /// {
    ///     // 使用自定义显示名称
    ///     [DebugData("当前生命值")]
    ///     private int health = 100;
    ///
    ///     // 留空则显示字段名 speed
    ///     [DebugData]
    ///     private float speed = 5f;
    /// }
    /// </code>
    /// <para>本特性只在 Debug Window 中生效，不会改变普通 Inspector 的绘制方式</para>
    /// </summary>
    /// <remarks>
    /// <para>关于适用范围：</para>
    /// <list type="bullet">
    /// <item>属性必须同时可读且可写（<c>CanRead &amp;&amp; CanWrite</c>）才会被收录，只读属性会被跳过</item>
    /// <item>只扫描实例成员，静态字段与静态属性不会被收录</item>
    /// <item>支持的类型：<c>int</c>、<c>float</c>、<c>double</c>、<c>long</c>、<c>bool</c>、<c>string</c>、
    /// <c>Vector2</c>、<c>Vector3</c>、<c>Vector4</c>、<c>Color</c>、枚举以及 <c>UnityEngine.Object</c> 派生类型</item>
    /// <item>不在上述列表中的类型会显示为 <c>Unsupported: 类型名</c>，值只读不可改</item>
    /// </list>
    /// <para>同一组件上的多个 <c>[DebugData]</c> 成员会按反射返回顺序依次显示，目前不支持指定排序。</para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class DebugDataAttribute : Attribute
    {
        /// <summary>
        /// 条目的显示名称（留空则使用成员自身的名字）
        /// </summary>
        public string Name { get; }

        /// <param name="name">条目的显示名称（留空使用成员名）</param>
        public DebugDataAttribute(string name = null)
        {
            Name = name;
        }
    }
}
