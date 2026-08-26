using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ToolKit.Editor
{
    [CustomPropertyDrawer(typeof(SearchableEnumAttribute))]
    public class SearchableEnumDrawer : PropertyDrawer
    {
        #region Public Methods

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.Enum)
            {
                DrawEnum(position, property, label);
                return;
            }

            Rect propertyRect = new(
                position.x,
                position.y,
                position.width,
                position.height - EditorGUIUtility.standardVerticalSpacing);

            EditorGUI.PropertyField(
                propertyRect,
                property,
                label,
                false);
        }

        #endregion Public Methods

        #region Enum Drawing

        private void DrawEnum(Rect position, SerializedProperty property, GUIContent label)
        {
            bool hasLabel = !string.IsNullOrEmpty(label.text);

            if (!hasLabel)
            {
                DrawEnumButton(position, property);
                return;
            }

            Rect labelRect = new(
                position.x,
                position.y,
                EditorGUIUtility.labelWidth,
                EditorGUIUtility.singleLineHeight);

            EditorGUI.LabelField(labelRect, label);

            Rect buttonRect = new(
                position.x + EditorGUIUtility.labelWidth,
                position.y,
                position.width - EditorGUIUtility.labelWidth,
                EditorGUIUtility.singleLineHeight);

            DrawEnumButton(buttonRect, property);
        }

        private void DrawEnumButton(Rect buttonRect, SerializedProperty property)
        {
            int index = property.enumValueIndex;
            int size = property.enumNames.Length;
            string enumName = index >= 0 && index < property.enumNames.Length
                ? property.enumNames[index]
                : string.Empty;

            if (GUI.Button(buttonRect, enumName, EditorStyles.popup))
            {
                Type enumType = fieldInfo?.FieldType;

                Rect screenRect = GUIUtility.GUIToScreenRect(buttonRect);
                float height = Mathf.Min(EditorGUIUtility.singleLineHeight * size+50, 250);

                SearchableEnumWindow.Show(property, screenRect, new(260, height));
            }
        }

        #endregion Enum Drawing
    }
}