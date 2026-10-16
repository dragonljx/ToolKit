using System;

namespace ToolKit
{
    /// <summary>
    /// 最简单的计时器实现：每到一个间隔就调用一次回调
    /// <para>用法：本类不对外公开，由 <c>TimerManager</c> 的 <c>Delay</c> / <c>Loop</c> 创建</para>
    /// <para>「延迟执行一次」与「重复执行」共用本实现，区别只在传入的执行次数</para>
    /// </summary>
    /// <remarks>
    /// <para>关于回调为 null：<see cref="Execute"/> 会跳过空回调，但计时器仍然照常计时与计数，</para>
    /// <para>因此传入 null 不会报错，只是静默地什么都不做——这通常意味着调用方漏传了逻辑，值得留意。</para>
    /// <para>时间推进、次数控制与补帧逻辑全部在基类 <see cref="AbstractTimer"/> 中，本类只负责「执行什么」。</para>
    /// </remarks>
    internal sealed class UpdateTimer : AbstractTimer
    {
        private readonly Action callback;


        /// <summary>
        /// 创建一个按间隔触发回调的计时器
        /// </summary>
        /// <param name="interval">间隔时间（秒），必须大于 0</param>
        /// <param name="repeatCount">执行次数：&gt;0 执行指定次数（1 即延迟执行一次），&lt;=0 无限循环</param>
        /// <param name="callback">到点后执行的回调，允许为 null</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="interval"/> 小于等于 0 时抛出</exception>
        public UpdateTimer(
            float interval,
            int repeatCount,
            Action callback)
            : base(interval, repeatCount)
        {
            this.callback = callback;
        }


        /// <summary>
        /// 调用一次回调，异常由基类捕获记录
        /// </summary>
        protected override void Execute()
        {
            callback?.Invoke();
        }
    }

}
