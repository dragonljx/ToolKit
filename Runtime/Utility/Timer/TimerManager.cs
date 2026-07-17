using System;
using System.Collections.Generic;
using UnityEngine;

namespace ToolKit
{
    public class TimerManager : Singleton<TimerManager>
    {

        private readonly List<AbstractTimer> timers = new List<AbstractTimer>();
        private readonly Queue<Action> queue = new Queue<Action>();

        /// <summary>每帧最大处理队列数量，防止无限灌入导致卡死</summary>
        private const int MaxQueuePerFrame = 256;

        // ---- 单例生命周期 ----

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

        /// <summary>延迟执行（执行一次）</summary>
        public static TimerHandle Delay(float time, Action callback)
        {
            return CreateTimer(time, 1, callback);
        }

        /// <summary>重复执行</summary>
        /// <param name="count">执行次数，<=0 表示无限循环</param>
        public static TimerHandle Loop(float interval, Action callback, int count = -1)
        {
            return CreateTimer(interval, count, callback);
        }

        /// <summary>顺序执行一组 Action</summary>
        /// <param name="loopCount">循环次数：1 = 只跑一遍，<=0 = 无限循环</param>
        public static TimerHandle Sequence(float interval, Action[] actions, int loopCount = 1)
        {
            var timer = new SequenceTimer(interval, actions, loopCount);

            timer.Start();

            Instance.timers.Add(timer);

            return new TimerHandle(timer);
        }

        private static TimerHandle CreateTimer(float time, int count, Action callback)
        {
            var timer = new UpdateTimer(time, count, callback);

            timer.Start();

            Instance.timers.Add(timer);

            return new TimerHandle(timer);
        }


        // ---- 主线程派发 ----

        /// <summary>将 Action 派发到 Unity 主线程执行</summary>
        /// <remarks>可从任意线程调用，线程安全</remarks>
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
