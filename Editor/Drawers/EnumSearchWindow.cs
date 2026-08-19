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
        private SerializedObject serializedObject;
        private string propertyPath;
        private string[] enumNames;
        private string search = "";
        private Vector2 scroll;
        private int hoverIndex = -1;
        private bool focusSearch;
        private int selectedIndex;
        private bool needScrollToSelected;
        private Color selectColor = new(0.368f, 0.47f, 0.61f, 0.25f);
        private GUIStyle style = new GUIStyle(EditorStyles.label);

        public static void Show(Rect rect, SerializedProperty property, Type enumType, int selectedIndex)
        {
            PopupWindow.Show(rect, new EnumSearchWindow()
            {
                property = property.Copy(),
                serializedObject = property.serializedObject,
                propertyPath = property.propertyPath,
                enumNames = enumType != null && enumType.IsEnum ? Enum.GetNames(enumType) : property.enumNames,
                selectedIndex = selectedIndex,
            });
        }

        public override void OnOpen()
        {
            style.padding = new RectOffset(10, 0, 0, 0);
            focusSearch = true;

            needScrollToSelected = true;
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
            var result = GetFiltered();

            if (needScrollToSelected)
            {
                needScrollToSelected = false;

                float itemHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                float listHeight = GetWindowSize().y - 30;

                string selectedName = selectedIndex >= 0 && selectedIndex < enumNames.Length ? enumNames[selectedIndex] : "";

                int displayIndex = result.IndexOf(selectedName);

                if (displayIndex >= 0)
                {
                    float target = displayIndex * itemHeight - listHeight * 0.5f;

                    float maxScroll = Mathf.Max( 0, result.Count * itemHeight - listHeight);

                    scroll.y = Mathf.Clamp( target, 0, maxScroll);
                }
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);

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

            bool selected = selectedIndex == enumIndex;

            if (rect.Contains(Event.current.mousePosition))
            {
                hoverIndex = index;
            }

            if (index == hoverIndex)
            {
                GUI.Box(rect, GUIContent.none, EditorStyles.selectionRect);
            }
            else if (selected)
            {
                EditorGUI.DrawRect(rect, selectColor);
            }

            GUI.Label(rect, item, style);

            if (Event.current.type == EventType.MouseDown &&
                rect.Contains(Event.current.mousePosition))
            {
                SerializedObject targetObject = serializedObject ?? property?.serializedObject;
                if (targetObject == null)
                {
                    editorWindow.Close();
                    GUIUtility.ExitGUI();
                }

                targetObject.Update();
                SerializedProperty targetProperty = GetTargetProperty();
                if (targetProperty == null)
                {
                    editorWindow.Close();
                    GUIUtility.ExitGUI();
                }

                targetProperty.enumValueIndex = enumIndex;
                targetObject.ApplyModifiedProperties();

                selectedIndex = enumIndex;

                editorWindow.Close();
                GUIUtility.ExitGUI();
            }
        }

        private SerializedProperty GetTargetProperty()
        {
            if (serializedObject == null || string.IsNullOrEmpty(propertyPath))
                return property;

            return serializedObject.FindProperty(propertyPath) ?? property;
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
