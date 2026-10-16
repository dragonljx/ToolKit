using System;
using System.Collections.Generic;
using UnityEngine;

namespace ToolKit
{
    /// <summary>
    /// 全局计时器管理器，用 <c>Update</c> 驱动全部定时任务，并提供跨线程的主线程派发能力
    /// <para>用法：直接调用静态方法创建任务，返回的句柄用于取消和暂停；不需要手动初始化</para>
    /// <code>
    /// // 延迟执行一次
    /// TimerHandle delay = TimerManager.Delay(1f, () => DLogger.Log("一秒后执行"));
    ///
    /// // 每 0.5 秒执行一次，共 5 次（count <= 0 表示无限循环）
    /// TimerHandle loop = TimerManager.Loop(0.5f, Tick, count: 5);
    ///
    /// // 顺序执行一组回调，每 0.3 秒走一步
    /// TimerHandle seq = TimerManager.Sequence(0.3f, new Action[]
    /// {
    ///     () => DLogger.Log("第一步"),
    ///     () => DLogger.Log("第二步"),
    ///     () => DLogger.Log("第三步"),
    /// });
    ///
    /// // 控制：暂停 / 恢复 / 取消
    /// loop.Pause();
    /// loop.Resume();
    /// seq.Cancel();
    ///
    /// // 从后台线程切回主线程执行
    /// Task.Run(() =>
    /// {
    ///     TimerManager.MainDispatcher(() => transform.position = Vector3.zero);
    /// });
    /// </code>
    /// <para>管理器自身是 <c>SingletonMono</c>，会在场景加载前自动创建并常驻，</para>
    /// <para>也可以手动往场景里放一个 <c>TimerManager</c> 物体来接管（此时会复用该实例）</para>
    /// </summary>
    /// <remarks>
    /// <para>关于时间源：所有计时都基于 <c>Time.deltaTime</c>，<strong>会受 <c>Time.timeScale</c> 影响</strong>，</para>
    /// <para>游戏暂停（<c>timeScale = 0</c>）时计时器也会一起停住。需要不受暂停影响的计时，</para>
    /// <para>本类没有直接支持，请自行用协程配合 <c>WaitForSecondsRealtime</c>。</para>
    /// <para>关于线程：</para>
    /// <list type="bullet">
    /// <item><c>Delay</c> / <c>Loop</c> / <c>Sequence</c> 会访问 <c>Instance</c>，
    /// 可能触发 GameObject 创建，<strong>必须在主线程调用</strong></item>
    /// <item><see cref="MainDispatcher"/> 是唯一可以从任意线程调用的方法，它直接读内部字段，
    /// 刻意不触发懒加载，避免在子线程调用 Unity API</item>
    /// <item>但这也意味着 <c>MainDispatcher</c> 要求管理器已经初始化，否则只记录一条错误日志并丢弃任务</item>
    /// </list>
    /// <para>关于异常：计时器回调抛出的异常会被捕获并记录，不会中断其他计时器，也不会传播给调用方。</para>
    /// <para>关于执行顺序：每帧先推进所有计时器，再处理主线程派发队列。</para>
    /// </remarks>
    public class TimerManager : SingletonMono<TimerManager>
    {

        private readonly List<AbstractTimer> timers = new List<AbstractTimer>();
        private readonly Queue<Action> queue = new Queue<Action>();

        /// <summary>每帧最大处理队列数量，防止无限灌入导致卡死</summary>
        private const int MaxQueuePerFrame = 256;

        // ---- 单例生命周期 ----

        /// <summary>
        /// 在场景加载前确保存在一个常驻的 TimerManager，让业务代码可以在任意脚本里直接调用静态方法
        /// <para>由 <c>RuntimeInitializeOnLoadMethod</c> 自动触发，不要手动调用</para>
        /// </summary>
        /// <remarks>
        /// <para>首行的 <c>Instance != null</c> 会触发 <c>SingletonMono</c> 的懒创建逻辑，</para>
        /// <para>该逻辑本身已经完成了「查找场景实例、找不到则新建」的工作，所以它通常为 true，
        /// 后面的查找与创建分支在正常流程下不会执行到。</para>
        /// </remarks>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (Instance != null)
                return;

            var existing = FindObjectOfType<TimerManager>();
            if (existing != null)
            {
                Instance = existing;
                DontDestroyOnLoad(existing.gameObject);
                return;
            }

            var obj = new GameObject(nameof(TimerManager));
            DontDestroyOnLoad(obj);
            Instance = obj.AddComponent<TimerManager>();
        }


        // ---- 公开 API ----

        /// <summary>
        /// 延迟指定时间后执行一次
        /// <code>
        /// // 3 秒后自动关闭弹窗
        /// TimerHandle handle = TimerManager.Delay(3f, () => popup.Close());
        ///
        /// // 用户提前手动关闭时记得取消，避免回调访问到已销毁的对象
        /// handle.Cancel();
        /// </code>
        /// </summary>
        /// <param name="time">延迟时间（秒），必须大于 0</param>
        /// <param name="callback">到点后执行的回调</param>
        /// <returns>用于取消或查询状态的操作句柄</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="time"/> 小于等于 0 时抛出</exception>
        /// <remarks>
        /// <para>注意计时从「创建的那一刻」开始算满一个完整间隔，<strong>不会立刻触发一次</strong>。</para>
        /// <para>本方法必须在本帧内创建了句柄才能取消；不需要取消的场景可以直接忽略返回值。</para>
        /// </remarks>
        public static TimerHandle Delay(float time, Action callback)
        {
            return CreateTimer(time, 1, callback);
        }

        /// <summary>
        /// 按固定间隔重复执行
        /// <code>
        /// // 无限循环，直到手动取消
        /// TimerHandle tick = TimerManager.Loop(1f, () => DLogger.Log("每秒一次"));
        ///
        /// // 只执行 5 次后自动结束
        /// TimerHandle limited = TimerManager.Loop(0.2f, UpdateProgress, count: 5);
        /// </code>
        /// </summary>
        /// <param name="interval">间隔时间（秒），必须大于 0</param>
        /// <param name="callback">每次到点执行的回调</param>
        /// <param name="count">执行次数，&lt;=0 表示无限循环（默认 -1）</param>
        /// <returns>用于取消、暂停或查询状态的操作句柄</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="interval"/> 小于等于 0 时抛出</exception>
        /// <remarks>
        /// <para>第一次执行同样要等满一个间隔，不是立刻执行。</para>
        /// <para>执行完最后一次后计时器会自行标记为结束，<c>TimerManager</c> 在下一帧把它从列表中移除。</para>
        /// </remarks>
        public static TimerHandle Loop(float interval, Action callback, int count = -1)
        {
            return CreateTimer(interval, count, callback);
        }

        /// <summary>
        /// 按固定间隔顺序执行一组回调，走到末尾后从头循环
        /// <code>
        /// // 每 0.5 秒推进一步，走完 4 步就结束
        /// TimerHandle step = TimerManager.Sequence(0.5f, new Action[]
        /// {
        ///     () => SetStage(1),
        ///     () => SetStage(2),
        ///     () => SetStage(3),
        ///     () => SetStage(4),
        /// });
        ///
        /// // 无限循环的跑马灯
        /// TimerManager.Sequence(0.2f, lights, loopCount: -1);
        /// </code>
        /// </summary>
        /// <param name="interval">每步之间的间隔时间（秒），必须大于 0</param>
        /// <param name="actions">要顺序执行的回调数组，不能为 null 或空</param>
        /// <param name="loopCount">整组回调的循环次数：1 = 只跑一遍（默认），&lt;=0 = 无限循环</param>
        /// <returns>用于取消、暂停或查询状态的操作句柄</returns>
        /// <exception cref="ArgumentException"><paramref name="actions"/> 为 null 或空数组时抛出</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="interval"/> 小于等于 0 时抛出</exception>
        /// <remarks>
        /// <para><paramref name="loopCount"/> 指的是整组数组跑几个来回，不是单个回调的重复次数。</para>
        /// <para>数组元素允许为 null，执行到时会跳过，但仍会占用一个间隔，可用于制造停顿。</para>
        /// </remarks>
        public static TimerHandle Sequence(float interval, Action[] actions, int loopCount = 1)
        {
            var timer = new SequenceTimer(interval, actions, loopCount);

            timer.Start();

            Instance.timers.Add(timer);

            return new TimerHandle(timer);
        }

        /// <summary>
        /// 创建并启动一个普通计时器的公共逻辑
        /// </summary>
        /// <param name="time">间隔时间（秒），必须大于 0</param>
        /// <param name="count">执行次数，&lt;=0 表示无限循环</param>
        /// <param name="callback">到点后执行的回调</param>
        /// <returns>包装好的操作句柄</returns>
        /// <remarks>
        /// <para>计时器在返回句柄之前就已经 <c>Start</c> 并加入列表，因此调用方拿到句柄时任务已经开始计时，
        /// 不存在「创建了但没跑」的中间状态。</para>
        /// </remarks>
        private static TimerHandle CreateTimer(float time, int count, Action callback)
        {
            var timer = new UpdateTimer(time, count, callback);

            timer.Start();

            Instance.timers.Add(timer);

            return new TimerHandle(timer);
        }


        // ---- 主线程派发 ----

        /// <summary>
        /// 把一个 <c>Action</c> 排入队列，在下一个主线程 <c>Update</c> 中执行
        /// <code>
        /// // 后台线程下载完成后回到主线程更新 UI
        /// Task.Run(() =>
        /// {
        ///     byte[] data = DownloadSync(url);
        ///
        ///     TimerManager.MainDispatcher(() => RefreshUI(data));
        /// });
        /// </code>
        /// </summary>
        /// <param name="action">要在主线程执行的操作，传 null 时直接返回</param>
        /// <remarks>
        /// <para>这是本类唯一可以在任意线程调用的方法，内部对队列加了锁。</para>
        /// <para>它<strong>刻意不访问 <c>Instance</c> 属性</strong>，而是直接读 <c>_instance</c> 字段，</para>
        /// <para>避免在子线程触发 GameObject 创建这类 Unity 主线程专属操作。</para>
        /// <para>代价是要求管理器已初始化：未初始化时只会记录一条错误日志并丢弃任务，不会自动补建。</para>
        /// <para>派发过来的操作在下一帧才执行，且单帧最多处理 <c>MaxQueuePerFrame</c> 条，
        /// 大量灌入会分多帧摊开，不会一次性卡死主线程。</para>
        /// </remarks>
        public static void MainDispatcher(Action action)
        {
            if (action == null)
                return;

            // 直接读字段，不触发懒加载（避免子线程调用 Unity API）
            var mgr = _instance;
            if (mgr == null)
            {
                Debug.LogError("TimerManager 尚未初始化，DispatchToMainThread 失败。");
                return;
            }

            lock (mgr.queue)
            {
                mgr.queue.Enqueue(action);
            }
        }

        // ---- Update 驱动 ----

        /// <summary>
        /// 每帧推进所有计时器，然后处理主线程派发队列
        /// <para>倒序遍历计时器列表，这样在遍历中移除已完成项不会打乱下标</para>
        /// </summary>
        private void Update()
        {
            float delta = Time.deltaTime;

            // 驱动所有计时器
            for (int i = timers.Count - 1; i >= 0; i--)
            {
                AbstractTimer timer = timers[i];

                timer.Update(delta);

                if (timer.IsDone)
                {
                    timers.RemoveAt(i);
                }
            }

            // 处理主线程派发队列
            ProcessQueue();
        }

        /// <summary>
        /// 从主线程派发队列中取出并执行操作，单帧处理数量有上限
        /// </summary>
        /// <remarks>
        /// <para>每次出队都单独加锁，而不是一次性把整批取出，这样执行耗时操作时新入队的任务不会被长时间阻塞。</para>
        /// <para>单个操作抛出异常会被捕获记录，不影响同一批里的其他操作。</para>
        /// </remarks>
        private void ProcessQueue()
        {
            int count;
            lock (queue)
            {
                count = Math.Min(queue.Count, MaxQueuePerFrame);
            }

            for (int i = 0; i < count; i++)
            {
                Action action = null;
                lock (queue)
                {
                    if (queue.Count == 0)
                        break;

                    action = queue.Dequeue();
                }

                if (action == null)
                    continue;

                try
                {
                    action.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}
