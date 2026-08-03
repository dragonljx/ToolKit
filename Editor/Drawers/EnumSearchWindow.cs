#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace ToolKit.Editor
{
    public class EnumSearchWindow : PopupWindowContent
    {
        private SerializedProperty property;
        private Type enumType;
        private string[] enumNames;
        private string search = "";
        private Vector2 scroll;
        private int hoverIndex = -1;
        private bool focusSearch;
        private Rect buttonRect;
        private Color selectColor = new(0.368f, 0.47f, 0.61f, 0.25f);
        public static void Show(Rect rect, SerializedProperty property, Type enumType)
        {

            PopupWindow.Show(rect, new EnumSearchWindow()
            {
                buttonRect = rect,
                property = property,
                enumType = enumType,
                enumNames = Enum.GetNames(enumType)
            });
        }
        public override void OnOpen()
        {
            focusSearch = true;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(260, 250);
        }

        public override void OnGUI(Rect rect)
        {

            hoverIndex = -1;
            HandleMouseMove();
            DrawSearch();
            GUILayout.Space(5);
            DrawList();
        }

        private void HandleMouseMove()
        {
            switch (Event.current.type)
            {
                case EventType.MouseMove:
                case EventType.ScrollWheel:
                case EventType.MouseLeaveWindow:
                    editorWindow.Repaint();
                    break;
            }
        }

        private void DrawSearch()
        {
            GUI.SetNextControlName("EnumSearch");
            search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);
            GUILayoutUtility.GetLastRect();
            if (focusSearch && Event.current.type == EventType.Repaint)
            {
                EditorGUI.FocusTextInControl("EnumSearch");
                focusSearch = false;

            }
        }

        private void DrawList()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            var result = GetFiltered();
            for (int i = 0; i < result.Count; i++)
            {
                DrawItem(i, result[i]);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawItem(int index, string item)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            int enumIndex = Array.IndexOf(enumNames, item);
            bool selected = property.enumValueIndex == enumIndex;

            if (rect.Contains(Event.current.mousePosition))
            {
                hoverIndex = index;
            }

            if (index == hoverIndex)
            {
                GUI.Box(rect, GUIContent.none, EditorStyles.selectionRect);

            }

            GUIStyle style = new GUIStyle(EditorStyles.label);
            if (selected && index != hoverIndex)
            {
                EditorGUI.DrawRect(rect, selectColor);
            }

            GUI.Label(rect, item, style);

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                property.serializedObject.Update();
                property.enumValueIndex = enumIndex;
                property.serializedObject.ApplyModifiedProperties();
                editorWindow.Close();
                GUIUtility.ExitGUI();
            }
        }

        private List<string> GetFiltered()
        {
            if (string.IsNullOrEmpty(search))
            {
                return enumNames.ToList();
            }
            return enumNames.Where(x => x.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        }
    }
}
#endif