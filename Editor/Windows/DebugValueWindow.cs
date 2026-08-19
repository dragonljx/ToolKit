using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace ToolKit.Editor
{
    public class DebugValueWindow : EditorWindow
    {
        private static DebugValueWindow instance;
        private VisualElement container;
        private Dictionary<string, Label> debugLabels = new();
        public void CreateGUI()
        {
            container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;
            container.style.paddingLeft = new StyleLength(20);
            container.style.paddingTop = new StyleLength(20);
            container.style.paddingRight = new StyleLength(20);

            rootVisualElement.Add(container);


            instance = this; // 保证 CreateGUI 时刷新实例

        }
        private void OnEnable()
        {
            // 订阅 Play Mode 状态改变事件
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            // 取消订阅，避免内存泄漏
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                container.Clear();
                debugLabels.Clear();
            }
        }
        public static void ShowValue(string name, object value)
        {
            if (instance == null) return;
            instance.InternalShowValue(name, value);
        }
        private void InternalShowValue(string name, object value)
        {
            if (!debugLabels.TryGetValue(name, out var label))
            {
                label = new Label();
                container.Add(label);

                debugLabels[name] = label;
            }
            label.text = $"{name} -> {value}";
        }

        public static void RemoveValue(string name)
        {
            if (instance == null) return;
            if (instance.debugLabels.TryGetValue(name, out var label))
            {
                instance.container.Remove(label);
                instance.debugLabels.Remove(name);
            }
        }
    }
}
