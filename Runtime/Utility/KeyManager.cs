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
    public enum InputEventType
    {
        Down,
        Up,
        Hold
    }

    [Flags]
    public enum ModifierKey
    {
        None = 0,
        Control = 1 << 0,
        Shift = 1 << 1,
        Alt = 1 << 2
    }

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
        Enter = KeyCode.KeypadEnter,
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

    public class InputBindingHandle : IDisposable
    {
        public readonly InputEventKey Key;
        public readonly Action Action;

#if ENABLE_INPUT_SYSTEM
        public InputAction InputAction { get; set; }
#endif

        public InputBindingHandle(InputEventKey key, Action action)
        {
            Key = key;
            Action = action;
        }

        public void Dispose()
        {
#if ENABLE_INPUT_SYSTEM
            InputAction?.Disable();
            InputAction?.Dispose();
#endif
        }
    }

    public class CombinationBindingHandle : IDisposable
    {
        public KeyCombination Key;
        public readonly Action Action;

#if ENABLE_INPUT_SYSTEM
        public InputAction InputAction { get; set; }
#endif

        public CombinationBindingHandle(KeyCombination key, Action action)
        {
            Key = key;
            Action = action;
        }

        public void Dispose()
        {
#if ENABLE_INPUT_SYSTEM
            InputAction?.Disable();
            InputAction?.Dispose();
#endif
        }
    }

    public readonly struct InputEventKey : IEquatable<InputEventKey>
    {
#if ENABLE_INPUT_SYSTEM
        public readonly Key Key;
#else
        public readonly KeyCode Key;
#endif
        public readonly InputEventType EventType;

#if ENABLE_INPUT_SYSTEM
        public InputEventKey(Key key, InputEventType eventType)
#else
        public InputEventKey(KeyCode key, InputEventType eventType)
#endif
        {
            Key = key;
            EventType = eventType;
        }

        public bool Equals(InputEventKey other)
        {
            return Key == other.Key && EventType == other.EventType;
        }

        public override bool Equals(object obj)
        {
            return obj is InputEventKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Key, EventType);
        }

        public override string ToString()
        {
            return $"{Key} + {EventType}";
        }

        public static bool operator ==(InputEventKey left, InputEventKey right)
            => left.Equals(right);

        public static bool operator !=(InputEventKey left, InputEventKey right)
            => !left.Equals(right);
    }

    public readonly struct KeyCombination : IEquatable<KeyCombination>
    {
        public readonly ModifierKey Modifiers;
        public readonly InputKey PrimaryKey;

        public KeyCombination(ModifierKey modifiers, InputKey primaryKey)
        {
            PrimaryKey = primaryKey;
            Modifiers = modifiers;
        }

        public bool Equals(KeyCombination other)
        {
            return PrimaryKey == other.PrimaryKey && Modifiers == other.Modifiers;
        }

        public override bool Equals(object obj)
        {
            return obj is KeyCombination other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PrimaryKey, Modifiers);
        }

        public override string ToString()
        {
            return $"{Modifiers} + {PrimaryKey}";
        }

        public static bool operator ==(KeyCombination left, KeyCombination right)
            => left.Equals(right);

        public static bool operator !=(KeyCombination left, KeyCombination right)
            => !left.Equals(right);
    }

    // ============= InputManager 主类 =============
    public class InputManager : ToolKit.Singleton<InputManager>
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
        public static InputBindingHandle AddKey(InputKey key,  Action action)
        {
#if ENABLE_INPUT_SYSTEM
            return AddKeyNew(key, InputEventType.Hold, action);
#else
            return AddKeyOld(key, InputEventType.Hold, action);
#endif
        }
        public static InputBindingHandle AddKeyDown(InputKey key,  Action action)
        {
#if ENABLE_INPUT_SYSTEM
            return AddKeyNew(key, InputEventType.Down, action);
#else
            return AddKeyOld(key, InputEventType.Down, action);
#endif
        }
        public static InputBindingHandle AddKeyUp(InputKey key, Action action)
        {
#if ENABLE_INPUT_SYSTEM
            return AddKeyNew(key, InputEventType.Up, action);
#else
            return AddKeyOld(key, InputEventType.Up, action);
#endif
        }

#if ENABLE_INPUT_SYSTEM
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
        public static void RemoveKey(InputBindingHandle handle)
        {
#if ENABLE_INPUT_SYSTEM
            RemoveKeyNew(handle);
#else
            RemoveKeyOld(handle);
#endif
        }

#if ENABLE_INPUT_SYSTEM
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
        public static void RemoveCombinationKey(CombinationBindingHandle handle)
        {
#if ENABLE_INPUT_SYSTEM
            RemoveCombinationKeyNew(handle);
#else
            RemoveCombinationKeyOld(handle);
#endif
        }

#if ENABLE_INPUT_SYSTEM
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
        public static ModifierKey GetCurrentModifiers()
        {
#if ENABLE_INPUT_SYSTEM
            return GetCurrentModifiersNew();
#else
            return GetCurrentModifiersOld();
#endif
        }

#if ENABLE_INPUT_SYSTEM
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
        public static void Clear()
        {
#if ENABLE_INPUT_SYSTEM
            ClearNew();
#else
            ClearOld();
#endif
        }

#if ENABLE_INPUT_SYSTEM
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
        private static void ClearOld()
        {
            Instance._combinationKeyDownDic.Clear();
            Instance._keyBindings.Clear();
        }
#endif
        protected override void OnDestroy()
        {
            Clear();
        }
 
        // ============= Update循环 =============
#if !ENABLE_INPUT_SYSTEM
        // 旧版Input Manager需要使用Update轮询
        void Update()
        {
            ProcessKeyBindings();
            ProcessCombinationKeys();
        }

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

        private void InvokeActions(List<Action> list)
        {
            foreach (var action in list)
                action?.Invoke();
        }
#endif
    }
}