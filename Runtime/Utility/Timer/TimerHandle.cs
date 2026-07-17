namespace ToolKit
{
    public sealed class TimerHandle
    {
        private AbstractTimer timer;


        internal TimerHandle(AbstractTimer timer)
        {
            this.timer = timer;
        }


        /// <summary>
        /// 计时器是否已结束（Cancel 被调用或执行次数达到上限）
        /// </summary>
        public bool IsDone => timer == null || timer.IsDone;

        /// <summary>
        /// 计时器是否正在运行
        /// </summary>
        public bool IsRunning => timer != null && timer.IsRunning;

        public void Cancel()
        {
            timer?.Stop();
            timer = null;
        }


        public void Pause()
        {
            timer?.Pause();
        }


        public void Resume()
        {
            timer?.Resume();
        }
    }
}