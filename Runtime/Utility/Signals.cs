// ========================================================================================
//
// 信息传递工具 信号
//
// Signals - A typesafe, lightweight messaging lib for Unity.
// ========================================================================================
// 2017-2019, Yanko Oliveira  / http://yankooliveira.com / http://twitter.com/yankooliveira
// Special thanks to Max Knoblich for code review and Aswhin Sudhir for the anonymous
// function asserts suggestion.
// ========================================================================================
// Inspired by StrangeIOC, minus the clutter.
// Based on http://wiki.unity3d.com/index.php/CSharpMessenger_Extended
// Converted to use strongly typed parameters and prevent use of strings as ids.
//
// Supports up to 3 parameters. More than that, and you should probably use a VO.
//
// Usage:
//    1) Define your class, eg:
//          class ScoreSignal : ASignal<int> {}
//    2) Add listeners on portions that should react, eg on Awake():
//          Signals.Get<ScoreSignal>().AddListener(OnScore);
//    3) Dispatch, eg:
//          Signals.Get<ScoreSignal>().Dispatch(userScore);
//    4) Don't forget to remove the listeners upon destruction! Eg on OnDestroy():
//          Signals.Get<ScoreSignal>().RemoveListener(OnScore);
//    5) If you don't want to use global Signals, you can have your very own SignalHub
//       instance in your class
//
// ========================================================================================

using System;
using System.Collections.Generic;


namespace ToolKit
{

    /// <summary>
    /// 信号的基础接口，提供类型哈希
    /// <para>用法：一般不需要直接实现它，继承 <c>ASignal</c> 系列即可自动获得实现</para>
    /// <para>需要自定义哈希策略时才实现本接口，但此时无法用 <c>ASignal</c> 的监听管理逻辑</para>
    /// </summary>
    public interface ISignal
    {
        /// <summary>
        /// 信号在所属 Hub 内的唯一标识
        /// <para>默认实现由 <c>ABaseSignal</c> 提供，取类型的完整名称字符串</para>
        /// </summary>
        string Hash { get; }
    }

    /// <summary>
    /// 全局信号中心，整个游戏共享一套信号实例
    /// <para>用法：定义信号类后直接通过静态方法注册监听、派发</para>
    /// <code>
    /// // 1) 定义信号：一个带参数的信号
    /// public sealed class ScoreChangedSignal : ASignal&lt;int&gt; { }
    ///
    /// // 2) 注册监听，务必在 OnEnable / Awake 里配对注册
    /// void OnEnable()
    /// {
    ///     Signals.Get&lt;ScoreChangedSignal&gt;().AddListener(OnScoreChanged);
    /// }
    ///
    /// // 3) 注销监听，漏掉这步是内存泄漏和「重复回调」的主要来源
    /// void OnDisable()
    /// {
    ///     Signals.Get&lt;ScoreChangedSignal&gt;().RemoveListener(OnScoreChanged);
    /// }
    ///
    /// void OnScoreChanged(int score)
    /// {
    ///     scoreLabel.text = score.ToString();
    /// }
    ///
    /// // 4) 在任意位置派发，所有监听者都会被调用
    /// Signals.Get&lt;ScoreChangedSignal&gt;().Dispatch(120);
    /// </code>
    /// <para>需要实例级的信号中心（比如每个房间各自一套事件）时，改用 <see cref="SignalHub"/></para>
    /// </summary>
    /// <remarks>
    /// <para>关于生命周期（最容易踩的坑）：</para>
    /// <list type="bullet">
    /// <item>信号实例存放在<strong>静态</strong>字段里，不随场景切换销毁，监听列表也会一直保留</item>
    /// <item>注册了监听却忘记注销，会让已销毁对象的方法持续被调用，表现为 <c>MissingReferenceException</c>
    /// 或「同一个回调被触发多次」</item>
    /// <item>在关闭了 Domain Reload 的项目里，编辑器反复进出播放模式也不会清空监听，<strong>残留在上一轮的监听会继续被调用</strong></item>
    /// <item>建议在 <c>OnEnable</c> / <c>OnDisable</c> 中成对注册与注销，而不是 <c>Awake</c> / <c>OnDestroy</c></item>
    /// </list>
    /// <para>关于派发时的异常：<c>Dispatch</c> 不捕获异常，某个监听者抛出异常会中断本次派发，
    /// 排在它后面的监听者收不到通知。单个监听者里的错误请在内部自行 try/catch。</para>
    /// <para>关于派发过程中增删监听：<c>Dispatch</c> 只读取一次委托字段，
    /// 因此派发期间新增或移除的监听者要到<strong>下一次</strong>派发才生效，不会影响正在进行的这一轮。</para>
    /// <para>本类不含任何同步机制，不是线程安全的，请只在主线程派发。</para>
    /// </remarks>
    public static class Signals
    {
        private static readonly SignalHub hub = new SignalHub();

        /// <summary>
        /// 获取全局唯一的指定类型信号实例，首次访问时自动创建
        /// <code>
        /// Signals.Get&lt;GameStartSignal&gt;().Dispatch();
        /// </code>
        /// </summary>
        /// <typeparam name="SType">信号类型，需继承 <c>ABaseSignal</c> 系列并提供无参构造函数</typeparam>
        /// <returns>该类型的信号单例，同一类型多次调用返回同一个实例</returns>
        public static SType Get<SType>() where SType : ISignal, new()
        {
            return hub.Get<SType>();
        }

        /// <summary>
        /// 通过哈希字符串给信号注册监听
        /// <para>给「信号类型在编译期未知」的场景使用（如配置驱动、脚本热更）</para>
        /// </summary>
        /// <param name="signalHash">信号哈希，即类型完整名称，如 <c>ToolKit.ScoreChangedSignal</c></param>
        /// <param name="handler">无参回调，要注册带参信号的回调请改用类型化的 <c>AddListener</c></param>
        /// <remarks>
        /// <para>本方法内部会尝试把信号转成无参的 <c>ASignal</c>，<strong>因此只对无参信号有效</strong>。</para>
        /// <para>对 <c>ASignal&lt;T&gt;</c> 这类带参信号调用会静默地什么都不做，不报错也不注册。</para>
        /// <para>哈希对不上任何已注册信号时同样静默跳过。除非确实需要在运行时动态绑定，否则请优先用类型化的写法。</para>
        /// </remarks>
        public static void AddListenerToHash(string signalHash, Action handler)
        {
            hub.AddListenerToHash(signalHash, handler);
        }

        /// <summary>
        /// 通过哈希字符串注销信号监听
        /// </summary>
        /// <param name="signalHash">信号哈希，需与注册时一致</param>
        /// <param name="handler">要注销的回调，需与注册时是同一个委托</param>
        /// <remarks>
        /// <para>与 <see cref="AddListenerToHash"/> 相同，只对无参信号有效，其余情况静默跳过。</para>
        /// </remarks>
        public static void RemoveListenerFromHash(string signalHash, Action handler)
        {
            hub.RemoveListenerFromHash(signalHash, handler);
        }
    }

    /// <summary>
    /// 实例级信号中心，为每个使用方提供互不干扰的一套信号
    /// <para>用法：在需要隔离事件的类里持有一个 <c>SignalHub</c> 实例，用法与全局 <c>Signals</c> 完全一致</para>
    /// <code>
    /// public class Room
    /// {
    ///     // 每个房间各有一套自己的信号，房间之间互不影响
    ///     private readonly SignalHub hub = new SignalHub();
    ///
    ///     public void Enter()
    ///     {
    ///         hub.Get&lt;PlayerJoinedSignal&gt;().AddListener(OnPlayerJoined);
    ///     }
    ///
    ///     public void OnPlayerJoined(string playerId)
    ///     {
    ///         DLogger.Log("Room", $"{playerId} 加入了 {GetHashCode()}");
    ///     }
    ///
    ///     public void BroadcastJoin(string playerId)
    ///     {
    ///         hub.Get&lt;PlayerJoinedSignal&gt;().Dispatch(playerId);
    ///     }
    /// }
    /// </code>
    /// <para>相比全局 <c>Signals</c> 的优势是<strong>生命周期可控</strong>：Hub 实例被回收时，</para>
    /// <para>注册其中的所有监听也随之消失，不会像全局信号那样跨场景残留</para>
    /// </summary>
    /// <remarks>
    /// <para>关于与全局 <c>Signals</c> 的隔离：两者各持一个独立的 Hub，</para>
    /// <para>同一个信号类型在全局与局部会得到<strong>两个不同的实例</strong>，监听列表互不相通，注意不要混用。</para>
    /// <para>关于 <c>Hash</c>：由于哈希取自类型名，同一个 Hub 内两种不同的信号类型不会冲突；</para>
    /// <para>但若用 <c>AddListenerToHash</c> 手动指定哈希，需自行保证唯一性。</para>
    /// </remarks>
    public class SignalHub
    {
        private Dictionary<Type, ISignal> signals = new Dictionary<Type, ISignal>();

        /// <summary>
        /// 获取指定类型的信号实例，不存在时创建并缓存
        /// </summary>
        /// <typeparam name="SType">信号类型</typeparam>
        /// <returns>本 Hub 内的信号实例，同一类型多次调用返回同一个实例</returns>
        /// <remarks>
        /// <para>创建走 <c>Activator.CreateInstance</c>，因此信号类必须有公开的无参构造函数；</para>
        /// <para>写成 <c>public sealed class XxxSignal : ASignal&lt;int&gt; { }</c> 即可，不需要额外代码。</para>
        /// </remarks>
        public SType Get<SType>() where SType : ISignal, new()
        {
            Type signalType = typeof(SType);
            ISignal signal;

            if (signals.TryGetValue(signalType, out signal))
            {
                return (SType)signal;
            }

            return (SType)Bind(signalType);
        }

        /// <summary>
        /// Manually provide a SignalHash and bind it to a given listener
        /// (you most likely want to use an AddListener, unless you know exactly
        /// what you are doing)
        /// </summary>
        /// <param name="signalHash">Unique hash for signal</param>
        /// <param name="handler">Callback for signal listener</param>
        public void AddListenerToHash(string signalHash, Action handler)
        {
            ISignal signal = GetSignalByHash(signalHash);
            if (signal != null && signal is ASignal)
            {
                (signal as ASignal).AddListener(handler);
            }
        }

        /// <summary>
        /// Manually provide a SignalHash and unbind it from a given listener
        /// (you most likely want to use a RemoveListener, unless you know exactly
        /// what you are doing)
        /// </summary>
        /// <param name="signalHash">Unique hash for signal</param>
        /// <param name="handler">Callback for signal listener</param>
        public void RemoveListenerFromHash(string signalHash, Action handler)
        {
            ISignal signal = GetSignalByHash(signalHash);
            if (signal != null && signal is ASignal)
            {
                (signal as ASignal).RemoveListener(handler);
            }
        }

        /// <summary>
        /// 创建并注册指定类型的信号
        /// </summary>
        /// <param name="signalType">要创建的信号类型，需有无参构造函数</param>
        /// <returns>新创建的信号实例；类型已注册时返回已有实例并记录错误日志</returns>
        /// <remarks>
        /// <para>正常流程不会走到「已注册」分支：<c>Get</c> 会先查缓存再调用本方法。</para>
        /// </remarks>
        private ISignal Bind(Type signalType)
        {
            ISignal signal;
            if (signals.TryGetValue(signalType, out signal))
            {
                UnityEngine.Debug.LogError(string.Format("Signal already registered for type {0}",
                    signalType.ToString()));
                return signal;
            }

            signal = (ISignal)Activator.CreateInstance(signalType);
            signals.Add(signalType, signal);
            return signal;
        }

        /// <summary>
        /// 创建并注册指定类型的信号（泛型重载）
        /// </summary>
        /// <typeparam name="T">要创建的信号类型</typeparam>
        /// <returns>新创建的信号实例</returns>
        private ISignal Bind<T>() where T : ISignal, new()
        {
            return Bind(typeof(T));
        }

        /// <summary>
        /// 按哈希查找已注册的信号
        /// </summary>
        /// <param name="signalHash">信号哈希</param>
        /// <returns>匹配到的信号；未注册过任何同哈希的信号时返回 null</returns>
        /// <remarks>
        /// <para>实现是线性遍历，信号数量很多时会有开销，不过实际项目中信号类型通常只有几十个，可以忽略。</para>
        /// </remarks>
        private ISignal GetSignalByHash(string signalHash)
        {
            foreach (ISignal signal in signals.Values)
            {
                if (signal.Hash == signalHash)
                {
                    return signal;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// 所有信号的抽象基类，提供「按类型生成哈希」的默认实现
    /// <para>用法：自定义信号时继承 <c>ASignal</c> 系列，本类由它们间接继承，不需要直接继承</para>
    /// </summary>
    /// <remarks>
    /// <para>哈希是惰性计算的：首次访问 <see cref="Hash"/> 时才取类型全名并缓存到 <c>_hash</c> 字段。</para>
    /// <para>由于类型名不随实例变化，同一个信号类型的所有实例哈希都相同。</para>
    /// </remarks>
    public abstract class ABaseSignal : ISignal
    {
        protected string _hash;

        /// <summary>
        /// Unique id for this signal
        /// </summary>
        public string Hash
        {
            get
            {
                if (string.IsNullOrEmpty(_hash))
                {
                    _hash = this.GetType().ToString();
                }

                return _hash;
            }
        }
    }

    /// <summary>
    /// 无参数的信号，用于「发生了某件事」这种不带数据的通知
    /// <para>用法：继承后即可通过 <c>Signals.Get&lt;你的信号&gt;()</c> 注册与派发</para>
    /// <code>
    /// // 定义
    /// public sealed class GameStartSignal : ASignal { }
    ///
    /// // 注册
    /// void OnEnable()  => Signals.Get&lt;GameStartSignal&gt;().AddListener(OnGameStart);
    /// void OnDisable() => Signals.Get&lt;GameStartSignal&gt;().RemoveListener(OnGameStart);
    ///
    /// void OnGameStart()
    /// {
    ///     DLogger.Log("Game", "游戏开始");
    /// }
    ///
    /// // 派发
    /// Signals.Get&lt;GameStartSignal&gt;().Dispatch();
    /// </code>
    /// </summary>
    /// <remarks>
    /// <para>关于匿名委托（重要限制）：<c>AddListener</c> 在编辑器下会断言拒绝匿名方法、Lambda 和闭包，
    /// 因为它们无法在 <c>RemoveListener</c> 时被重新构造出来，等于<strong>注册之后永远无法注销</strong>。</para>
    /// <para>请始终传具名方法或缓存的委托字段，不要写 <c>AddListener(() =&gt; ...)</c>。</para>
    /// <para>该断言只在编辑器（<c>UNITY_EDITOR</c>）中生效，构建后会被剔除，因此这类错误不会被带到线上，
    /// 但也意味着发布版本不会有任何提示，需要在开发阶段就发现。</para>
    /// </remarks>
    public abstract class ASignal : ABaseSignal
    {
        private Action callback;

        /// <summary>
        /// Adds a listener to this Signal
        /// </summary>
        /// <param name="handler">Method to be called when signal is fired</param>
        public void AddListener(Action handler)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Assert(
                handler.Method.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute),
                    inherit: false).Length == 0,
                "Adding anonymous delegates as Signal callbacks is not supported (you wouldn't be able to unregister them later).");
#endif
            callback += handler;
        }

        /// <summary>
        /// Removes a listener from this Signal
        /// </summary>
        /// <param name="handler">Method to be unregistered from signal</param>
        public void RemoveListener(Action handler)
        {
            callback -= handler;
        }

        /// <summary>
        /// Dispatch this signal
        /// </summary>
        public void Dispatch()
        {
            if (callback != null)
            {
                callback();
            }
        }
    }

    /// <summary>
    /// 带 1 个参数的信号，最常用的一种
    /// <para>用法：继承后用 <c>Dispatch(值)</c> 把数据广播给所有监听者</para>
    /// <code>
    /// // 定义
    /// public sealed class ScoreChangedSignal : ASignal&lt;int&gt; { }
    ///
    /// // 注册与注销必须成对出现
    /// void OnEnable()  => Signals.Get&lt;ScoreChangedSignal&gt;().AddListener(OnScoreChanged);
    /// void OnDisable() => Signals.Get&lt;ScoreChangedSignal&gt;().RemoveListener(OnScoreChanged);
    ///
    /// void OnScoreChanged(int score)
    /// {
    ///     scoreLabel.text = score.ToString();
    /// }
    ///
    /// // 派发
    /// Signals.Get&lt;ScoreChangedSignal&gt;().Dispatch(120);
    /// </code>
    /// </summary>
    /// <remarks>
    /// <para>参数是<strong>按值传递</strong>的：传引用类型时所有监听者拿到的是同一个对象，</para>
    /// <para>任一监听者修改它都会影响后续监听者。需要隔离时请在派发前拷贝，或改用不可变类型。</para>
    /// <para>同样不支持匿名委托，原因见 <c>ASignal</c> 的说明。</para>
    /// </remarks>
    /// <typeparam name="T">参数类型</typeparam>
    public abstract class ASignal<T> : ABaseSignal
    {
        private Action<T> callback;

        /// <summary>
        /// Adds a listener to this Signal
        /// </summary>
        /// <param name="handler">Method to be called when signal is fired</param>
        public void AddListener(Action<T> handler)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Assert(
                handler.Method.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute),
                    inherit: false).Length == 0,
                "Adding anonymous delegates as Signal callbacks is not supported (you wouldn't be able to unregister them later).");
#endif
            callback += handler;
        }

        /// <summary>
        /// Removes a listener from this Signal
        /// </summary>
        /// <param name="handler">Method to be unregistered from signal</param>
        public void RemoveListener(Action<T> handler)
        {
            callback -= handler;
        }

        /// <summary>
        /// Dispatch this signal with 1 parameter
        /// </summary>
        public void Dispatch(T arg1)
        {
            if (callback != null)
            {
                callback(arg1);
            }
        }
    }

    /// <summary>
    /// 带 2 个参数的信号
    /// <para>用法：与 <c>ASignal&lt;T&gt;</c> 完全一致，只是回调签名多一个参数</para>
    /// <code>
    /// public sealed class DamageSignal : ASignal&lt;int, string&gt; { }
    ///
    /// void OnEnable()  => Signals.Get&lt;DamageSignal&gt;().AddListener(OnDamage);
    /// void OnDisable() => Signals.Get&lt;DamageSignal&gt;().RemoveListener(OnDamage);
    ///
    /// void OnDamage(int amount, string source)
    /// {
    ///     DLogger.Log("Combat", $"{source} 造成 {amount} 点伤害");
    /// }
    ///
    /// Signals.Get&lt;DamageSignal&gt;().Dispatch(35, "Trap");
    /// </code>
    /// </summary>
    /// <remarks>
    /// <para>参数达到 3 个以上时可读性会明显下降，此时更推荐定义一个专用的小结构体作为单个参数传递。</para>
    /// </remarks>
    /// <typeparam name="T">第一个参数类型</typeparam>
    /// <typeparam name="U">第二个参数类型</typeparam>
    public abstract class ASignal<T, U> : ABaseSignal
    {
        private Action<T, U> callback;

        /// <summary>
        /// Adds a listener to this Signal
        /// </summary>
        /// <param name="handler">Method to be called when signal is fired</param>
        public void AddListener(Action<T, U> handler)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Assert(
                handler.Method.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute),
                    inherit: false).Length == 0,
                "Adding anonymous delegates as Signal callbacks is not supported (you wouldn't be able to unregister them later).");
#endif
            callback += handler;
        }

        /// <summary>
        /// Removes a listener from this Signal
        /// </summary>
        /// <param name="handler">Method to be unregistered from signal</param>
        public void RemoveListener(Action<T, U> handler)
        {
            callback -= handler;
        }

        /// <summary>
        /// Dispatch this signal
        /// </summary>
        public void Dispatch(T arg1, U arg2)
        {
            if (callback != null)
            {
                callback(arg1, arg2);
            }
        }
    }

    /// <summary>
    /// 带 3 个参数的信号
    /// <para>用法：与 <c>ASignal&lt;T&gt;</c> 一致，回调签名为三个参数</para>
    /// <code>
    /// public sealed class LootSignal : ASignal&lt;int, string, bool&gt; { }
    ///
    /// void OnLoot(int itemId, string name, bool isRare) { /* ... */ }
    ///
    /// Signals.Get&lt;LootSignal&gt;().Dispatch(1001, "长剑", false);
    /// </code>
    /// </summary>
    /// <remarks>
    /// <para>参数到 3 个之后，调用处的字面量已经很难分辨含义，建议把业务概念封装成结构体再传。</para>
    /// </remarks>
    /// <typeparam name="T">第一个参数类型</typeparam>
    /// <typeparam name="U">第二个参数类型</typeparam>
    /// <typeparam name="V">第三个参数类型</typeparam>
    public abstract class ASignal<T, U, V> : ABaseSignal
    {
        private Action<T, U, V> callback;

        /// <summary>
        /// Adds a listener to this Signal
        /// </summary>
        /// <param name="handler">Method to be called when signal is fired</param>
        public void AddListener(Action<T, U, V> handler)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Assert(
                handler.Method.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute),
                    inherit: false).Length == 0,
                "Adding anonymous delegates as Signal callbacks is not supported (you wouldn't be able to unregister them later).");
#endif
            callback += handler;
        }

        /// <summary>
        /// Removes a listener from this Signal
        /// </summary>
        /// <param name="handler">Method to be unregistered from signal</param>
        public void RemoveListener(Action<T, U, V> handler)
        {
            callback -= handler;
        }

        /// <summary>
        /// Dispatch this signal
        /// </summary>
        public void Dispatch(T arg1, U arg2, V arg3)
        {
            if (callback != null)
            {
                callback(arg1, arg2, arg3);
            }
        }
    }

    /// <summary>
    /// 带 4 个参数的信号，是本文件中参数最多的重载
    /// <para>用法：与 <c>ASignal&lt;T&gt;</c> 一致，回调签名为四个参数</para>
    /// <code>
    /// public sealed class CharacterSpawnSignal : ASignal&lt;int, string, Vector3, Quaternion&gt; { }
    ///
    /// Signals.Get&lt;CharacterSpawnSignal&gt;().Dispatch(7, "Boss", pos, rot);
    /// </code>
    /// </summary>
    /// <remarks>
    /// <para>本文件顶部的说明写着「最多支持 3 个参数」，实际已提供 4 个参数的重载，该注释是历史遗留。</para>
    /// <para>到 4 个参数时几乎一定应该改成传一个结构体，否则调用处完全无法自解释。</para>
    /// </remarks>
    /// <typeparam name="T">第一个参数类型</typeparam>
    /// <typeparam name="U">第二个参数类型</typeparam>
    /// <typeparam name="V">第三个参数类型</typeparam>
    /// <typeparam name="J">第四个参数类型</typeparam>
    public abstract class ASignal<T, U, V, J> : ABaseSignal
    {
        private Action<T, U, V, J> callback;

        /// <summary>
        /// Adds a listener to this Signal
        /// </summary>
        /// <param name="handler">Method to be called when signal is fired</param>
        public void AddListener(Action<T, U, V, J> handler)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Assert(
                handler.Method.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute),
                    inherit: false).Length == 0,
                "Adding anonymous delegates as Signal callbacks is not supported (you wouldn't be able to unregister them later).");
#endif
            callback += handler;
        }

        /// <summary>
        /// Removes a listener from this Signal
        /// </summary>
        /// <param name="handler">Method to be unregistered from signal</param>
        public void RemoveListener(Action<T, U, V, J> handler)
        {
            callback -= handler;
        }

        /// <summary>
        /// Dispatch this signal
        /// </summary>
        public void Dispatch(T arg1, U arg2, V arg3, J arg4)
        {
            if (callback != null)
            {
                callback(arg1, arg2, arg3, arg4);
            }
        }
    }
}
