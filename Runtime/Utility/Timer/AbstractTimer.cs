using System;

namespace ToolKit
{
    /// <summary>
    /// 计时器的抽象基类，负责时间推进、次数控制与暂停/恢复
    /// <para>用法：本类不对外公开，由 <c>TimerManager</c> 创建具体的 <c>UpdateTimer</c> / <c>SequenceTimer</c></para>
    /// <para>子类只需实现 <see cref="Execute"/>，把「到点了做什么」写进去即可</para>
    /// </summary>
    /// <remarks>
    /// <para>关于时间模型（容易误解的一点）：</para>
    /// <list type="bullet">
    /// <item>构造函数会把剩余时间初始化为完整的 <paramref name="interval"/>，因此<strong>第一次执行也要先等满一个间隔</strong>，
    /// 不存在「立刻执行一次再开始计时」的语义</item>
    /// <item>单帧内的 <c>deltaTime</c> 大于间隔时，会用 <c>while</c> 循环补齐欠下的次数，
    /// 掉帧后不会丢帧，而是集中补执行</item>
    /// <item>补执行的次数有上限（<c>MaxExecutionsPerFrame</c>），达到上限会丢弃本帧剩余次数并把剩余时间重置为一个完整间隔，
    /// 避免长时间卡顿后出现「执行风暴」</item>
    /// </list>
    /// <para>关于异常：<see cref="Execute"/> 抛出的异常会被捕获并记录，<strong>不会中断计时器，也不会传播给调用方</strong>，</para>
    /// <para>因此回调里的错误只会在 Console 里看到，业务侧不会收到通知。</para>
    /// <para>关于取消的时机（边界情况）：取消是在计时器回调内部发生时，本帧已经排队的补执行<strong>仍会继续跑完</strong>，</para>
    /// <para>因为 <c>running</c> / <c>IsDone</c> 只在每次 <c>Update</c> 进入时检查一次。要避免这一点，</para>
    /// <para>可以自行把间隔设得足够大，或者在回调里额外加一个标志位判断。</para>
    /// </remarks>
    internal abstract class AbstractTimer
    {
        /// <summary>单帧最大补执行次数，防止大 deltaTime 导致卡死</summary>
        private const int MaxExecutionsPerFrame = 100;

        protected float interval;
        protected float remainTime;

        protected bool running;
        protected bool paused;

        /// <summary>执行次数：&gt;0 执行指定次数，&lt;=0 无限循环</summary>
        protected int repeatCount;
        protected int currentCount;

        /// <summary>计时器是否已结束（Stop 被调用或执行次数达到上限）</summary>
        public bool IsDone { get; protected set; }

        /// <summary>计时器是否正在运行（已启动且未暂停）</summary>
        public bool IsRunning => running && !paused;

        /// <param name="interval">间隔时间（秒），必须大于 0</param>
        /// <param name="repeatCount">执行次数：&gt;0 执行指定次数，&lt;=0 无限循环</param>
        /// <exception cref="ArgumentOutOfRangeException">interval &lt;= 0 时抛出</exception>
        public AbstractTimer(float interval, int repeatCount)
        {
            if (interval <= 0f)
                throw new ArgumentOutOfRangeException(
                    nameof(interval), "interval 必须大于 0");

            this.interval = interval;
            this.repeatCount = repeatCount;

            remainTime = interval;
            currentCount = 0;
        }

        /// <summary>
        /// 推进计时，到点则执行并在需要时自动结束
        /// <para>由 <c>TimerManager</c> 每帧在 <c>Update</c> 中调用，业务代码不要手动调用</para>
        /// </summary>
        /// <param name="deltaTime">距离上一帧的秒数，通常为 <c>Time.deltaTime</c>；小于等于 0 时直接跳过</param>
        /// <remarks>
        /// <para>已结束、未启动或处于暂停状态的计时器会被直接跳过，不做任何推进。</para>
        /// <para>注意 <c>deltaTime</c> 来自 <c>Time.deltaTime</c>，会受 <c>Time.timeScale</c> 影响，
        /// 因此游戏暂停时计时器也会暂停。</para>
        /// </remarks>
        public void Update(float deltaTime)
        {
            if (!running || paused || IsDone)
                return;

            if (deltaTime <= 0f)
                return;

            remainTime -= deltaTime;

            int executionsThisFrame = 0;

            while (remainTime <= 0f)
            {
                if (executionsThisFrame >= MaxExecutionsPerFrame)
                {
                    // 补帧次数达到上限，放弃本帧剩余次数避免卡死
                    remainTime = interval;
                    break;
                }

                try
                {
                    Execute();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }

                executionsThisFrame++;

                if (repeatCount > 0)
                {
                    currentCount++;

                    if (currentCount >= repeatCount)
                    {
                        Stop();
                        break;
                    }
                }

                remainTime += interval;
            }
        }

        /// <summary>
        /// 执行一次计时到点的逻辑，由子类实现
        /// <para>异常会被 <see cref="Update"/> 捕获并记录，不会传播出去</para>
        /// </summary>
        protected abstract void Execute();

        /// <summary>启动计时器（会重置时间和次数）</summary>
        public virtual void Start()
        {
            remainTime = interval;
            currentCount = 0;
            running = true;
            paused = false;
            IsDone = false;
        }

        /// <summary>
        /// 结束计时器
        /// <para>会把 <see cref="IsDone"/> 置为 true，<c>TimerManager</c> 在下一帧清掉它</para>
        /// </summary>
        /// <remarks>
        /// <para>本方法不重置 <c>currentCount</c>，只是停止推进；想重新用请调用 <see cref="Reset"/> 后再 <see cref="Start"/>。</para>
        /// <para>可重复调用，已结束的计时器再次 <c>Stop</c> 不会有副作用。</para>
        /// </remarks>
        public virtual void Stop()
        {
            running = false;
            IsDone = true;
        }

        /// <summary>
        /// 暂停计时，恢复后从暂停处继续
        /// <para>未启动或已结束时是安全的空操作</para>
        /// </summary>
        public virtual void Pause()
        {
            if (!running || IsDone)
                return;

            paused = true;
        }

        /// <summary>
        /// 恢复被暂停的计时
        /// <para>未启动或已结束时是安全的空操作；对未暂停的计时器调用也无副作用</para>
        /// </summary>
        public virtual void Resume()
        {
            if (!running || IsDone)
                return;

            paused = false;
        }

        /// <summary>完全重置到初始状态（不自动启动，需要调用 Start()）</summary>
        public virtual void Reset()
        {
            remainTime = interval;
            currentCount = 0;
            running = false;
            paused = false;
            IsDone = false;
        }
    }
}
