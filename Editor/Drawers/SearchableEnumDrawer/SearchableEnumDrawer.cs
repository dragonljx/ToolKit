using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ToolKit.Editor
{
    /// <summary>
    /// <c>[SearchableEnum]</c> 的绘制器，把枚举字段画成一个可搜索的弹出按钮
    /// <para>用法：由 Unity 根据特性自动匹配，不需要手动引用</para>
    /// <code>
    /// // 加了特性后，点击字段会弹出带搜索框的选择窗口
    /// [SearchableEnum]
    /// [SerializeField]
    /// private KeyCode key;
    /// </code>
    /// <para>绘制时用一个 <c>EditorStyles.popup</c> 样式的按钮替代 StandardDropdown，
    /// 点击后把位置换算到屏幕坐标并弹出 <see cref="SearchableEnumWindow"/></para>
    /// </summary>
    /// <remarks>
    /// <para>关于生效范围：只对 <c>SerializedPropertyType.Enum</c> 生效。</para>
    /// <list type="bullet">
    /// <item>其余类型会原样转发给 <c>EditorGUI.PropertyField</c>，因此特性误挂在非枚举字段上
    /// 不会破坏原有绘制</item>
    /// <item>枚举数组同样可用：Unity 对数组元素逐个应用 <c>PropertyDrawer</c>，
    /// 每个元素都是 Enum 类型，会各自弹出搜索窗口</item>
    /// <item>枚举列表（<c>List&lt;枚举&gt;</c>）同理，每个元素独立生效</item>
    /// </list>
    /// <para>关于标签：绘制时按 <c>label.text</c> 是否为空分两条路径。</para>
    /// <para>有标签时先画标签，再在剩余宽度上画按钮；无标签时（例如集合元素）按钮占满整个矩形。</para>
    /// <para>关于高度：本类没有重写 <c>GetPropertyHeight</c>，走基类默认实现，
    /// 与普通单行字段高度一致；但转发分支里主动减去了一次
    /// <c>standardVerticalSpacing</c>，避免非枚举字段比正常行高一截。</para>
    /// <para>关于弹出窗口的位置：通过 <c>GUIUtility.GUIToScreenRect</c> 把按钮的 GUI 坐标
    /// 换算成屏幕坐标，因此窗口会紧贴按钮下方弹出，滚动视图里也能正确定位。</para>
    /// <para>关于窗口高度：按枚举项数量自适应，上限 250px，见 <see cref="DrawEnumButton"/>。</para>
    /// </remarks>
    [CustomPropertyDrawer(typeof(SearchableEnumAttribute))]
    public class SearchableEnumDrawer : PropertyDrawer
    {
        #region Public Methods

        /// <summary>
        /// 按属性类型分派绘制：枚举走自定义弹出按钮，其余类型原样转发
        /// </summary>
        /// <param name="position">绘制区域</param>
        /// <param name="property">要绘制的序列化属性</param>
        /// <param name="label">字段标签</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.Enum)
            {
                DrawEnum(position, property, label);
                return;
            }

            Rect propertyRect = new(
                position.x,
                position.y,
                position.width,
                position.height - EditorGUIUtility.standardVerticalSpacing);

            EditorGUI.PropertyField(
                propertyRect,
                property,
                label,
                false);
        }

        #endregion Public Methods

        #region Enum Drawing

        /// <summary>
        /// 绘制枚举字段：按有无标签决定按钮的起始位置与宽度
        /// </summary>
        /// <param name="position">绘制区域</param>
        /// <param name="property">枚举类型的序列化属性</param>
        /// <param name="label">字段标签，文本为空时按钮占满整行</param>
        private void DrawEnum(Rect position, SerializedProperty property, GUIContent label)
        {
            bool hasLabel = !string.IsNullOrEmpty(label.text);

            if (!hasLabel)
            {
                DrawEnumButton(position, property);
                return;
            }

            Rect labelRect = new(
                position.x,
                position.y,
                EditorGUIUtility.labelWidth,
                EditorGUIUtility.singleLineHeight);

            EditorGUI.LabelField(labelRect, label);

            Rect buttonRect = new(
                position.x + EditorGUIUtility.labelWidth,
                position.y,
                position.width - EditorGUIUtility.labelWidth,
                EditorGUIUtility.singleLineHeight);

            DrawEnumButton(buttonRect, property);
        }

        /// <summary>
        /// 绘制枚举按钮，点击时在按钮下方弹出可搜索的选择窗口
        /// </summary>
        /// <param name="buttonRect">按钮占用的矩形</param>
        /// <param name="property">枚举类型的序列化属性，会原样传给弹出窗口以便写回选择结果</param>
        /// <remarks>
        /// <para>按钮文本取 <c>property.enumNames[enumValueIndex]</c>；索引越界时（例如枚举值来自
        /// 序列化数据但枚举定义已改动）显示空文本，不会抛异常。</para>
        /// <para>窗口高度按 <c>枚举项数 × 单行高 + 50</c> 计算，并限制在 250px 以内，
        /// 超过部分由窗口内部滚动。</para>
        /// <para>这里的 <c>enumType</c> 变量取了 <c>fieldInfo.FieldType</c> 但没有被使用，
        /// 属于残留代码；类型信息实际由窗口从 <paramref name="property"/> 上再取一次。</para>
        /// </remarks>
        private void DrawEnumButton(Rect buttonRect, SerializedProperty property)
        {
            int index = property.enumValueIndex;
            int size = property.enumNames.Length;
            string enumName = index >= 0 && index < property.enumNames.Length
                ? property.enumNames[index]
                : string.Empty;

            if (GUI.Button(buttonRect, enumName, EditorStyles.popup))
            {
                Type enumType = fieldInfo?.FieldType;

                Rect screenRect = GUIUtility.GUIToScreenRect(buttonRect);
                float height = Mathf.Min(EditorGUIUtility.singleLineHeight * size+50, 250);

                SearchableEnumWindow.Show(property, screenRect, new(260, height));
            }
        }

        #endregion Enum Drawing
    }
}
