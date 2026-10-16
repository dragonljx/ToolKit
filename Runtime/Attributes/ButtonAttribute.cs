using System;
using UnityEngine;

namespace ToolKit
{
    /// <summary>
    /// 按钮颜色枚举
    /// <para>用于 ButtonAttribute / PropertyButtonAttribute / DebugButtonAttribute 的配色</para>
    /// </summary>
    public enum ButtonColor
    {
        /// <summary>绿色（通常用于确认/保存操作）</summary>
        Green,
        /// <summary>红色（通常用于删除/危险操作）</summary>
        Red,
        /// <summary>黄色（通常用于警告/重置操作）</summary>
        Yellow,
        /// <summary>蓝色（通常用于加载/信息操作）</summary>
        Blue,
        /// <summary>白色（默认）</summary>
        White
    }

    /// <summary>
    /// 按钮启用模式
    /// <para>控制在编辑模式和播放模式下按钮是否可点击</para>
    /// </summary>
    public enum ButtonMode
    {
        /// <summary>始终启用（编辑和播放模式都可点击）</summary>
        AlwaysEnabled,
        /// <summary>仅在播放模式启用</summary>
        EnabledInPlayMode,
        /// <summary>仅在编辑模式启用（播放时禁用）</summary>
        DisabledInPlayMode
    }

    /// <summary>
    /// 在 Inspector 中为方法显示一个可点击按钮
    /// <para>用法：在方法上添加此特性，会在 Inspector 底部生成按钮</para>
    /// <para>方法带不带参数由反射自动识别：带参数时会在按钮上方生成可编辑的参数输入字段</para>
    /// <code>
    /// // 示例 1：无参数方法
    /// [Button("保存数据", ButtonColor.Green, order: 1)]
    /// private void SaveData()
    /// {
    ///     Debug.Log("数据已保存");
    /// }
    ///
    /// // 示例 2：带参数方法（参数可在 Inspector 中编辑）
    /// [Button("加载数据", ButtonColor.Blue, order: 2)]
    /// private void LoadData(string path, int version)
    /// {
    ///     Debug.Log($"从 {path} 加载版本 {version}");
    /// }
    ///
    /// // 示例 3：分组按钮（同组按钮水平排列）
    /// [Button("重置位置", ButtonColor.Yellow, group: "变换操作")]
    /// private void ResetPosition() { transform.position = Vector3.zero; }
    ///
    /// [Button("重置旋转", ButtonColor.Yellow, group: "变换操作")]
    /// private void ResetRotation() { transform.rotation = Quaternion.identity; }
    ///
    /// // 示例 4：只在播放模式可点（一键导出运行时快照）
    /// [Button("导出快照", ButtonColor.White, mode: ButtonMode.EnabledInPlayMode)]
    /// private void ExportSnapshot()
    /// {
    ///     Debug.Log(JsonUtility.ToJson(transform));
    /// }
    /// </code>
    /// <para>注意：ButtonAttribute 基于 CustomEditor，与自定义 CustomEditor 可能冲突。</para>
    /// <para>如果需要在有 CustomEditor 的脚本中使用，请改用 PropertyButtonAttribute</para>
    /// </summary>
    /// <remarks>
    /// <para>支持的功能：</para>
    /// <list type="bullet">
    /// <item>支持无参数和带参数的方法（参数可在 Inspector 中编辑）</item>
    /// <item>支持实例方法和静态方法</item>
    /// <item>支持 public、private、protected 访问级别</item>
    /// <item>支持分组显示（同组按钮水平排列）</item>
    /// <item>支持自定义按钮颜色（5 种预设颜色）</item>
    /// <item>支持自定义显示顺序（Order）</item>
    /// <item>支持多种参数类型（int、float、string、bool、Vector、Color、Enum、UnityObject 等）</item>
    /// </list>
    /// <para>关于与 PropertyButtonAttribute 的选择：</para>
    /// <para>脚本没有自定义 CustomEditor 时用本特性，功能更全（分组、排序、启用模式、间距）；</para>
    /// <para>脚本已有 CustomEditor（或需要能在 DrawDefaultInspector 中生效）时改用 PropertyButtonAttribute，</para>
    /// <para>后者基于 PropertyDrawer，不会被 CustomEditor 覆盖，但只能挂在字段上。</para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class ButtonAttribute : Attribute
    {
        /// <summary>
        /// 按钮显示文本（留空则使用方法名）
        /// </summary>
        public string Name;

        /// <summary>
        /// 按钮颜色（默认 White）
        /// </summary>
        public ButtonColor Color;

        /// <summary>
        /// 按钮显示顺序（数值越小越靠前，默认 0）
        /// <para>可用于控制按钮在 Inspector 中的排列顺序</para>
        /// </summary>
        public int Order;

        /// <summary>
        /// 按钮分组名称（同一组的按钮会水平排列）
        /// <para>留空表示不分组，单独垂直显示</para>
        /// <para>分组可用于将相关功能的按钮组织在一起</para>
        /// </summary>
        public string Group;

        /// <summary>
        /// 按钮启用模式（控制在编辑模式/播放模式下是否可点击）
        /// </summary>
        public ButtonMode Mode;

        /// <summary>
        /// 按钮间距（像素，默认 5）
        /// </summary>
        public float Spacing;

        /// <summary>
        /// 按钮高度（像素，默认 25）
        /// <para>可用于区分重要操作（高度更大）和普通操作</para>
        /// </summary>
        public float ButtonHeight;

        /// <summary>
        /// 按钮宽度（像素）
        /// <para>-1 = 占满整行宽度（默认）</para>
        /// <para>0 = 自适应文本宽度</para>
        /// <para>&gt;0 = 固定像素宽度</para>
        /// </summary>
        public float ButtonWidth;

        /// <summary>
        /// 创建一个按钮特性
        /// </summary>
        /// <param name="name">按钮显示文本（留空使用方法名）</param>
        /// <param name="color">按钮颜色（默认 White）</param>
        /// <param name="group">分组名称（留空不分组）</param>
        /// <param name="order">显示顺序（数值越小越靠前，默认 0）</param>
        /// <param name="mode">启用模式（控制在编辑/播放模式下的可用性，默认始终启用）</param>
        /// <param name="spacing">按钮间距（像素，默认 5）</param>
        /// <param name="buttonHeight">按钮高度（像素，默认 25）</param>
        /// <param name="buttonWidth">按钮宽度（-1=全宽，0=自适应，&gt;0=固定宽度，默认 -1）</param>
        public ButtonAttribute(
            string name = null,
            ButtonColor color = ButtonColor.White,
            string group = "",
            int order = 0,
            ButtonMode mode = ButtonMode.AlwaysEnabled,
            float spacing = 5f,
            float buttonHeight = 25f,
            float buttonWidth = -1f)
        {
            Color = color;
            Name = name;
            Group = group;
            Order = order;
            Mode = mode;
            Spacing = spacing;
            ButtonHeight = buttonHeight;
            ButtonWidth = buttonWidth;
        }
    }
}
