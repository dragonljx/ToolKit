
using UnityEditor;
using UnityEngine;
namespace ToolKit.Editor
{

    [CustomPropertyDrawer(typeof(SearchableEnumAttribute))]
    public class SearchableEnumDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {

            if (property.propertyType != SerializedPropertyType.Enum)
            {
                EditorGUI.PropertyField(
                    position,
                    property,
                    label,
                    true
                );

                return;
            }
            EditorGUI.BeginProperty(position, label, property);
            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
            Rect buttonRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, 
                position.width - EditorGUIUtility.labelWidth, 
                EditorGUIUtility.singleLineHeight);
            GUIContent displayLabel = new GUIContent(ObjectNames.NicifyVariableName(label.text), label.tooltip);

            EditorGUI.LabelField(labelRect, displayLabel);
            string currentName = property.enumNames[property.enumValueIndex];
            if (GUI.Button(buttonRect, currentName, EditorStyles.popup))
            {
                EnumSearchWindow.Show(buttonRect, property, fieldInfo.FieldType);
            }
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }

}