using UnityEditor;
using UnityEngine;

namespace ToolKit.Editor
{
    /// <summary>
    /// <c>[ReadOnly]</c> 的绘制器，把字段画成灰显、不可编辑的样子
    /// <para>用法：由 Unity 根据特性自动匹配，不需要手动引用</para>
    /// <code>
    /// // 加了特性后，Inspector 里的字段会变灰且无法修改
    /// [ReadOnly]
    /// [SerializeField]
    /// private int runtimeValue;
    /// </code>
    /// <para>实现方式是把 <c>GUI.enabled</c> 关掉后再走一次正常的字段绘制，
    /// 因此字段的显示样式与可编辑时完全一致，只是不可操作</para>
    /// </summary>
    /// <remarks>
    /// <para>关于「只读」的实际含义：它只是<strong>显示层面</strong>的限制。</para>
    /// <list type="bullet">
    /// <item>字段照常序列化、照常进包，只是不能在 Inspector 里改</item>
    /// <item>代码里仍然可以随意写入，子类或运行时逻辑不受任何约束</item>
    /// <item><c>[ReadOnly]</c> 特性本身带 <c>[Conditional("UNITY_EDITOR")]</c>，构建时整个特性被剔除，
    /// 连带本绘制器也不会参与编译</item>
    /// </list>
    /// <para>关于高度：本类没有重写 <c>GetPropertyHeight</c>，使用基类默认实现转发给
    /// <c>EditorGUI.GetPropertyHeight</c>，因此嵌套对象、集合等复杂字段的折叠展开高度都是正确的。</para>
    /// <para>关于嵌套场景下的一个小问题：结束时会无条件把 <c>GUI.enabled</c> 置回 true，
    /// 而不是恢复进入前的值。若本字段被画在一个同样处于禁用状态的容器里
    /// （例如父级折叠区被禁用，或外层还有一个 <c>[ReadOnly]</c>），
    /// 内层绘制结束后会把外层的禁用状态一并打开。简单字段上不会暴露，层级嵌套时需要注意。</para>
    /// </remarks>
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        /// <summary>
        /// 关闭 GUI 交互后按正常方式绘制字段，从而实现「灰显且不可编辑」
        /// </summary>
        /// <param name="position">绘制区域</param>
        /// <param name="property">要绘制的序列化属性</param>
        /// <param name="label">字段标签</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {

            GUI.enabled = false;

            EditorGUI.PropertyField(position, property, label);

            GUI.enabled = true;
        }
    }
}
