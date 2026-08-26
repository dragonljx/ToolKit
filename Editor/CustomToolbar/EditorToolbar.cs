using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ToolKit.Editor
{
    public enum ToolbarLocation
    {
        Left,
        Right
    }

    [InitializeOnLoad]
    public static class EditorToolbar
    {
        private struct ToolbarUI
        {
            public Func<VisualElement> Factory;
            public ToolbarLocation Location;
        }

        private static readonly Type ToolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
        private static readonly List<ToolbarUI> DrawUI = new();
        private static ScriptableObject _toolbar;
        private static StyleSheet _styleSheet;
        private static Font _toolbarFont;

        static EditorToolbar()
        {
            _styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.dragon.toolkit/Editor/CustomToolbar/ToolbarStyle.uss");
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        public static void AddButtonRight(string text, Action callback = null) => AddButton(text, callback, ToolbarLocation.Right);
        public static void AddButtonLift(string text, Action callback = null) => AddButton(text, callback, ToolbarLocation.Left);

        public static void AddButton(string text, Action callback = null, ToolbarLocation location = ToolbarLocation.Right)
        {
            AddElement(() =>
            {
                var button = new Button(callback) { text = text };
                button.AddToClassList("unity-toolbar-button");
                button.AddToClassList("custom-toolbar-button");
                button.AddToClassList("custom-toolbar-element");
                button.style.paddingLeft = 8;
                button.style.paddingRight = 8;
                return button;
            }, location);
        }

        public static void AddDropdownField(List<string> choices, Func<string, string> selector, ToolbarLocation location = ToolbarLocation.Right, Action<DropdownField> click = null)
        {
            AddElement(() =>
            {
                var dropdown = new DropdownField(choices, 0, selector);
                if (click != null) dropdown.RegisterCallback<MouseDownEvent>(_ => click(dropdown));
                dropdown.style.unityFont = _toolbarFont;
                dropdown.AddToClassList("unity-toolbar-dropdown");
                dropdown.AddToClassList("custom-toolbar-dropdown");
                dropdown.AddToClassList("custom-toolbar-element");
                return dropdown;
            }, location);
        }

        public static void AddElement<T>(ToolbarLocation location = ToolbarLocation.Right) where T : VisualElement, new()
        {
            AddElement(() => new T(), location);
        }

        private static void AddElement(Func<VisualElement> factory, ToolbarLocation location)
        {
            DrawUI.Add(new ToolbarUI { Factory = factory, Location = location });
            _toolbar = null;
        }

        private static void OnUpdate()
        {
            if (_toolbar != null) return;
            var toolbars = Resources.FindObjectsOfTypeAll(ToolbarType);
            if (toolbars.Length == 0) return;
            _toolbar = toolbars[0] as ScriptableObject;
            if (_toolbar == null) return;
            CreateToolbar();
        }

        private static void CreateToolbar()
        {
            var rootField = _toolbar.GetType().GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
            var root = rootField?.GetValue(_toolbar) as VisualElement;
            if (root == null) return;
            if (_styleSheet != null) root.styleSheets.Add(_styleSheet);

            var left = new VisualElement { name = "CustomToolbarLeft" };
            left.AddToClassList("custom-toolbar-overflow");
            left.style.flexDirection = FlexDirection.Row;

            var right = new VisualElement { name = "CustomToolbarRight" };
            right.AddToClassList("custom-toolbar-overflow");
            right.style.flexDirection = FlexDirection.Row;
            right.style.justifyContent = Justify.FlexEnd;
            left.style.justifyContent = Justify.FlexStart;

            var leftZone = root.Q("ToolbarZoneLeftAlign");
            var rightZone = root.Q("ToolbarZoneRightAlign");
            if (leftZone == null || rightZone == null) return;

            leftZone.Add(left);
            rightZone.Add(right);

            foreach (var item in DrawUI)
            {
                var element = item.Factory();
                if (item.Location == ToolbarLocation.Left) left.Add(element);
                else right.Add(element);
            }
        }
    }
}