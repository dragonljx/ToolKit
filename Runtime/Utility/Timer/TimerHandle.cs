namespace ToolKit
{
    /// <summary>
    /// 计时器的操作句柄，用来取消、暂停或查询一个已启动的计时任务
    /// <para>用法：由 <c>TimerManager</c> 的 <c>Delay</c> / <c>Loop</c> / <c>Sequence</c> 返回，持有它才能控制该任务</para>
    /// <code>
    /// // 只跑一次的延迟任务
    /// TimerHandle handle = TimerManager.Delay(2f, () => DLogger.Log("两秒后执行"));
    ///
    /// // 不需要时可以取消，避免回调在对象销毁后仍然触发
    /// handle.Cancel();
    ///
    /// // 循环任务可以暂停与恢复
    /// TimerHandle loop = TimerManager.Loop(0.5f, Tick);
    /// loop.Pause();
    /// loop.Resume();
    ///
    /// // 查询状态，注意取消之后 IsDone 也是 true
    /// bool finished = handle.IsDone;
    /// bool active = loop.IsRunning;
    /// </code>
    /// <para>句柄本身不是 Unity 对象，但计时器由 <c>TimerManager</c> 驱动，</para>
    /// <para>而 <c>TimerManager</c> 是挂在 <c>DontDestroyOnLoad</c> 物体上的，因此跨场景切换后句柄仍然有效</para>
    /// </summary>
    /// <remarks>
    /// <para>关于取消的语义：</para>
    /// <list type="bullet">
    /// <item><see cref="Cancel"/> 会停止计时并把内部引用置空，因此可以安全地重复调用</item>
    /// <item>取消之后 <see cref="IsDone"/> 返回 true，<see cref="IsRunning"/> 返回 false</item>
    /// <item>被取消的计时器要到 <c>TimerManager</c> 的下一帧 <c>Update</c> 才会从任务列表中移除，
    /// 但这期间不会再有回调触发</item>
    /// <item>句柄没有实现 <c>IDisposable</c>，不能放进 <c>using</c> 块；请在 <c>OnDestroy</c> / <c>OnDisable</c> 里显式取消</item>
    /// </list>
    /// <para>关于资源回收：句柄不持有任何需要释放的资源，计时器跑完或取消后会自行从列表移除，</para>
    /// <para>不需要（也无法）手动释放句柄本身，让它被 GC 回收即可。</para>
    /// <para>关于自动取消：任务达到执行次数上限后会自行结束，此时 <see cref="IsDone"/> 变为 true，</para>
    /// <para>但内部引用不会被清空，所以 <see cref="Pause"/> / <see cref="Resume"/> 仍会做一次空转调用。</para>
    /// </remarks>
    public sealed class TimerHandle
    {
        private AbstractTimer timer;


        /// <summary>
        /// 包装一个计时器实例
        /// <para>构造函数为 <c>internal</c>，只能由 <c>TimerManager</c> 创建</para>
        /// </summary>
        /// <param name="timer">要控制的计时器</param>
        internal TimerHandle(AbstractTimer timer)
        {
            this.timer = timer;
        }


        /// <summary>
        /// 计时器是否已结束（Cancel 被调用或执行次数达到上限）
        /// <para>已取消的句柄同样返回 true，无法据此区分「正常跑完」与「被取消」</para>
        /// </summary>
        public bool IsDone => timer == null || timer.IsDone;

        /// <summary>
        /// 计时器是否正在运行
        /// <para>已结束、已取消或处于暂停状态时都返回 false，需要区分暂停与结束请配合 <see cref="IsDone"/> 判断</para>
        /// </summary>
        public bool IsRunning => timer != null && timer.IsRunning;

        /// <summary>
        /// 取消计时器，之后不会再触发回调
        /// <code>
        /// private TimerHandle handle;
        ///
        /// private void OnDestroy()
        /// {
        ///     // 对象销毁后必须取消，否则回调里会访问到已销毁的对象
        ///     handle?.Cancel();
        /// }
        /// </code>
        /// </summary>
        /// <remarks>
        /// <para>幂等操作，重复调用或对已结束的计时器调用都不会出错。</para>
        /// <para>取消只影响后续回调，已经排队等待本帧补执行的次数会被一并停止。</para>
        /// </remarks>
        public void Cancel()
        {
            timer?.Stop();
            timer = null;
        }


        /// <summary>
        /// 暂停计时，恢复后从暂停处继续而不是重新开始
        /// <para>已取消或已结束时是安全的空操作</para>
        /// </summary>
        public void Pause()
        {
            timer?.Pause();
        }


        /// <summary>
        /// 恢复被暂停的计时
        /// <para>已取消或已结束时是安全的空操作</para>
        /// </summary>
        public void Resume()
        {
            timer?.Resume();
        }
    }
}
