using System;

namespace ToolKit
{
    internal abstract class AbstractTimer
    {
        /// <summary>单帧最大补执行次数，防止大 deltaTime 导致卡死</summary>
        private const int MaxExecutionsPerFrame = 100;

        protected float interval;
        protected float remainTime;

        protected bool running;
        protected bool paused;

        /// <summary>执行次数：>0 执行指定次数，<=0 无限循环</summary>
        protected int repeatCount;
        protected int currentCount;

        /// <summary>计时器是否已结束（Stop 被调用或执行次数达到上限）</summary>
        public bool IsDone { get; protected set; }

        /// <summary>计时器是否正在运行（已启动且未暂停）</summary>
        public bool IsRunning => running && !paused;

        /// <param name="interval">间隔时间（秒），必须大于 0</param>
        /// <param name="repeatCount">执行次数：>0 执行指定次数，<=0 无限循环</param>
        /// <exception cref="ArgumentOutOfRangeException">interval <= 0 时抛出</exception>
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

        public virtual void Stop()
        {
            running = false;
            IsDone = true;
        }

        public virtual void Pause()
        {
            if (!running || IsDone)
                return;

            paused = true;
        }

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
