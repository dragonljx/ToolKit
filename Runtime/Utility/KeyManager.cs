using System;
using System.Collections.Generic;
using UnityEngine;
// 条件编译：根据是否使用新Input System来引用不同的命名空间
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ToolKit
{
    // ============= 枚举定义保持不变 =============

    /// <summary>
    /// 按键的触发时机
    /// <para>决定回调在按键生命周期的哪个阶段被调用</para>
    /// </summary>
    public enum InputEventType
    {
        /// <summary>按下瞬间触发一次</summary>
        Down,
        /// <summary>抬起瞬间触发一次</summary>
        Up,
        /// <summary>按住期间持续触发，<strong>每帧都会调用</strong></summary>
        Hold
    }

    /// <summary>
    /// 组合键的修饰键，可按位组合使用
    /// <code>
    /// // 同时要求 Control 与 Shift
    /// ModifierKey combo = ModifierKey.Control | ModifierKey.Shift;
    /// </code>
    /// </summary>
    [Flags]
    public enum ModifierKey
    {
        /// <summary>无修饰键</summary>
        None = 0,
        /// <summary>Ctrl（左或右）</summary>
        Control = 1 << 0,
        /// <summary>Shift（左或右）</summary>
        Shift = 1 << 1,
        /// <summary>Alt（左或右）</summary>
        Alt = 1 << 2
    }

    /// <summary>
    /// 跨输入系统的按键枚举，屏蔽 Input Manager 与 Input System 的差异
    /// <para>用法：业务代码只引用 <c>InputKey</c>，切换输入系统时不需要改动</para>
    /// <code>
    /// InputManager.AddKeyDown(InputKey.F, OnPressF);
    /// InputManager.AddKeyDown(InputKey.Escape, OnCancel);
    /// </code>
    /// </summary>
    /// <remarks>
    /// <para>关于条件编译：枚举的底层取值随 <c>ENABLE_INPUT_SYSTEM</c> 切换 ——</para>
    /// <list type="bullet">
    /// <item>启用新输入系统时取值来自 <c>Key</c></item>
    /// <item>否则取值来自 <c>KeyCode</c></item>
    /// <item>两套取值<strong>数值并不一致</strong>，因此不要把本枚举强转成 int 后序列化或存档，
    /// 换输入系统后存档里的按键会全部错位</item>
    /// </list>
    /// <para>关于覆盖范围：</para>
    /// <list type="bullet">
    /// <item>目前只收录了 26 个字母键、<c>Space</c> / <c>Enter</c> / <c>Escape</c> 和数字键 0~9</item>
    /// <item>方向键、功能键（F1~F12）、小键盘等都没有收录，需要时得自行扩展枚举</item>
    /// <item>注意 <c>Enter</c> 指的是<strong>主键盘回车</strong>（旧输入系统下的 <c>KeyCode.Return</c>），
    /// 与新输入系统的 <c>Key.Enter</c> 语义一致；小键盘回车（<c>KeyCode.KeypadEnter</c>）没有收录</item>
    /// </list>
    /// </remarks>
    public enum InputKey
    {
#if ENABLE_INPUT_SYSTEM
        // 新Input System的Key枚举
        A = Key.A,
        B = Key.B,
        C = Key.C,
        E = Key.E,
        F = Key.F,
        G = Key.G,
        H = Key.H,
        I = Key.I,
        J = Key.J,
        K = Key.K,
        L = Key.L,
        M = Key.M,
        N = Key.N,
        O = Key.O,
        P = Key.P,
        Q = Key.Q,
        R = Key.R,
        T = Key.T,
        U = Key.U,
        V = Key.V,
        X = Key.X,
        Y = Key.Y,
        Z = Key.Z,
        W = Key.W,
        S = Key.S,
        D = Key.D,

        Space = Key.Space,
        Enter = Key.Enter,
        Escape = Key.Escape,

        Alpha1 = Key.Digit1,
        Alpha2 = Key.Digit2,
        Alpha3 = Key.Digit3,
        Alpha4 = Key.Digit4,
        Alpha5 = Key.Digit5,
        Alpha6 = Key.Digit6,
        Alpha7 = Key.Digit7,
        Alpha8 = Key.Digit8,
        Alpha9 = Key.Digit9,
        Alpha0 = Key.Digit0,
#else
        // 旧Input Manager的KeyCode
        A = KeyCode.A,
        B = KeyCode.B,
        C = KeyCode.C,
        E = KeyCode.E,
        F = KeyCode.F,
        G = KeyCode.G,
        H = KeyCode.H,
        I = KeyCode.I,
        J = KeyCode.J,
        K = KeyCode.K,
        L = KeyCode.L,
        M = KeyCode.M,
        N = KeyCode.N,
        O = KeyCode.O,
        P = KeyCode.P,
        Q = KeyCode.Q,
        R = KeyCode.R,
        T = KeyCode.T,
        U = KeyCode.U,
        V = KeyCode.V,
        X = KeyCode.X,
        Y = KeyCode.Y,
        Z = KeyCode.Z,
        W = KeyCode.W,
        S = KeyCode.S,
        D = KeyCode.D,

        Space = KeyCode.Space,
        Enter = KeyCode.Return,
        Escape = KeyCode.Escape,

        Alpha1 = KeyCode.Alpha1,
        Alpha2 = KeyCode.Alpha2,
        Alpha3 = KeyCode.Alpha3,
        Alpha4 = KeyCode.Alpha4,
        Alpha5 = KeyCode.Alpha5,
        Alpha6 = KeyCode.Alpha6,
        Alpha7 = KeyCode.Alpha7,
        Alpha8 = KeyCode.Alpha8,
        Alpha9 = KeyCode.Alpha9,
        Alpha0 = KeyCode.Alpha0,
#endif
    }

    // ============= 数据结构保持不变 =============

    /// <summary>
    /// 单键绑定的操作句柄，用于解绑
    /// <para>用法：调用 <c>AddKey</c> / <c>AddKeyDown</c> / <c>AddKeyUp</c> 时返回</para>
    /// <code>
    /// private InputBindingHandle handle;
    ///
    /// void OnEnable()
    /// {
    ///     handle = InputManager.AddKeyDown(InputKey.F, OnPressF);
    /// }
    ///
    /// void OnDisable()
    /// {
    ///     // 必须用 RemoveKey 解绑，不能只调 Dispose
    ///     InputManager.RemoveKey(handle);
    /// }
    /// </code>
    /// </summary>
    /// <remarks>
    /// <para>关于 <see cref="Dispose"/> 的陷阱（重要）：</para>
    /// <list type="bullet">
    /// <item>在启用新输入系统时，<see cref="Dispose"/> 只会禁用并释放底层的 <c>InputAction</c>，
    /// <strong>不会把绑定从管理器的列表中移除</strong></item>
    /// <item>在旧输入系统下，<see cref="Dispose"/> 是<strong>完全的空操作</strong>，调用它不会解绑任何东西</item>
    /// <item>因此解绑请统一使用 <c>InputManager.RemoveKey(handle)</c>，不要依赖 <c>Dispose</c> 或 <c>using</c> 块</item>
    /// </list>
    /// <para>句柄本身不持有需要手动释放的资源，让它被 GC 回收即可。</para>
    /// </remarks>
    public class InputBindingHandle : IDisposable
    {
        /// <summary>
        /// 该绑定对应的按键与触发时机，解绑时用它定位
        /// </summary>
        public readonly InputEventKey Key;

        /// <summary>
        /// 触发时要执行的委托
        /// </summary>
        public readonly Action Action;

#if ENABLE_INPUT_SYSTEM
        /// <summary>
        /// 新输入系统下的底层动作对象，由管理器在创建时赋值
        /// <para>仅在启用 <c>ENABLE_INPUT_SYSTEM</c> 时存在</para>
        /// </summary>
        public InputAction InputAction { get; set; }
#endif

        /// <summary>
        /// 创建一个绑定句柄
        /// <para>由 <c>InputManager</c> 内部调用，业务代码不需要自己构造</para>
        /// </summary>
        /// <param name="key">绑定的按键与触发时机</param>
        /// <param name="action">触发时执行的回调</param>
        public InputBindingHandle(InputEventKey key, Action action)
        {
            Key = key;
            Action = action;
        }

        /// <summary>
        /// 释放新输入系统下的底层动作对象
        /// <para><strong>不解绑</strong>，解绑请用 <c>InputManager.RemoveKey</c></para>
        /// </summary>
        public void Dispose()
        {
#if ENABLE_INPUT_SYSTEM
            InputAction?.Disable();
            InputAction?.Dispose();
#endif
        }
    }

    /// <summary>
    /// 组合键绑定的操作句柄，用于解绑
    /// <para>用法：调用 <c>AddCombinationKey</c> 时返回，解绑用 <c>RemoveCombinationKey</c></para>
    /// <code>
    /// private CombinationBindingHandle handle;
    ///
    /// void OnEnable()
    /// {
    ///     handle = InputManager.AddCombinationKey(
    ///         ModifierKey.Control | ModifierKey.Shift,
    ///         InputKey.F,
    ///         OnSaveAll);
    /// }
    ///
    /// void OnDisable()
    /// {
    ///     InputManager.RemoveCombinationKey(handle);
    /// }
    /// </code>
    /// </summary>
    /// <remarks>
    /// <para>关于 <c>AddCombinationKey</c> 返回 null：启用新输入系统且修饰键超过 2 个时，</para>
    /// <para>创建会失败并返回 null，此时直接传给 <c>RemoveCombinationKey</c> 会抛 <c>NullReferenceException</c>，
    /// 调用方需要判空。</para>
    /// <para>与 <see cref="InputBindingHandle.Dispose"/> 相同，本类的 <c>Dispose</c> 只释放底层动作对象，不解绑。</para>
    /// </remarks>
    public class CombinationBindingHandle : IDisposable
    {
        /// <summary>
        /// 该绑定对应的修饰键与主键组合，解绑时用它定位
        /// </summary>
        public KeyCombination Key;

        /// <summary>
        /// 组合键触发时要执行的委托
        /// </summary>
        public readonly Action Action;

#if ENABLE_INPUT_SYSTEM
        /// <summary>
        /// 新输入系统下的底层动作对象，由管理器在创建时赋值
        /// <para>仅在启用 <c>ENABLE_INPUT_SYSTEM</c> 时存在</para>
        /// </summary>
        public InputAction InputAction { get; set; }
#endif

        /// <summary>
        /// 创建一个组合键绑定句柄
        /// <para>由 <c>InputManager</c> 内部调用，业务代码不需要自己构造</para>
        /// </summary>
        /// <param name="key">修饰键与主键的组合</param>
        /// <param name="action">触发时执行的回调</param>
        public CombinationBindingHandle(KeyCombination key, Action action)
        {
            Key = key;
            Action = action;
        }

        /// <summary>
        /// 释放新输入系统下的底层动作对象
        /// <para><strong>不解绑</strong>，解绑请用 <c>InputManager.RemoveCombinationKey</c></para>
        /// </summary>
        public void Dispose()
        {
#if ENABLE_INPUT_SYSTEM
            InputAction?.Disable();
            InputAction?.Dispose();
#endif
        }
    }

    /// <summary>
    /// 单键绑定的键：按键 + 触发时机
    /// <para>用法：作为绑定字典的键，也可以在遍历绑定表时用来识别是哪个绑定</para>
    /// <para>已实现值相等语义，可以直接作为字典键或放进集合做比较</para>
    /// </summary>
    /// <remarks>
    /// <para>关于字段类型随条件编译变化：启用新输入系统时 <c>Key</c> 是 <c>Key</c> 类型，</para>
    /// <para>否则是 <c>KeyCode</c>。跨两种模式共享的代码不要直接访问该字段。</para>
    /// </remarks>
    public readonly struct InputEventKey : IEquatable<InputEventKey>
    {
#if ENABLE_INPUT_SYSTEM
        /// <summary>底层按键值（新输入系统的 <c>Key</c>）</summary>
        public readonly Key Key;
#else
        /// <summary>底层按键值（旧输入系统的 <c>KeyCode</c>）</summary>
        public readonly KeyCode Key;
#endif
        /// <summary>触发时机</summary>
        public readonly InputEventType EventType;

        /// <summary>
        /// 创建一个单键绑定标识
        /// </summary>
        /// <param name="key">底层按键值</param>
        /// <param name="eventType">触发时机</param>
#if ENABLE_INPUT_SYSTEM
        public InputEventKey(Key key, InputEventType eventType)
#else
        public InputEventKey(KeyCode key, InputEventType eventType)
#endif
        {
            Key = key;
            EventType = eventType;
        }

        /// <summary>
        /// 按键与触发时机都相同才视为相等
        /// </summary>
        /// <param name="other">要比较的另一个键</param>
        /// <returns>两者相等返回 true</returns>
        public bool Equals(InputEventKey other)
        {
            return Key == other.Key && EventType == other.EventType;
        }

        /// <summary>
        /// 与任意对象比较，类型不符时返回 false
        /// </summary>
        /// <param name="obj">要比较的对象</param>
        /// <returns>类型匹配且内容相同时返回 true</returns>
        public override bool Equals(object obj)
        {
            return obj is InputEventKey other && Equals(other);
        }

        /// <summary>
        /// 由按键与触发时机组合出哈希值，可直接作为字典键使用
        /// </summary>
        /// <returns>该键的哈希值</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Key, EventType);
        }

        /// <summary>
        /// 输出形如 <c>F + Down</c> 的可读文本，用于日志排查
        /// </summary>
        /// <returns>按键与触发时机的组合文本</returns>
        public override string ToString()
        {
            return $"{Key} + {EventType}";
        }

        /// <summary>
        /// 相等运算符，等价于调用 <see cref="Equals(InputEventKey)"/>
        /// </summary>
        /// <param name="left">左操作数</param>
        /// <param name="right">右操作数</param>
        /// <returns>两者相等返回 true</returns>
        public static bool operator ==(InputEventKey left, InputEventKey right)
            => left.Equals(right);

        /// <summary>
        /// 不等运算符，等价于对 <see cref="Equals(InputEventKey)"/> 取反
        /// </summary>
        /// <param name="left">左操作数</param>
        /// <param name="right">右操作数</param>
        /// <returns>两者不相等返回 true</returns>
        public static bool operator !=(InputEventKey left, InputEventKey right)
            => !left.Equals(right);
    }

    /// <summary>
    /// 组合键标识：修饰键组合 + 主键
    /// <para>用法：作为组合键绑定字典的键，或用于标识某次组合键触发</para>
    /// <para>同样实现了值相等语义，可直接作为字典键</para>
    /// </summary>
    public readonly struct KeyCombination : IEquatable<KeyCombination>
    {
        /// <summary>需要的修饰键，按位组合</summary>
        public readonly ModifierKey Modifiers;

        /// <summary>主键</summary>
        public readonly InputKey PrimaryKey;

        /// <summary>
        /// 创建一个组合键标识
        /// </summary>
        /// <param name="modifiers">修饰键组合，如 <c>ModifierKey.Control | ModifierKey.Shift</c></param>
        /// <param name="primaryKey">主键</param>
        public KeyCombination(ModifierKey modifiers, InputKey primaryKey)
        {
            PrimaryKey = primaryKey;
            Modifiers = modifiers;
        }

        /// <summary>
        /// 主键与修饰键都相同才视为相等
        /// </summary>
        /// <param name="other">要比较的另一个组合键</param>
        /// <returns>两者相等返回 true</returns>
        public bool Equals(KeyCombination other)
        {
            return PrimaryKey == other.PrimaryKey && Modifiers == other.Modifiers;
        }

        /// <summary>
        /// 与任意对象比较，类型不符时返回 false
        /// </summary>
        /// <param name="obj">要比较的对象</param>
        /// <returns>类型匹配且内容相同时返回 true</returns>
        public override bool Equals(object obj)
        {
            return obj is KeyCombination other && Equals(other);
        }

        /// <summary>
        /// 由主键与修饰键组合出哈希值，可直接作为字典键使用
        /// </summary>
        /// <returns>该组合键的哈希值</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(PrimaryKey, Modifiers);
        }

        /// <summary>
        /// 输出形如 <c>Control, Shift + F</c> 的可读文本，用于日志排查
        /// </summary>
        /// <returns>修饰键与主键的组合文本</returns>
        public override string ToString()
        {
            return $"{Modifiers} + {PrimaryKey}";
        }

        /// <summary>
        /// 相等运算符，等价于调用 <see cref="Equals(KeyCombination)"/>
        /// </summary>
        /// <param name="left">左操作数</param>
        /// <param name="right">右操作数</param>
        /// <returns>两者相等返回 true</returns>
        public static bool operator ==(KeyCombination left, KeyCombination right)
            => left.Equals(right);

        /// <summary>
        /// 不等运算符，等价于对 <see cref="Equals(KeyCombination)"/> 取反
        /// </summary>
        /// <param name="left">左操作数</param>
        /// <param name="right">右操作数</param>
        /// <returns>两者不相等返回 true</returns>
        public static bool operator !=(KeyCombination left, KeyCombination right)
            => !left.Equals(right);
    }

    // ============= InputManager 主类 =============

    /// <summary>
    /// 跨输入系统的按键管理器，用同一套 API 同时支持旧 Input Manager 与新 Input System
    /// <para>用法：静态方法注册与解绑，成对调用；管理器单例会自动创建</para>
    /// <code>
    /// // 单键：按下 / 抬起 / 按住
    /// InputBindingHandle down = InputManager.AddKeyDown(InputKey.F, () => DLogger.Log("按下 F"));
    /// InputBindingHandle up = InputManager.AddKeyUp(InputKey.F, () => DLogger.Log("抬起 F"));
    /// InputBindingHandle hold = InputManager.AddKey(InputKey.W, OnMove); // 每帧触发
    ///
    /// // 组合键：Ctrl + Shift + F
    /// CombinationBindingHandle combo = InputManager.AddCombinationKey(
    ///     ModifierKey.Control | ModifierKey.Shift,
    ///     InputKey.F,
    ///     () => DLogger.Log("Ctrl + Shift + F")
    /// );
    ///
    /// // 解绑：务必成对调用，否则对象销毁后回调仍会触发
    /// InputManager.RemoveKey(down);
    /// InputManager.RemoveKey(up);
    /// InputManager.RemoveKey(hold);
    /// InputManager.RemoveCombinationKey(combo);
    ///
    /// // 查询当前按下的修饰键
    /// ModifierKey current = InputManager.GetCurrentModifiers();
    /// </code>
    /// <para>两种输入系统的差异全部封装在条件编译里，业务代码只引用 <see cref="InputKey"/> 与
    /// <see cref="ModifierKey"/>，切换时不需要改动</para>
    /// </summary>
    /// <remarks>
    /// <para>关于解绑（最容易踩的坑）：</para>
    /// <list type="bullet">
    /// <item>必须调用 <c>RemoveKey</c> / <c>RemoveCombinationKey</c>，
    /// <strong>句柄上的 <c>Dispose</c> 在旧输入系统下是空操作，在新输入系统下也只释放底层对象而不解绑</strong></item>
    /// <item>忘记解绑会让回调持有对已销毁对象的引用，表现为「对象已销毁但方法还在被调用」</item>
    /// <item>所有添加方法都会触发单例创建，因此必须在主线程调用</item>
    /// </list>
    /// <para>关于组合键的修饰键匹配：判断条件是「当前按下的修饰键<strong>包含</strong>绑定要求的修饰键」，
    /// 而不是「完全相等」。</para>
    /// <para>因此绑定了 <c>Ctrl+F</c> 之后，按下 <c>Ctrl+Shift+F</c> 也会<strong>同时</strong>触发该绑定，
    /// 需要互斥的组合请自行在回调里判断 <c>GetCurrentModifiers()</c>。</para>
    /// <para>关于 <c>Hold</c> 的触发频率：两种输入系统下都是<strong>每帧触发一次</strong>，
    /// 而不是「按住后只触发一次」。因为它会持续占用回调，回调内请避免做重活。</para>
    /// <para>关于在回调中增删绑定（旧输入系统）：<c>Update</c> 会直接遍历绑定字典，
    /// 回调里调用 <c>AddKey</c> / <c>RemoveKey</c> 会改动正在遍历的集合，导致 <c>InvalidOperationException</c>。
    /// 需要动态改绑时请延迟到下一帧执行。新输入系统走的是事件回调，不受此限制。</para>
    /// <para>关于重复绑定：旧输入系统下会检测到同一个回调被重复添加，记录警告并忽略；
    /// 新输入系统下不做去重，会创建出多个 <c>InputAction</c>，回调会被调用多次。</para>
    /// </remarks>
    public class InputManager : ToolKit.SingletonMono<InputManager>
    {
#if ENABLE_INPUT_SYSTEM
        // 新Input System使用InputAction字典
        private readonly Dictionary<InputEventKey, List<InputAction>> _keyBindings = new();
        private readonly Dictionary<KeyCombination, List<InputAction>> _combinationKeyDownDic = new();
#else
        // 旧Input Manager使用Action回调字典
        private readonly Dictionary<InputEventKey, List<Action>> _keyBindings = new();
        private readonly Dictionary<KeyCombination, List<Action>> _combinationKeyDownDic = new();
#endif

        // ============= 添加单键 =============

        /// <summary>
        /// 绑定「按住」事件，回调在按住期间每帧触发
        /// <code>
        /// // 持续移动
        /// InputManager.AddKey(InputKey.W, () => Move(Vector3.forward * Time.deltaTime));
        /// </code>
        /// </summary>
        /// <param name="key">要监听的按键</param>
        /// <param name="action">每次触发时执行的回调</param>
        /// <returns>用于解绑的操作句柄，配合 <see cref="RemoveKey"/> 使用</returns>
        public static InputBindingHandle AddKey(InputKey key,  Action action)
        {
#if ENABLE_INPUT_SYSTEM
            return AddKeyNew(key, InputEventType.Hold, action);
#else
            return AddKeyOld(key, InputEventType.Hold, action);
#endif
        }

        /// <summary>
        /// 绑定「按下」事件，回调在按下的那一帧触发一次
        /// <code>
        /// InputManager.AddKeyDown(InputKey.F, () => Interact());
        /// </code>
        /// </summary>
        /// <param name="key">要监听的按键</param>
        /// <param name="action">按下时要执行的回调</param>
        /// <returns>用于解绑的操作句柄，配合 <see cref="RemoveKey"/> 使用</returns>
        public static InputBindingHandle AddKeyDown(InputKey key,  Action action)
        {
#if ENABLE_INPUT_SYSTEM
            return AddKeyNew(key, InputEventType.Down, action);
#else
            return AddKeyOld(key, InputEventType.Down, action);
#endif
        }

        /// <summary>
        /// 绑定「抬起」事件，回调在松开的那一帧触发一次
        /// <code>
        /// InputManager.AddKeyUp(InputKey.Space, () => ReleaseJump());
        /// </code>
        /// </summary>
        /// <param name="key">要监听的按键</param>
        /// <param name="action">抬起时要执行的回调</param>
        /// <returns>用于解绑的操作句柄，配合 <see cref="RemoveKey"/> 使用</returns>
        public static InputBindingHandle AddKeyUp(InputKey key, Action action)
        {
#if ENABLE_INPUT_SYSTEM
            return AddKeyNew(key, InputEventType.Up, action);
#else
            return AddKeyOld(key, InputEventType.Up, action);
#endif
        }

#if ENABLE_INPUT_SYSTEM
        /// <summary>
        /// 新输入系统下的单键绑定：为每次绑定创建一个独立的 <c>InputAction</c> 并启用
        /// </summary>
        /// <param name="key">要监听的按键</param>
        /// <param name="type">触发时机</param>
        /// <param name="action">触发时执行的回调</param>
        /// <returns>包装了底层 <c>InputAction</c> 的操作句柄</returns>
        /// <remarks>
        /// <para>触发时机的映射关系：<c>Down</c> → <c>started</c>，<c>Up</c> → <c>canceled</c>，
        /// <c>Hold</c> → <c>performed</c>。</para>
        /// <para><c>Hold</c> 使用 <c>PassThrough</c> 类型，因此按住时每帧都会触发 <c>performed</c>。</para>
        /// <para>本方法不做重复检测，同一个回调绑定多次就会创建多个动作，回调相应地会被调用多次。</para>
        /// <para>每次绑定都会输出一条 <c>Debug.Log</c>，绑定数量多时注意 Console 噪音。</para>
        /// </remarks>
        private static InputBindingHandle AddKeyNew(InputKey key, InputEventType type, Action action)
        {
            Key unityKey = (Key)key;
            InputEventKey code = new InputEventKey(unityKey, type);

            if (!Instance._keyBindings.TryGetValue(code, out var inputActionList))
            {
                inputActionList = new List<InputAction>();
                Instance._keyBindings[code] = inputActionList;
            }

            // 创建InputAction
            var inputAction = new InputAction(
                name: $"{key}_{type}",
                type: type == InputEventType.Hold ? InputActionType.PassThrough : InputActionType.Button,
                binding: $"<Keyboard>/{unityKey}"
            );

            // 添加交互类型
            switch (type)
            {
                case InputEventType.Down:
                    inputAction.started += ctx => action?.Invoke();
                    break;
                case InputEventType.Up:
                    inputAction.canceled += ctx => action?.Invoke();
                    break;
                case InputEventType.Hold:
                    inputAction.performed += ctx => action?.Invoke();
                    break;
            }

            inputAction.Enable();
            inputActionList.Add(inputAction);

            var handle = new InputBindingHandle(code, action);
            handle.InputAction = inputAction;

            Debug.Log($"[新InputSystem][{type}] {code} 添加");
            return handle;
        }
#else
        /// <summary>
        /// 旧输入系统下的单键绑定：只把回调登记进字典，实际轮询在 <c>Update</c> 中进行
        /// </summary>
        /// <param name="key">要监听的按键</param>
        /// <param name="type">触发时机</param>
        /// <param name="action">触发时执行的回调</param>
        /// <returns>操作句柄；重复绑定时返回的句柄未真正登记，调用 <see cref="RemoveKey"/> 无副作用</returns>
        /// <remarks>
        /// <para>同一个按键与触发时机下重复登记同一个回调时，会记录警告并忽略本次登记。</para>
        /// <para>不同回调绑定到同一个键是允许的，它们会被依次调用。</para>
        /// </remarks>
        private static InputBindingHandle AddKeyOld(InputKey key, InputEventType type, Action action)
        {
            InputEventKey code = new InputEventKey((KeyCode)key, type);

            if (!Instance._keyBindings.TryGetValue(code, out var list))
            {
                list = new List<Action>();
                Instance._keyBindings[code] = list;
            }

            if (list.Contains(action))
            {
                Debug.LogWarning($"重复绑定忽略: [{code}] {type}");
                return new InputBindingHandle(code, action);
            }

            list.Add(action);
            Debug.Log($"[旧InputManager][{type}] {code} 添加");
            return new InputBindingHandle(code, action);
        }
#endif

        // ============= 移除单键 =============

        /// <summary>
        /// 解除一个单键绑定
        /// <code>
        /// void OnDisable()
        /// {
        ///     InputManager.RemoveKey(handle);
        /// }
        /// </code>
        /// </summary>
        /// <param name="handle">添加时返回的句柄，传 null 会抛 <c>NullReferenceException</c></param>
        /// <remarks>
        /// <para>绑定已不存在时是安全的空操作，可重复调用。</para>
        /// <para>新输入系统下按「键名」查找要移除的动作，若同一个键与时机绑定了多个回调，
        /// 只会移除其中<strong>一个</strong>，且未必是句柄对应的那一个。避免重复绑定可以规避这个问题。</para>
        /// </remarks>
        public static void RemoveKey(InputBindingHandle handle)
        {
#if ENABLE_INPUT_SYSTEM
            RemoveKeyNew(handle);
#else
            RemoveKeyOld(handle);
#endif
        }

#if ENABLE_INPUT_SYSTEM
        /// <summary>
        /// 新输入系统下的解绑：从列表中找到同名动作并禁用、释放
        /// </summary>
        /// <param name="handle">添加时返回的句柄</param>
        private static void RemoveKeyNew(InputBindingHandle handle)
        {
            if (!Instance._keyBindings.TryGetValue(handle.Key, out var list))
                return;

            var inputAction = list.Find(a => a.name == $"{handle.Key.Key}_{handle.Key.EventType}");
            if (inputAction != null)
            {
                list.Remove(inputAction);
                inputAction.Disable();
                inputAction.Dispose();
                Debug.Log($"[新InputSystem][{handle.Key}] {handle.Action} 移除");
            }
        }
#else
        /// <summary>
        /// 旧输入系统下的解绑：从回调列表中移除对应委托
        /// </summary>
        /// <param name="handle">添加时返回的句柄</param>
        private static void RemoveKeyOld(InputBindingHandle handle)
        {
            if (!Instance._keyBindings.TryGetValue(handle.Key, out var list))
                return;

            if (!list.Contains(handle.Action))
                return;

            list.Remove(handle.Action);
            Debug.Log($"[旧InputManager][{handle.Key}] {handle.Action} 移除");
        }
#endif

        // ============= 添加组合键 =============

        /// <summary>
        /// 绑定组合键的按下事件
        /// <code>
        /// // 单个修饰键
        /// var save = InputManager.AddCombinationKey(ModifierKey.Control, InputKey.S, SaveAll);
        ///
        /// // 两个修饰键
        /// var saveAs = InputManager.AddCombinationKey(
        ///     ModifierKey.Control | ModifierKey.Shift, InputKey.S, SaveAs);
        ///
        /// // 不传修饰键时等价于普通单键的按下事件
        /// var plain = InputManager.AddCombinationKey(ModifierKey.None, InputKey.S, Save);
        /// </code>
        /// </summary>
        /// <param name="modifierKey">要求的修饰键组合；传 <see cref="ModifierKey.None"/> 时退化为单键</param>
        /// <param name="primaryKey">主键</param>
        /// <param name="action">触发时执行的回调</param>
        /// <returns>用于解绑的操作句柄；新输入系统下修饰键超过 2 个时返回 <strong>null</strong></returns>
        /// <remarks>
        /// <para>修饰键最多支持 2 个（受新输入系统的 <c>ButtonWithTwoModifiers</c> 组合器限制），
        /// 超过时创建失败并返回 null，调用方必须判空。</para>
        /// <para>无论如何都只监听「按下」时机，没有对应的抬起或按住版本。</para>
        /// </remarks>
        public static CombinationBindingHandle AddCombinationKey(ModifierKey modifierKey, InputKey primaryKey,
            Action action)
        {
#if ENABLE_INPUT_SYSTEM
            return AddCombinationKeyNew(modifierKey, primaryKey, action);
#else
            return AddCombinationKeyOld(modifierKey, primaryKey, action);
#endif
        }

#if ENABLE_INPUT_SYSTEM
        /// <summary>
        /// 新输入系统下的组合键绑定：按修饰键数量选择直连、<c>ButtonWithOneModifier</c> 或
        /// <c>ButtonWithTwoModifiers</c> 组合器
        /// </summary>
        /// <param name="modifierKey">要求的修饰键组合</param>
        /// <param name="primaryKey">主键</param>
        /// <param name="action">触发时执行的回调</param>
        /// <returns>操作句柄；修饰键超过 2 个时返回 null</returns>
        private static CombinationBindingHandle AddCombinationKeyNew(
            ModifierKey modifierKey, InputKey primaryKey, Action action)
        {
            KeyCombination codeCombination = new KeyCombination(modifierKey, primaryKey);

            if (!Instance._combinationKeyDownDic.TryGetValue(codeCombination, out var inputActionList))
            {
                inputActionList = new List<InputAction>();
                Instance._combinationKeyDownDic[codeCombination] = inputActionList;
            }

            var inputAction = new InputAction(
                name: $"Combo_{modifierKey}_{primaryKey}",
                type: InputActionType.Button
            );

            // 收集需要的修饰键
            var modifiers = new List<string>();
            if ((modifierKey & ModifierKey.Control) != 0)
                modifiers.Add("ctrl");
            if ((modifierKey & ModifierKey.Shift) != 0)
                modifiers.Add("shift");
            if ((modifierKey & ModifierKey.Alt) != 0)
                modifiers.Add("alt");

            string primaryKeyStr = primaryKey.ToString().ToLower();

            // 根据修饰键数量选择不同的组合模式
            switch (modifiers.Count)
            {
                case 0:
                    // 没有修饰键，就是单键
                    inputAction.AddBinding($"<Keyboard>/{primaryKeyStr}");
                    break;

                case 1:
                    // 一个修饰键：使用 ButtonWithOneModifier
                    inputAction.AddCompositeBinding("ButtonWithOneModifier")
                        .With("modifier", $"<Keyboard>/{modifiers[0]}")
                        .With("button", $"<Keyboard>/{primaryKeyStr}");
                    break;

                case 2:
                    // 两个修饰键：使用 ButtonWithTwoModifiers
                    inputAction.AddCompositeBinding("ButtonWithTwoModifiers")
                        .With("modifier1", $"<Keyboard>/{modifiers[0]}")
                        .With("modifier2", $"<Keyboard>/{modifiers[1]}")
                        .With("button", $"<Keyboard>/{primaryKeyStr}");
                    break;

                default:

                    inputAction.Disable();
                    inputAction.Dispose();
                    Debug.LogWarning($"[InputManager] 不支持超过2修饰键");
                    return null;
            }


            inputAction.started += _ => action?.Invoke();
            inputAction.Enable();
            inputActionList.Add(inputAction);

            var handle = new CombinationBindingHandle(codeCombination, action);
            handle.InputAction = inputAction;

            Debug.Log($"[InputManager] 添加组合键: {modifierKey} + {primaryKey}");
            return handle;
        }
#else
        /// <summary>
        /// 旧输入系统下的组合键绑定：登记回调，实际匹配在 <c>ProcessCombinationKeys</c> 中逐帧进行
        /// </summary>
        /// <param name="modifierKey">要求的修饰键组合</param>
        /// <param name="primaryKey">主键</param>
        /// <param name="action">触发时执行的回调</param>
        /// <returns>操作句柄；重复绑定时返回的句柄未真正登记</returns>
        /// <remarks>
        /// <para>与单键不同，重复绑定这里只记录一条普通日志，不会给出警告级别的提示。</para>
        /// <para>旧输入系统下没有修饰键数量限制，因为匹配逻辑是自己实现的。</para>
        /// </remarks>
        private static CombinationBindingHandle AddCombinationKeyOld(ModifierKey modifierKey, InputKey primaryKey, Action action)
        {
            KeyCombination codeCombination = new KeyCombination(modifierKey, primaryKey);

            if (!Instance._combinationKeyDownDic.TryGetValue(codeCombination, out var list))
            {
                list = new List<Action>();
                Instance._combinationKeyDownDic[codeCombination] = list;
            }

            if (list.Contains(action))
            {
                Debug.Log($"[旧InputManager][{codeCombination}] 重复添加");
            }
            else
            {
                list.Add(action);
            }

            Debug.Log($"[旧InputManager][{codeCombination}] 添加");
            return new CombinationBindingHandle(codeCombination, action);
        }
#endif

        // ============= 移除组合键 =============

        /// <summary>
        /// 解除一个组合键绑定
        /// <code>
        /// void OnDisable()
        /// {
        ///     InputManager.RemoveCombinationKey(handle);
        /// }
        /// </code>
        /// </summary>
        /// <param name="handle">添加时返回的句柄；若添加时因修饰键过多返回了 null，这里会抛 <c>NullReferenceException</c></param>
        /// <remarks>
        /// <para>调用前请先判空，尤其是修饰键可能超过 2 个的场景。</para>
        /// <para>绑定已不存在时是安全的空操作。</para>
        /// </remarks>
        public static void RemoveCombinationKey(CombinationBindingHandle handle)
        {
#if ENABLE_INPUT_SYSTEM
            RemoveCombinationKeyNew(handle);
#else
            RemoveCombinationKeyOld(handle);
#endif
        }

#if ENABLE_INPUT_SYSTEM
        /// <summary>
        /// 新输入系统下的组合键解绑：按动作名查找并禁用、释放
        /// </summary>
        /// <param name="handle">添加时返回的句柄，不可为 null</param>
        private static void RemoveCombinationKeyNew(CombinationBindingHandle handle)
        {
            if (!Instance._combinationKeyDownDic.TryGetValue(handle.Key, out var list))
                return;

            var inputAction = list.Find(a =>
                a.name == $"Combo_{handle.Key.Modifiers}_{handle.Key.PrimaryKey}");

            if (inputAction != null)
            {
                list.Remove(inputAction);
                inputAction.Disable();
                inputAction.Dispose();
                Debug.Log($"[新InputSystem][{handle.Key}] {handle.Action} 移除");
            }
        }
#else
        /// <summary>
        /// 旧输入系统下的组合键解绑：从回调列表中移除对应委托
        /// </summary>
        /// <param name="handle">添加时返回的句柄，不可为 null</param>
        private static void RemoveCombinationKeyOld(CombinationBindingHandle handle)
        {
            if (!Instance._combinationKeyDownDic.TryGetValue(handle.Key, out var eventMap))
                return;

            if (!eventMap.Contains(handle.Action))
                return;

            eventMap.Remove(handle.Action);
            Debug.Log($"[旧InputManager][{handle.Key}] {handle.Action} 移除");
        }
#endif

        // ============= 获取当前修饰键 =============

        /// <summary>
        /// 查询当前正在按下的修饰键
        /// <code>
        /// // 某些功能只在没按修饰键时才响应，避免与快捷键冲突
        /// if (InputManager.GetCurrentModifiers() == ModifierKey.None)
        /// {
        ///     OpenInventory();
        /// }
        /// </code>
        /// </summary>
        /// <returns>当前按下的修饰键组合，没有任何修饰键按下时返回 <see cref="ModifierKey.None"/></returns>
        /// <remarks>
        /// <para>左右两侧的 Ctrl / Shift / Alt 都会被识别为对应的修饰键。</para>
        /// <para>新输入系统下键盘不存在时（如纯触屏设备）直接返回 <c>None</c>。</para>
        /// </remarks>
        public static ModifierKey GetCurrentModifiers()
        {
#if ENABLE_INPUT_SYSTEM
            return GetCurrentModifiersNew();
#else
            return GetCurrentModifiersOld();
#endif
        }

#if ENABLE_INPUT_SYSTEM
        /// <summary>
        /// 新输入系统下的修饰键查询：读 <c>Keyboard.current</c> 的修饰键按下状态
        /// </summary>
        /// <returns>当前修饰键组合</returns>
        private static ModifierKey GetCurrentModifiersNew()
        {
            ModifierKey m = ModifierKey.None;
            var keyboard = Keyboard.current;

            if (keyboard == null) return m;

            if (keyboard.ctrlKey.isPressed)
                m |= ModifierKey.Control;
            if (keyboard.shiftKey.isPressed)
                m |= ModifierKey.Shift;
            if (keyboard.altKey.isPressed)
                m |= ModifierKey.Alt;

            return m;
        }
#else
        /// <summary>
        /// 旧输入系统下的修饰键查询：左右两侧的按键都检查一遍
        /// </summary>
        /// <returns>当前修饰键组合</returns>
        private static ModifierKey GetCurrentModifiersOld()
        {
            ModifierKey m = ModifierKey.None;

            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                m |= ModifierKey.Control;

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                m |= ModifierKey.Shift;

            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                m |= ModifierKey.Alt;

            return m;
        }
#endif

        // ============= 清理 =============

        /// <summary>
        /// 清空所有按键绑定，并释放新输入系统下的 <c>InputAction</c>
        /// <code>
        /// // 切换关卡前统一清理，避免上一关的回调残留
        /// InputManager.Clear();
        /// </code>
        /// </summary>
        /// <remarks>
        /// <para>是全局清理，会一次性移除所有使用方注册的绑定，请在确认没有其他人还在依赖按键时调用。</para>
        /// <para>管理器自身被销毁时也会自动调用一次。</para>
        /// </remarks>
        public static void Clear()
        {
#if ENABLE_INPUT_SYSTEM
            ClearNew();
#else
            ClearOld();
#endif
        }

#if ENABLE_INPUT_SYSTEM
        /// <summary>
        /// 新输入系统下的清理：逐个禁用并释放动作，再清空字典
        /// </summary>
        private static void ClearNew()
        {
            foreach (var list in Instance._combinationKeyDownDic.Values)
            {
                foreach (var action in list)
                {
                    action.Disable();
                    action.Dispose();
                }

                list.Clear();
            }

            Instance._combinationKeyDownDic.Clear();

            foreach (var list in Instance._keyBindings.Values)
            {
                foreach (var action in list)
                {
                    action.Disable();
                    action.Dispose();
                }

                list.Clear();
            }

            Instance._keyBindings.Clear();
        }
#else
        /// <summary>
        /// 旧输入系统下的清理：直接清空两个回调字典
        /// </summary>
        private static void ClearOld()
        {
            Instance._combinationKeyDownDic.Clear();
            Instance._keyBindings.Clear();
        }
#endif

        /// <summary>
        /// 销毁时清理所有绑定
        /// <para>注意本重写<strong>没有调用 <c>base.OnDestroy()</c></strong>，因此基类的单例引用不会被置空，
        /// 销毁后 <c>Instance</c> 仍指向已销毁的对象</para>
        /// </summary>
        protected override void OnDestroy()
        {
            Clear();
        }

        // ============= Update循环 =============
#if !ENABLE_INPUT_SYSTEM
        // 旧版Input Manager需要使用Update轮询
        /// <summary>
        /// 旧输入系统下的轮询入口，每帧检查键盘状态并派发回调
        /// <para>启用新输入系统时整个方法块被条件编译剔除，改为事件回调驱动</para>
        /// </summary>
        void Update()
        {
            ProcessKeyBindings();
            ProcessCombinationKeys();
        }

        /// <summary>
        /// 遍历单键绑定表，按各自的触发时机检查按键状态并调用回调
        /// </summary>
        /// <remarks>
        /// <para>直接遍历字典，因此回调中调用 <c>AddKey</c> / <c>RemoveKey</c> 会改动正在遍历的集合，
        /// 抛 <c>InvalidOperationException</c>。需要动态改绑请延迟到下一帧。</para>
        /// <para>字典的遍历顺序不保证与登记顺序一致，多个回调之间没有固定的执行先后。</para>
        /// </remarks>
        private void ProcessKeyBindings()
        {
            foreach (var keyPair in _keyBindings)
            {
                var key = keyPair.Key;
                var list = keyPair.Value;
                switch (key.EventType)
                {
                    case InputEventType.Down:
                        if (Input.GetKeyDown(key.Key))
                            InvokeActions(list);
                        break;
                    case InputEventType.Up:
                        if (Input.GetKeyUp(key.Key))
                            InvokeActions(list);
                        break;
                    case InputEventType.Hold:
                        if (Input.GetKey(key.Key))
                            InvokeActions(list);
                        break;
                }
            }
        }

        /// <summary>
        /// 遍历组合键绑定表，匹配当前修饰键状态与主键按下事件并调用回调
        /// </summary>
        /// <remarks>
        /// <para>修饰键匹配规则是「当前按下的包含要求的」，允许按住多余的修饰键，
        /// 因此 <c>Ctrl+F</c> 的绑定会被 <c>Ctrl+Shift+F</c> 一并触发。</para>
        /// <para>没有任何修饰键按下时整段逻辑会被跳过，<c>ModifierKey.None</c> 的绑定因此不会生效。</para>
        /// <para>与 <see cref="ProcessKeyBindings"/> 相同，回调中改绑会破坏遍历。</para>
        /// </remarks>
        private void ProcessCombinationKeys()
        {
            ModifierKey current = GetCurrentModifiers();
            if (current != ModifierKey.None)
            {
                foreach (var combination in _combinationKeyDownDic)
                {
                    var keyCombination = combination.Key;
                    var actions = combination.Value;

                    if ((current & keyCombination.Modifiers) != keyCombination.Modifiers)
                        continue;

                    KeyCode primary = (KeyCode)keyCombination.PrimaryKey;
                    if (Input.GetKeyDown(primary))
                    {
                        InvokeActions(actions);
                    }
                }
            }
        }

        /// <summary>
        /// 依次执行一组回调，单个回调为 null 时跳过
        /// </summary>
        /// <param name="list">要执行的回调列表</param>
        /// <remarks>
        /// <para>不捕获异常，某个回调抛出异常会中断同一列表后续回调的执行。</para>
        /// </remarks>
        private void InvokeActions(List<Action> list)
        {
            foreach (var action in list)
                action?.Invoke();
        }
#endif
    }
}
