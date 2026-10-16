using System;

namespace ToolKit
{
    /// <summary>
    /// 标记一个方法，使其在 Debug Window 中显示为可点击按钮
    /// <para>用法：在方法上添加此特性，打开 Tool Kit/Debug Window 即可看到按钮</para>
    /// <para>方法带不带参数由反射自动识别：带参数时会在按钮上展开参数输入区，参数值跨刷新保留</para>
    /// <code>
    /// public class PlayerDebug : MonoBehaviour
    /// {
    ///     private int health = 100;
    ///
    ///     // 无参数方法：直接绘制按钮
    ///     [DebugButton("回满血", ButtonColor.Green, group: "状态", order: 0)]
    ///     private void RestoreHealth()
    ///     {
    ///         health = 100;
    ///     }
    ///
    ///     // 带参数方法：参数可在 Debug Window 中编辑
    ///     [DebugButton("扣血", ButtonColor.Red, group: "状态", order: 1)]
    ///     private void TakeDamage(int amount)
    ///     {
    ///         health -= amount;
    ///     }
    /// }
    /// </code>
    /// <para>与 ButtonAttribute / PropertyButtonAttribute 不同，本特性不会在普通 Inspector 中生成按钮，</para>
    /// <para>只在 Debug Window 中生效。三者都需要，是因为 Inspector 与集中调试面板的展示空间不同</para>
    /// </summary>
    /// <remarks>
    /// <para>关于分组与排序：</para>
    /// <list type="bullet">
    /// <item>同 <c>Group</c> 的按钮会先显示分组标题，再在同一行内水平排列，宽度按可用空间均分</item>
    /// <item><c>Group</c> 留空的按钮排在所有分组之前，同样水平排列</item>
    /// <item><c>Order</c> 只决定同一组件内按钮的先后，数值越小越靠前</item>
    /// </list>
    /// <para>带参数的方法在水平排列中会独占一整行，因为它们需要额外的参数输入区。</para>
    /// <para>该方法所在的组件类型会被 <c>DebugReflectionCache</c>（Editor 程序集）缓存反射结果，</para>
    /// <para>可在 <c>public</c> / <c>private</c> / <c>protected</c> 方法上使用，但<strong>不支持静态方法</strong>，
    /// 因为扫描只遍历实例成员。</para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public class DebugButtonAttribute : Attribute
    {
        /// <summary>
        /// 按钮显示文本（留空则使用方法名）
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 按钮颜色（默认 <see cref="ButtonColor.White"/>）
        /// </summary>
        public ButtonColor Color { get; }

        /// <summary>
        /// 分组名称（同组按钮水平排列）
        /// <para>留空表示不分组，会排在所有分组之前</para>
        /// </summary>
        public string Group { get; }

        /// <summary>
        /// 显示顺序（数值越小越靠前，默认 0）
        /// <para>仅在同一组件内排序，不同组件之间按层级路径排序</para>
        /// </summary>
        public int Order { get; }

        /// <param name="name">按钮显示文本（留空使用方法名）</param>
        /// <param name="color">按钮颜色（默认 White）</param>
        /// <param name="group">分组名称（留空不分组）</param>
        /// <param name="order">显示顺序（数值越小越靠前，默认 0）</param>
        public DebugButtonAttribute(
            string name = null,
            ButtonColor color = ButtonColor.White,
            string group = "",
            int order = 0)
        {
            Name = name;
            Color = color;
            Group = group;
            Order = order;
        }
    }
}
