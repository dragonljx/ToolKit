using UnityEngine;
using ToolKit;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ToolKit.Examples
{
    /// <summary>
    /// <c>[PropertyButton]</c> 的完整用法示例，同时演示「脚本带有 CustomEditor 时按钮依然可用」
    /// <para>用法：把本组件挂到任意 GameObject 上，在 Inspector 里点击各按钮观察效果</para>
    /// <para>本类是可直接运行的教学样例，不是框架的一部分，实际业务中不需要继承或引用它</para>
    /// </summary>
    /// <remarks>
    /// <para>关于本示例想说明的三件事：</para>
    /// <list type="bullet">
    /// <item>按钮可以放在属性的上方、下方、左侧或右侧，由 <c>ButtonPosition</c> 控制</item>
    /// <item>被调用的方法可以是 <c>private</c>，通过反射调用，不需要对外开放</item>
    /// <item>把 <c>drawProperty</c> 设为 false 可以只留按钮、隐藏字段本身，适合当纯操作入口用</item>
    /// </list>
    /// <para>关于下面那段 <c>CustomEditor</c>：它是本示例的关键——正因为有它，
    /// 如果用普通的 <c>[Button]</c> 特性，按钮会完全不显示；而 <c>[PropertyButton]</c>
    /// 基于 <c>PropertyDrawer</c>，不会被 <c>CustomEditor</c> 覆盖，所以照常工作。</para>
    /// <para>注意 Unity 不允许在同一个字段上叠加多个 <c>PropertyDrawer</c> 特性，
    /// 因此一个字段只能挂一个 <c>[PropertyButton]</c>（示例中的 <c>counter</c> 字段演示了这一点，
    /// 第二个按钮被注释掉了）。需要多个按钮时请拆成多个字段。</para>
    /// </remarks>
    public class PropertyButtonExample : MonoBehaviour
    {
        [Header("基本用法")]
        [PropertyButton(nameof(ResetHealth), "重置生命值", ButtonColor.Red)]
        public int health = 100;

        [PropertyButton("AddGold", "增加金币", ButtonColor.Yellow)]
        public int gold = 0;

        [Header("不同位置")]
        [PropertyButton("LogMessage", "日志", ButtonColor.Blue, ButtonPosition.Above)]
        public string message = "Hello World";

        [PropertyButton("ClearName", "清空", ButtonColor.Red, ButtonPosition.Right, buttonWidth: 60)]
        public string playerName = "Player";

        [Header("只显示按钮")]
        [PropertyButton("DoSomething", "执行操作", ButtonColor.Green, drawProperty: false)]
        public bool dummyField; // 这个字段不会显示，只显示按钮

        [Header("多个按钮")]
        [PropertyButton("Increment", "+1", ButtonColor.Green, ButtonPosition.Above)]
        //[PropertyButton("Decrement", "-1", ButtonColor.Red, ButtonPosition.Below)]
        public int counter = 0;

        /// <summary>
        /// 把生命值重置为满值
        /// <para>演示：被调用的方法可以是私有的</para>
        /// </summary>
        private void ResetHealth()
        {
            health = 100;
            Debug.Log("生命值已重置");
        }

        /// <summary>
        /// 给金币加 10 并输出当前数量
        /// </summary>
        private void AddGold()
        {
            gold += 10;
            Debug.Log($"金币 +10，当前: {gold}");
        }

        /// <summary>
        /// 输出当前 message 字段的内容
        /// </summary>
        private void LogMessage()
        {
            Debug.Log($"消息: {message}");
        }

        /// <summary>
        /// 清空玩家名称
        /// </summary>
        private void ClearName()
        {
            playerName = "";
        }

        /// <summary>
        /// 演示 drawProperty 为 false 时的按钮回调
        /// </summary>
        private void DoSomething()
        {
            Debug.Log("执行了一些操作！");
        }

        /// <summary>
        /// 计数器加一
        /// </summary>
        private void Increment()
        {
            counter++;
        }

        /// <summary>
        /// 计数器减一
        /// <para>当前没有字段引用它，作为「一个字段只能挂一个 PropertyButton」的对照示例保留</para>
        /// </summary>
        private void Decrement()
        {
            counter--;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// <see cref="PropertyButtonExample"/> 的自定义编辑器，用来验证 <c>[PropertyButton]</c> 在有
    /// <c>CustomEditor</c> 时依然可用
    /// <para>自定义 Inspector 里照常调用 <c>DrawDefaultInspector()</c>，
    /// <c>[PropertyButton]</c> 生成的按钮就会随之绘制出来</para>
    /// </summary>
    /// <remarks>
    /// <para>如果把示例里的 <c>[PropertyButton]</c> 换成 <c>[Button]</c>，按钮会消失，
    /// 因为 <c>[Button]</c> 依赖一个自动生成的 <c>CustomEditor</c>，与本类冲突。</para>
    /// <para>这也是选择两种按钮特性的判断依据：脚本有自定义 Inspector 就用 <c>[PropertyButton]</c>，
    /// 没有就用功能更全的 <c>[Button]</c>。</para>
    /// </remarks>
    [CustomEditor(typeof(PropertyButtonExample))]
    public class PropertyButtonExampleEditor : Editor
    {
        /// <summary>
        /// 绘制自定义 Inspector：先显示一段说明，再走默认绘制
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "这个脚本使用了 CustomEditor，但 PropertyButton 仍然能正常显示！\n" +
                "这是因为 PropertyButton 基于 PropertyDrawer，不会被 CustomEditor 覆盖。",
                MessageType.Info);

            EditorGUILayout.Space();

            // 使用 DrawDefaultInspector 或手动绘制属性
            DrawDefaultInspector();

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
