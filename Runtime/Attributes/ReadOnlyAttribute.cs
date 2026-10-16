using System;
using System.Diagnostics;
using UnityEngine;

namespace ToolKit
{
    /// <summary>
    /// 将序列化字段在 Inspector 中显示为只读（灰显、不可编辑）
    /// <para>用法：在字段上添加此特性，字段值仍会正常序列化与显示，只是无法在 Inspector 中修改</para>
    /// <para>实现方式是绘制时关闭 GUI，因此不会阻止代码在运行时或通过其他 Inspector 逻辑写入</para>
    /// <code>
    /// public class PlayerInfo : MonoBehaviour
    /// {
    ///     // 运行时生成、仅供查看的 ID
    ///     [ReadOnly]
    ///     [SerializeField]
    ///     private int runtimeValue;
    ///
    ///     // 只读与分组、提示等特性可以叠加
    ///     [ReadOnly]
    ///     [SerializeField]
    ///     [Tooltip("由构建流程写入")]
    ///     private string buildVersion;
    /// }
    /// </code>
    /// <para>适用于任意字段类型：绘制时直接转发给 <c>EditorGUI.PropertyField</c>，</para>
    /// <para>所以嵌套对象、集合和自定义 PropertyDrawer 都能保持原有的显示效果</para>
    /// </summary>
    /// <remarks>
    /// <para>关于 <c>[Conditional("UNITY_EDITOR")]</c>：</para>
    /// <list type="bullet">
    /// <item>该特性在打包时会被编译器整体剔除，因此不会给运行时程序集带来任何开销</item>
    /// <item>同时也意味着不要依赖它做运行时校验，它纯粹是编辑器显示层面的约束</item>
    /// <item>字段本身照常序列化，构建后如需修改，仍可通过代码或 PlayerPrefs 等外部存储覆盖</item>
    /// </list>
    /// <para>同一个字段上只能挂一个本特性（<c>AllowMultiple = false</c>）。</para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    [Conditional("UNITY_EDITOR")]
    public class ReadOnlyAttribute : PropertyAttribute
    { }
}
