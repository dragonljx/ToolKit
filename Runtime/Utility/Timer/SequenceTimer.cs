using System;

namespace ToolKit
{
    /// <summary>
    /// 顺序执行一组回调的计时器：每到一个间隔执行下一个，走到末尾后从头再来
    /// <para>用法：本类不对外公开，由 <c>TimerManager.Sequence</c> 创建</para>
    /// <para>适合做脚本化的演出序列，例如「亮灯 → 播放音效 → 播动画 → 收尾」</para>
    /// </summary>
    /// <remarks>
    /// <para>关于「循环次数」与「执行次数」的换算：</para>
    /// <list type="bullet">
    /// <item>基类只认识「总执行次数」，本类在构造时把它换算成 <c>loopCount * actions.Length</c></item>
    /// <item>因此 <c>loopCount</c> 指的是<strong>整组 Action 跑几个来回</strong>，而不是单个 Action 的重复次数</item>
    /// <item>换算使用 <c>checked</c> 上下文，<c>loopCount</c> 与数组长度都很大时可能抛 <c>OverflowException</c></item>
    /// </list>
    /// <para>关于数组的拷贝：构造时会 <c>Clone</c> 一份，因此构造之后再修改原数组不会影响计时器，
    /// 但数组元素本身若为可变对象，仍会共享同一个引用。</para>
    /// <para>关于 null 元素：数组里允许出现 null，执行到时会跳过，但<strong>仍然占用一次计数与一个间隔</strong>，</para>
    /// <para>相当于一段静默的等待，可以用来制造停顿。</para>
    /// </remarks>
    internal sealed class SequenceTimer : AbstractTimer
    {
        private readonly Action[] actions;

        /// <summary>当前执行到第几个 Action，走到末尾后回绕到 0</summary>
        private int index;

        /// <param name="interval">间隔时间（秒），必须大于 0</param>
        /// <param name="actions">要顺序执行的 Action 数组，不可为 null 或空</param>
        /// <param name="loopCount">循环次数：1 = 只跑一遍就停，&lt;=0 = 无限循环</param>
        /// <exception cref="ArgumentException">actions 为 null 或空时抛出</exception>
        /// <exception cref="ArgumentOutOfRangeException">interval &lt;= 0 时抛出</exception>
        public SequenceTimer(
            float interval,
            Action[] actions,
            int loopCount = 1)
            : base(interval, CalculateRepeatCount(actions, loopCount))
        {
            if (actions == null || actions.Length == 0)
                throw new ArgumentException(
                    "actions 不能为 null 或空数组", nameof(actions));

            // 拷贝一份，避免外部修改影响计时器
            this.actions = (Action[])actions.Clone();
        }

        /// <summary>计算 base 构造函数所需的 repeatCount</summary>
        /// <param name="actions">要顺序执行的 Action 数组</param>
        /// <param name="loopCount">整组 Action 的循环次数，&lt;=0 表示无限</param>
        /// <returns>换算出的总执行次数；无限循环时返回 -1</returns>
        /// <exception cref="ArgumentException"><paramref name="actions"/> 为 null 或空时抛出</exception>
        /// <remarks>
        /// <para>本方法在基类构造函数执行期间被调用，此时子类字段尚未赋值，</para>
        /// <para>所以它必须是 <c>static</c> 且只能依赖传入的参数。</para>
        /// </remarks>
        private static int CalculateRepeatCount(Action[] actions, int loopCount)
        {
            if (actions == null || actions.Length == 0)
                throw new ArgumentException(
                    "actions 不能为 null 或空数组", nameof(actions));

            if (loopCount <= 0)
                return -1; // 无限循环

            checked
            {
                return loopCount * actions.Length;
            }
        }

        /// <summary>
        /// 执行当前下标的 Action，并把下标推进到下一个
        /// <para>异常由基类捕获记录；下标会照常推进，不会因为某一步失败而卡死序列</para>
        /// </summary>
        protected override void Execute()
        {
            actions[index]?.Invoke();

            index++;

            if (index >= actions.Length)
            {
                index = 0;
            }
        }

        /// <summary>
        /// 重置计时并回到数组的第一个 Action
        /// <para>重写基类实现是为了同时复位 <c>index</c>，否则重新启动会从中间继续</para>
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            index = 0;
        }
    }
}
