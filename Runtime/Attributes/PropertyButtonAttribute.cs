using System;
using UnityEngine;

namespace ToolKit
{
    /// <summary>
    /// 在字段上显示一个可点击按钮，调用指定的方法
    /// <para>用法：在字段上添加此特性，指定要调用的方法名</para>
    /// <para>方法带不带参数由反射自动识别：带参数时会在按钮上方生成可编辑的参数输入字段</para>
    /// <code>
    /// // 无参数方法：只绘制按钮
    /// [PropertyButton("ResetValue", "重置", ButtonColor.Red)]
    /// public int myValue;
    ///
    /// private void ResetValue()
    /// {
    ///     myValue = 0;
    /// }
    ///
    /// // 带参数方法：参数可在 Inspector 中编辑
    /// [PropertyButton("AddValue", "添加", ButtonColor.Green, defaultParams: new object[] { 10 })]
    /// public int score;
    ///
    /// private void AddValue(int amount)
    /// {
    ///     score += amount;
    /// }
    /// </code>
    /// <para>与 ButtonAttribute 不同，PropertyButton 基于 PropertyDrawer，</para>
    /// <para>即使脚本有 CustomEditor 也能正常显示</para>
    /// </summary>
    /// <remarks>
    /// <para>关于 <c>defaultParams</c>：</para>
    /// <list type="bullet">
    /// <item>只能是编译期常量（<c>null</c>、数值、<c>string</c>、<c>bool</c>、枚举值、<c>typeof(...)</c>），
    /// 所以 <c>Color.red</c> / <c>Vector3.zero</c> 这类属性或静态字段写不进来</item>
    /// <item>它是构造函数的最后一个参数，建议用命名参数传：<c>defaultParams: new object[] { 10 }</c></item>
    /// <item>类型不匹配时会尝试自动转换（例如 int 字面量到 float 参数），失败则回退到类型默认值并打印警告</item>
    /// </list>
    /// <para>另外，Unity 不会堆叠同一字段上的多个 PropertyDrawer 属性（只会有一个生效），</para>
    /// <para>所以这里声明 <c>AllowMultiple = false</c>，一个字段只能挂一个 PropertyButton，</para>
    /// <para>需要多个按钮请拆成多个字段（编译期就会拦住重复标注）。</para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class PropertyButtonAttribute : PropertyAttribute
    {
        /// <summary>
        /// 要调用的方法名（可以是 public/private/protected）
        /// </summary>
        public string MethodName { get; }

        /// <summary>
        /// 按钮显示文本（默认使用方法名）
        /// </summary>
        public string ButtonText { get; }

        /// <summary>
        /// 按钮颜色
        /// </summary>
        public ButtonColor Color { get; }

        /// <summary>
        /// 按钮位置：在属性之前还是之后
        /// </summary>
        public ButtonPosition Position { get; }

        /// <summary>
        /// 按钮高度（默认 20）
        /// </summary>
        public float ButtonHeight { get; }

        /// <summary>
        /// 按钮宽度
        /// <para>Above / Below：-1 = 占满整行（默认），0 = 自适应文本宽度，>0 = 固定像素宽度</para>
        /// <para>Left / Right：-1 = 默认宽度（80px），0 = 自适应文本宽度，>0 = 固定像素宽度</para>
        /// </summary>
        public float ButtonWidth { get; }

        /// <summary>
        /// 是否在按钮下方绘制属性（默认 true）
        /// <para>如果设为 false，只显示按钮，不显示属性字段</para>
        /// </summary>
        public bool DrawProperty { get; }

        /// <summary>
        /// 方法参数的初始值
        /// <para>长度不足的部分使用参数类型的默认值；类型不匹配时会尝试自动转换</para>
        /// </summary>
        public object[] DefaultParams { get; }

        /// <param name="methodName">要调用的方法名（必需）</param>
        /// <param name="buttonText">按钮显示文本（默认使用方法名）</param>
        /// <param name="color">按钮颜色</param>
        /// <param name="position">按钮相对属性的位置</param>
        /// <param name="buttonHeight">按钮高度</param>
        /// <param name="buttonWidth">按钮宽度（见 <see cref="ButtonWidth"/>）</param>
        /// <param name="drawProperty">是否绘制原属性字段</param>
        /// <param name="defaultParams">方法参数的初始值（仅带参数的方法需要），建议用命名参数传</param>
        public PropertyButtonAttribute(
            string methodName,
            string buttonText = null,
            ButtonColor color = ButtonColor.White,
            ButtonPosition position = ButtonPosition.Above,
            float buttonHeight = 20f,
            float buttonWidth = -1f,
            bool drawProperty = true,
            object[] defaultParams = null)
        {
            MethodName = methodName;
            ButtonText = buttonText ?? methodName;
            Color = color;
            Position = position;
            ButtonHeight = buttonHeight;
            ButtonWidth = buttonWidth;
            DrawProperty = drawProperty;
            DefaultParams = defaultParams ?? Array.Empty<object>();
        }
    }

    /// <summary>
    /// 按钮相对于属性的位置
    /// </summary>
    public enum ButtonPosition
    {
        /// <summary>在属性上方</summary>
        Above,
        /// <summary>在属性下方</summary>
        Below,
        /// <summary>在属性左侧（与 label 平齐）</summary>
        Left,
        /// <summary>在属性右侧</summary>
        Right
    }
}
