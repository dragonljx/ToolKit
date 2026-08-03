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
        struct ToolbarUI
        {
            public VisualElement UI;
            public ToolbarLocation Location;
        }

        private static readonly Type MToolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
        private static ScriptableObject _mCurrentToolbar;
        private static List<ToolbarUI> _drawUI;
        private static StyleSheet _styleSheet;
  
        static EditorToolbar()
        {
            _styleSheet =
                AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Packages/dragon.toolkit/Editor/CustomToolbar/ToolbarStyle.uss");
            _drawUI = new List<ToolbarUI>();
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }


        public static void AddDropdownField(List<string> choices, Func<string, string> selectorCallback,
            ToolbarLocation location, Action<DropdownField> clickCallback = null)
        {
            DropdownField dropdownField = new DropdownField(choices, 0, selectorCallback);
            if (clickCallback != null)
            {
                dropdownField.RegisterCallback<MouseDownEvent>(evt => clickCallback.Invoke(dropdownField));
            }

            dropdownField.hierarchy.ElementAt(0).style.overflow = Overflow.Visible;
            AddDrawUI(dropdownField, location);
        }

        public static void AddButton(string buttonText, Action callback, ToolbarLocation location)
        {
            Button button = new Button(callback);
            button.text = buttonText;
            button.style.overflow = Overflow.Visible;
            AddDrawUI(button, location);
        }

        static void AddDrawUI(VisualElement ui, ToolbarLocation location)
        {
            _drawUI.Add(new ToolbarUI()
            {
                UI = ui,
                Location = location
            });
        }

        private static void OnUpdate()
        {
            if (_mCurrentToolbar != null) return;
            var toolbars = Resources.FindObjectsOfTypeAll(MToolbarType);
            _mCurrentToolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;

            if (_mCurrentToolbar == null) return;
            var root = _mCurrentToolbar.GetType()
                .GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
            var mRoot = root?.GetValue(_mCurrentToolbar) as VisualElement;

            if (mRoot == null) return;
            var elementLeft = new VisualElement()
            {
                style =
                {
                    flexGrow = 1,
                    flexDirection = FlexDirection.Row,
                    // width = Length.Percent(100),
                    justifyContent = Justify.FlexEnd
                },
                name = "Custom Left"
            };
            var elementRight = new VisualElement()
            {
                style =
                {
                    flexGrow = 1,
                    flexDirection = FlexDirection.Row,
                    // width = Length.Percent(100),
                    justifyContent = Justify.FlexStart
                },
                name = "Custom Right"
            };

            var toolbarLeft = mRoot.Q("ToolbarZoneLeftAlign");
            toolbarLeft.Add(elementLeft);
            var toolbarRight = mRoot.Q("ToolbarZoneRightAlign");
            toolbarRight.Add(elementRight);

            foreach (var ui in _drawUI)
            {
                if (ui.Location == ToolbarLocation.Right)
                {
                    elementRight.Add(ui.UI);
                }
                else
                {
                    elementLeft.Add(ui.UI);
                }
            }
        }
    }
}