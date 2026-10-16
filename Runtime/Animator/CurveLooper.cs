using UnityEngine;

namespace ToolKit
{
    /// <summary>
    /// 曲线的时间推进模式
    /// <para>决定把「全局时间」映射到「曲线时间」时如何处理超出 0~duration 的部分</para>
    /// </summary>
    public enum CurveLoopMode
    {
        /// <summary>
        /// 循环播放。
        /// 时间超过 duration 后回绕到 0 重新开始，曲线会不断重复。
        /// </summary>
        Loop,

        /// <summary>
        /// 往返播放。
        /// 时间超过 duration 后反向播放，到 0 再次反向，形成来回摆动效果。
        /// </summary>
        PingPong,

        /// <summary>
        /// 单次播放并停在末尾。
        /// 时间超过 duration 后输出保持在曲线终点值，低于 0 时保持在起点值。
        /// </summary>
        Clamp
    }

    /// <summary>
    /// 让 <c>AnimationCurve</c> 可以按 Loop / PingPong / Clamp 三种方式随时间播放
    /// <para>用法：构造时传入曲线与单次播放时长，之后每帧把 <c>Time.time</c> 传给 <see cref="Evaluate"/></para>
    /// <code>
    /// // 2 秒一个来回的呼吸动画
    /// private readonly CurveLooper breath = new CurveLooper(breathCurve, 2f, CurveLoopMode.PingPong);
    ///
    /// private void Update()
    /// {
    ///     float scale = breath.Evaluate(Time.time);
    ///     transform.localScale = Vector3.one * scale;
    ///
    ///     // 想知道当前播放进度时读 CurrentTime（0 ~ duration）
    ///     float progress = breath.CurrentTime / 2f;
    /// }
    /// </code>
    /// <para>曲线本身不关心自己的横轴范围，所以通常把关键帧做在 <c>0 ~ duration</c> 之间；</para>
    /// <para>即使关键帧范围与之不符，<c>AnimationCurve</c> 也会按自身定义做外推</para>
    /// </summary>
    /// <remarks>
    /// <para>关于三种模式的时间映射：</para>
    /// <list type="bullet">
    /// <item><see cref="CurveLoopMode.Loop"/> 用 <c>Mathf.Repeat</c>，得到 <c>[0, duration)</c> 的循环值</item>
    /// <item><see cref="CurveLoopMode.PingPong"/> 用 <c>Mathf.PingPong</c>，得到 <c>[0, duration]</c> 的三角波</item>
    /// <item><see cref="CurveLoopMode.Clamp"/> 用 <c>Mathf.Clamp</c>，得到 <c>[0, duration]</c> 且两端保持</item>
    /// <item>Loop 与 PingPong 的区别在于衔接处：Loop 是「到末尾后瞬间跳回起点」，PingPong 是「到末尾后平滑折返」</item>
    /// </list>
    /// <para>关于参数校验：构造函数<strong>不做任何校验</strong>。曲线传 null 会在求值时抛 <c>NullReferenceException</c>；</para>
    /// <para>duration 传 0 会让 Loop 模式产生除零，得到 <c>NaN</c> 并一路污染到最终结果，请确保 duration 大于 0。</para>
    /// <para>本类不含 Unity 生命周期依赖，也可以由代码手动驱动时间参数，方便做暂停、倒放或按帧调试。</para>
    /// </remarks>
    public class CurveLooper
    {
        private readonly AnimationCurve curve;
        private readonly float duration;
        private readonly CurveLoopMode mode;

        /// <summary>
        /// 最近一次求值时映射出的曲线时间，取值范围 <c>[0, duration]</c>
        /// <para>可以读出来算播放进度；<strong>给它赋值没有意义</strong>，下次 <see cref="Evaluate"/> 会直接覆写</para>
        /// <para>未调用过 <see cref="Evaluate"/> 时为 0</para>
        /// </summary>
        public float CurrentTime { get; set; }

        /// <summary>
        /// 创建一个曲线播放器
        /// <code>
        /// // 默认循环播放
        /// var loop = new CurveLooper(curve, 3f);
        ///
        /// // 往返播放
        /// var pingPong = new CurveLooper(curve, 3f, CurveLoopMode.PingPong);
        /// </code>
        /// </summary>
        /// <param name="curve">要播放的动画曲线，不能为 null</param>
        /// <param name="duration">单次播放的时长（秒），必须大于 0</param>
        /// <param name="mode">时间推进模式，默认 <see cref="CurveLoopMode.Loop"/></param>
        public CurveLooper(AnimationCurve curve, float duration, CurveLoopMode mode = CurveLoopMode.Loop)
        {
            this.curve = curve;
            this.duration = duration;
            this.mode = mode;
        }

        /// <summary>
        /// 按当前模式把传入时间映射到曲线时间，并求出曲线值
        /// <code>
        /// // 每帧推进：传累计时间就是自动播放
        /// float value = looper.Evaluate(Time.time);
        ///
        /// // 也可以自己做暂停：冻结时间就不再前进
        /// float value2 = looper.Evaluate(paused ? pausedAt : Time.time - pausedAt + timeWhenPaused);
        /// </code>
        /// </summary>
        /// <param name="time">全局时间（秒），通常传 <c>Time.time</c> 或 <c>Time.unscaledTime</c></param>
        /// <returns>曲线在映射后时间点上的输出值，并同步更新 <see cref="CurrentTime"/></returns>
        /// <remarks>
        /// <para>关于时间源的选择：传 <c>Time.time</c> 会受 <c>Time.timeScale</c> 影响（暂停时停止），</para>
        /// <para>传 <c>Time.unscaledTime</c> 则不受暂停影响，UI 动画通常用后者。</para>
        /// <para>本方法每次都重新计算映射，没有帧间状态依赖，因此可以从任意时间点开始求值，</para>
        /// <para>同一帧内多次调用也不会累加出错。</para>
        /// </remarks>
        public float Evaluate(float time)
        {
            switch (mode)
            {
                case CurveLoopMode.Loop:
                    CurrentTime = Mathf.Repeat(time, duration);
                    break;

                case CurveLoopMode.PingPong:
                    CurrentTime = Mathf.PingPong(time, duration);
                    break;

                case CurveLoopMode.Clamp:
                default:
                    CurrentTime = Mathf.Clamp(time, 0f, duration);
                    break;
            }

            return curve.Evaluate(CurrentTime);
        }
    }
}
