using System;
using UnityEngine;

namespace ToolKit
{
    /// <summary>
    /// 将枚举字段的可选值改为带搜索框的弹出列表，枚举项很多时可以按名称快速过滤
    /// <para>用法：在枚举字段上添加此特性，点击字段会弹出一个可搜索的选择窗口</para>
    /// <code>
    /// public class InputConfig : MonoBehaviour
    /// {
    ///     // KeyCode 有数百个枚举值，用普通下拉框逐个翻找非常低效
    ///     [SearchableEnum]
    ///     [SerializeField]
    ///     private KeyCode key;
    ///
    ///     // 枚举数组同样生效，数组的每个元素都会各自弹出搜索窗口
    ///     [SearchableEnum]
    ///     [SerializeField]
    ///     private KeyCode[] comboKeys;
    /// }
    /// </code>
    /// <para>只对 <c>SerializedPropertyType.Enum</c> 生效；其余类型会原样转发给</para>
    /// <para><c>EditorGUI.PropertyField</c>，因此挂在非枚举字段上不会破坏原有绘制</para>
    /// </summary>
    /// <remarks>
    /// <para>关于弹出窗口：</para>
    /// <list type="bullet">
    /// <item>窗口尺寸按枚举项数量自适应，高度上限 250px，超出部分滚动查看</item>
    /// <item>窗口依赖 UXML 资源绘制，若资源缺失会在 Console 报 <c>VisualTreeAsset 未设置</c></item>
    /// <item>枚举值数量很少（如三五个状态）时没有必要使用，普通下拉框反而更快</item>
    /// </list>
    /// <para>本特性是无参数标记，同一个字段上只能挂一个（Unity 不会堆叠多个 PropertyDrawer）。</para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SearchableEnumAttribute : PropertyAttribute
    {
    }
}
