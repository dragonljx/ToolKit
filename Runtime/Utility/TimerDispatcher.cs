using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace ToolKits
{
    #region 时间调度对象

    public enum TimerType
    {
        Destroy,
        Stop,
        Register,
    }

    public abstract class TimerBase
    {
        /// <summary>
        /// 剩余时长
        /// </summary>
        protected float residueTime;

        /// <summary>
        /// 倒计时长
        /// </summary>
        protected float time;

        /// <summary>
        /// 剩余循环次数
        /// </summary>
        protected int remainingNumber;

        protected int runNumber;
        protected bool autoKill;
        protected bool existList;
        protected bool isPause;

        protected Action<TimerBase, TimerType> removeCall;

        /// <summary>
        /// unity  update 计时器
        /// </summary>
        /// <param name="time"></param>
        /// <param name="runNumber">执行次数</param>
        /// <param name="autoKill">执行完之后是否杀死计时器</param>
        /// <param name="immediately">是否立刻运行</param>
        /// <param name="removeCall"></param>
        public TimerBase(float time, int runNumber, bool autoKill, bool immediately,
            Action<TimerBase, TimerType> removeCall)
        {
            this.time = time;
            this.residueTime = time;
            this.remainingNumber = runNumber;
            this.runNumber = runNumber;
            this.autoKill = autoKill;
            this.removeCall = removeCall;
            this.existList = immediately;
        }

        public virtual void UpdateTime(float dateTime) { }

        /// <summary>
        /// 重置时间
        /// </summary>
        public void Reset()
        {
            residueTime = time;
        }

        /// <summary>
        /// 设置新的时间并重置
        /// </summary>
        public void Reset(float newTime)
        {
            residueTime = time = newTime;
        }

        /// <summary>
        /// 开始 
        /// 如果执行了 Stop 需要使用该函数才能重新启动
        /// </summary>
        public void Start()
        {
            if (!existList)
            {
                existList = true;
                removeCall.Invoke(this, TimerType.Register);
                remainingNumber = runNumber;
            }

            Resume();
            Reset();
        }

        /// <summary>
        /// 开始
        /// 如果执行了 Stop 需要使用该函数才能重新启动
        /// </summary>
        public void Start(float newTime)
        {
            if (!existList)
            {
                existList = true;
                removeCall.Invoke(this, TimerType.Register);
                remainingNumber = runNumber;
            }

            Resume();
            Reset(newTime);
        }

        public void Pause()
        {
            isPause = true;
        }

        public void Resume()
        {
            isPause = false;
        }

        public void Stop()
        {
            if (existList)
            {
                existList = false;
                removeCall.Invoke(this, TimerType.Stop);
            }
        }
    }

    public class AsyncTimer
    {
        private int time;
        private Timer timer;
        private int remainingNumber;
        private int runNumber;
        private bool autoKill;
        private Action<AsyncTimer> timerAction;
        private Action<AsyncTimer> stopAction;

        /// <summary>
        /// 倒计时
        /// </summary>
        /// <param name="time">间隔时间</param>
        /// <param name="runNumber">调用次数 如果为-1则持续调用</param>
        /// <param name="autoKill">执行完之后是否杀死计时器</param>
        /// <param name="immediately">是否立刻运行</param>
        /// <param name="timerCall">倒计时结束执行的方法</param>
        /// <param name="stopCall">结束调用</param>
        public AsyncTimer(float time, int runNumber, bool autoKill, bool immediately, Action<AsyncTimer> timerCall,
            Action<AsyncTimer> stopCall)
        {
            remainingNumber = runNumber;
            this.runNumber = runNumber;
            this.autoKill = autoKill;
            this.time = (int)(time * 1000);
            this.timerAction = timerCall;
            this.stopAction = stopCall;

            int firstTime;

            if (immediately)
            {
                firstTime = this.time;
            }
            else
            {
                firstTime = Timeout.Infinite;
            }

            CreateTimer(firstTime);
        }

        private void CreateTimer(int firstTime)
        {
            timer = new Timer(Run, null, firstTime, firstTime);
        }

        private void Run(object obj)
        {
            lock (this)
            {
                remainingNumber--;
                timerAction.Invoke(this);
                if (remainingNumber == 0)
                {
                    if (this.autoKill)
                    {
                        timer.Dispose();
                        stopAction?.Invoke(this);
                    }
                    else
                    {
                        Stop();
                    }
                }
            }
        }

        /// <summary>
        /// 修改调用间隔并重置
        /// </summary>
        /// <param name="time"></param>
        public void Reset(float time)
        {
            this.time = (int)(time * 1000);
            timer.Change(this.time, this.time);
        }

        /// <summary>
        /// 重置调用时间
        /// </summary>
        public void Restart()
        {
            timer.Change(this.time, this.time);
        }

        public void Stop()
        {
            remainingNumber = runNumber;
            timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public void Dispose()
        {
            timer?.Dispose();
        }
    }

    public class QueueTimer : TimerBase
    {
        /// <summary>
        /// 当前运行的位置
        /// </summary>
        private int currentIndex = 0;

        private int actionSize;

        Action[] actionArray;

        /// <summary>
        /// unity  update 计时器
        /// </summary>
        /// <param name="time"></param>
        /// <param name="runNumber">执行次数</param>
        /// <param name="autoKill">执行完之后是否杀死计时器</param>
        /// <param name="immediately">是否立刻运行</param>
        /// <param name="actionArray"></param>
        /// <param name="removeCall"></param>
        public QueueTimer(float time, int runNumber, bool autoKill, bool immediately, Action[] actionArray,
            Action<TimerBase, TimerType> removeCall) :
            base(time, runNumber, autoKill, immediately, removeCall)
        {
            this.actionArray = actionArray;
            actionSize = actionArray.Length;
            currentIndex = 0;
        }

        public override void UpdateTime(float dateTime)
        {
            Debug.Log(isPause);
            if (isPause) return;
            residueTime -= dateTime;
            if (residueTime <= 0)
            {
                residueTime = 0f;

                actionArray[currentIndex]?.Invoke();
                currentIndex++;
                Reset();

                // 循环结束后 进行下一步判定
                if (currentIndex == actionSize)
                {
                    if (remainingNumber != -1)
                    {
                        remainingNumber--;
                        if (remainingNumber == 0)
                        {
                            existList = false;
                            if (autoKill)
                            {
                                removeCall.Invoke(this, TimerType.Destroy);
                            }
                            else
                            {
                                remainingNumber = runNumber;
                                removeCall.Invoke(this, TimerType.Stop);
                            }
                        }
                    }

                    currentIndex = 0;
                }
            }
        }
    }

    public class UpdateTimer : TimerBase
    {
        private Action callBack;


        /// <summary>
        /// unity  update 计时器
        /// </summary>
        /// <param name="time"></param>
        /// <param name="runNumber">执行次数</param>
        /// <param name="autoKill">执行完之后是否杀死计时器</param>
        /// <param name="immediately">是否立刻运行</param>
        /// <param name="callBack"></param>
        /// <param name="removeCall"></param>
        public UpdateTimer(float time, int runNumber, bool autoKill, bool immediately, Action callBack,
            Action<TimerBase, TimerType> removeCall) :
            base(time, runNumber, autoKill, immediately, removeCall)
        {
            this.callBack = callBack;
        }

        public override void UpdateTime(float dateTime)
        {
            if (isPause) return;
            residueTime -= dateTime;
            if (residueTime <= 0)
            {
                residueTime = 0f;

                if (remainingNumber != -1)
                {
                    remainingNumber--;
                    if (remainingNumber == 0)
                    {
                        existList = false;
                        if (autoKill)
                        {
                            removeCall?.Invoke(this, TimerType.Destroy);
                            callBack.Invoke();
                        }
                        else
                        {
                            remainingNumber = runNumber;
                            removeCall?.Invoke(this, TimerType.Stop);
                            callBack.Invoke();
                        }

                        return;
                    }
                }

                callBack.Invoke();
                Reset();
            }
        }
    }

    #endregion


    /// <summary>
    /// 多线程调用
    /// </summary>
    public class TimerDispatcher : Singleton<TimerDispatcher>
    {
        private event Action<float> updateEvent;
        private Queue<Action> asynEventQueue = new Queue<Action>();
        private List<AsyncTimer> countDownAsyncList = new List<AsyncTimer>();
        private List<TimerBase> countDownMainList = new List<TimerBase>();
        private static object _lock = new object();

        /// <summary>
        /// 启动后自动加载
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void StartRun()
        {
            _ = Instance;
        }

        /// <summary>
        /// 主线程执行一次
        /// </summary>
        /// <param name="callBack"></param>
        public static void Run(Action callBack)
        {
            AddCallback(callBack);
        }

        #region 协程

        /// <summary>
        /// 添加协程执行（使用协程）
        /// </summary>
        /// <param name="routine"></param>
        /// <returns></returns>
        public static Coroutine RunCoroutine(IEnumerator routine)
        {
            return Instance.StartCoroutine(routine);
        }

        /// <summary>
        /// 添加延迟执行（使用协程）
        /// </summary>
        /// <param name="time">需要延迟时间</param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static Coroutine RunCoroutine(float time, Action action, int number = 1)
        {
            return Instance.StartCoroutine(TimerCall(time, action, number));
        }

        static IEnumerator TimerCall(float time, Action action, int number)
        {
            while (number != 0)
            {
                number--;

                yield return new WaitForSeconds(time);
                action?.Invoke();
            }
        }

        /// <summary>
        /// 添加延迟帧执行（使用协程）
        /// </summary>
        /// <param name="frameNumber"> 需要延迟多少帧</param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static Coroutine RunCoroutineFrame(int frameNumber, Action action)
        {
            return Instance.StartCoroutine(TimerFrameCall(frameNumber, action));
        }

        /// <summary>
        /// 添加延迟物理帧执行（使用协程）
        /// </summary>
        /// <param name="frameNumber"> 需要延迟多少帧</param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static Coroutine RunCoroutineFixedFrame(int frameNumber, Action action)
        {
            return Instance.StartCoroutine(TimerFixedFrameCall(frameNumber, action));
        }

        /// <summary>
        /// 删除协程
        /// </summary>
        /// <param name="coroutine"></param>
        public static void RemoveCoroutine(Coroutine coroutine)
        {
            Instance.StopCoroutine(coroutine);
        }
        public static void RemoveCoroutine(IEnumerator coroutine)
        {
            Instance.StopCoroutine(coroutine);
        }

        private static IEnumerator TimerFrameCall(int frameNumber, Action action)
        {
            for (int i = 0; i < frameNumber; i++)
            {
                yield return null;
            }

            action?.Invoke();
        }

        private static IEnumerator TimerFixedFrameCall(int frameNumber, Action action)
        {
            for (int i = 0; i < frameNumber; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            action?.Invoke();
        }

        #endregion


        /// <summary>
        /// 延迟调用 在Update计算
        /// </summary>
        /// <param name="time">延迟时间</param>
        /// <param name="action">触发事件</param>
        /// <param name="autoKill">调用完成后删除</param>
        /// <param name="immediately">立刻执行</param>
        /// <param name="number">执行次数 如果为 -1 代表循环</param>
        /// <returns></returns>
        public static UpdateTimer RunTimer(float time, Action action, bool autoKill = true, bool immediately = true,
            int number = 1)
        {
            UpdateTimer updateTimer = new UpdateTimer(time, number, autoKill, immediately, action, CountDownCall);
            if (immediately)
            {
                Instance.countDownMainList.Add(updateTimer);
            }

            return updateTimer;
        }

        /// <summary>
        /// 删除主线程倒计时
        /// </summary>
        /// <param name="timer"></param>
        public static void RemoveMainInvoke(TimerBase timer)
        {
            Instance.countDownMainList.Remove(timer);
        }


        /// <summary>
        /// 在多线程延迟执行 线程计时
        /// </summary>
        /// <param name="time">延迟时间</param>
        /// <param name="action">触发事件</param>
        /// <param name="autoKill">调用完成后删除</param>
        /// <param name="immediately">立刻执行</param>
        /// <param name="number">执行次数 如果为 -1 代表循环</param>
        public static AsyncTimer RunTimerAsync(float time, Action<AsyncTimer> callBack, bool autoKill = true,
            bool immediately = true, int number = 1)
        {
            AsyncTimer asyncTimer = new AsyncTimer(time, number, autoKill, immediately, callBack, RemoveAsyncInvoke);
            Instance.countDownAsyncList.Add(asyncTimer);
            return asyncTimer;
        }

        /// <summary>
        /// 删除多线程倒计时
        /// </summary>
        /// <param name="asyncTimer"></param>
        public static void RemoveAsyncInvoke(AsyncTimer asyncTimer)
        {
            Instance.countDownAsyncList.Remove(asyncTimer);
            asyncTimer.Dispose();
        }


        private static void CountDownCall(TimerBase timer, TimerType type)
        {
            switch (type)
            {
                case TimerType.Destroy:
                    Instance.countDownMainList.Remove(timer);
                    timer = null;
                    break;
                case TimerType.Stop:

                    Instance.countDownMainList.Remove(timer);
                    timer = null;
                    break;
                case TimerType.Register:
                    if (!Instance.countDownMainList.Contains(timer))
                    {
                        Instance.countDownMainList.Add(timer);
                    }

                    break;
            }
        }

        private static void AddCallback(Action callBack)
        {
            lock (_lock)
            {
                Instance.asynEventQueue.Enqueue(callBack);
            }
        }


        /// <summary>
        /// 添加主线程Update调用（包含时间参数）
        /// </summary>
        /// <param name="action"></param>
        public static Action<float> AddUpdateInvoke(Action<float> action)
        {
            Instance.updateEvent += action;
            return action;
        }

        /// <summary>
        /// 删除指定 AddUpdateInvoke 调用
        /// 
        /// </summary>
        /// <param name="action"></param>
        public static void RemoveUpdateInvoke(Action<float> action)
        {
            Instance.updateEvent -= action;
        }

        /// <summary>
        /// 清空所有 AddUpdateInvoke 调用
        /// </summary>
        public static void RemoveAllUpdate()
        {
            Instance.updateEvent = null;
        }

        /// <summary>
        /// 顺序调用事件队列
        /// </summary>
        /// <param name="time">单次调用间隔</param>
        /// <param name="callList">事件队列</param>
        /// <param name="autoKill">是-否 调用结束后删除</param>
        /// <param name="immediately">是-否 立刻进行调用</param>
        /// <param name="number">循环调用次数 -1代表循环</param>
        /// <returns></returns>
        public static QueueTimer RunSequentialTimer(float time, Action[] callList, bool autoKill = true,
            bool immediately = true, int number = 1)
        {
            QueueTimer queueTimer = new QueueTimer(time, number, autoKill, immediately, callList, CountDownCall);

            if (immediately)
            {
                Instance.countDownMainList.Add(queueTimer);
            }

            return queueTimer;
        }

        void Update()
        {
            // 调用异步方法
            while (asynEventQueue.Count > 0)
            {
                asynEventQueue.Dequeue()?.Invoke();
            }


            float time = Time.deltaTime;

            // 调用updat方法
            updateEvent?.Invoke(time);

            for (int i = 0; i < countDownMainList.Count; i++)
            {
                countDownMainList[i].UpdateTime(time);
            }
        }

        public void OnDestroy()
        {
            for (int i = 0; i < countDownAsyncList.Count; i++)
            {
                countDownAsyncList[i].Dispose();
            }
        }
    }
}