using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace ToolKit.Editor
{
    [CustomPropertyDrawer(typeof(SearchableEnumAttribute))]
    public class SearchableEnumDrawer : PropertyDrawer
    {
        #region Constants

        private const string SerializedDictionaryTypeName = "ToolKit.SerializedDictionary.SerializedDictionary`2";
        private const string SerializedDictionaryListName = "_serializedList";
        private const string SerializedDictionaryKeyName = "Key";
        private const string SerializedDictionaryValueName = "Value";

        #endregion

        #region Fields

        private readonly Dictionary<string, ReorderableList> _dictionaryLists = new();
        private readonly Dictionary<string, Type> _typeCache = new();

        #endregion

        #region Public Methods

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.serializedObject.targetObject == null)
            {
                _dictionaryLists.Clear();
                _typeCache.Clear();
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            Type targetType = GetDeclaredType(property);

            if (property.isArray && IsEnumArray(property, targetType))
            {
                DrawArray(position, property, label);
            }
            else if (property.propertyType == SerializedPropertyType.Enum)
            {
                DrawEnum(position, property, label);
            }
            else if (IsSerializedDictionary(targetType))
            {
                DrawSerializedDictionary(position, property, label);
            }
            else
            {
                DrawUnsupported(position, property, label);
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.serializedObject.targetObject == null)
                return EditorGUIUtility.singleLineHeight;

            Type targetType = GetDeclaredType(property);

            if (property.isArray && IsEnumArray(property, targetType))
            {
                return property.isExpanded
                    ? EditorGUIUtility.singleLineHeight * (property.arraySize + 1)
                    : EditorGUIUtility.singleLineHeight;
            }

            if (property.propertyType == SerializedPropertyType.Enum)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            if (IsSerializedDictionary(targetType))
            {
                return CalculateDictionaryHeight(property);
            }

            return EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;
        }

        #endregion

        #region Enum Drawing

        private void DrawEnum(Rect position, SerializedProperty property, GUIContent label)
        {
            bool hasLabel = !string.IsNullOrEmpty(label.text);

            if (hasLabel)
            {
                Rect labelRect = new(position.x, position.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(labelRect, label);

                Rect buttonRect = new(
                    position.x + EditorGUIUtility.labelWidth,
                    position.y,
                    position.width - EditorGUIUtility.labelWidth,
                    EditorGUIUtility.singleLineHeight
                );
                DrawEnumButton(buttonRect, property);
            }
            else
            {
                DrawEnumButton(position, property);
            }
        }

        private void DrawEnumButton(Rect buttonRect, SerializedProperty property)
        {
            int index = property.enumValueIndex;
            string enumName = index >= 0 && index < property.enumNames.Length
                ? property.enumNames[index]
                : string.Empty;

            if (GUI.Button(buttonRect, enumName, EditorStyles.popup))
            {
                EnumSearchWindow.Show(buttonRect, property, GetEnumType(property), index);
            }
        }

        #endregion

        #region Array Drawing

        private bool IsEnumArray(SerializedProperty property, Type declaredType)
        {
            if (!property.isArray || declaredType == null)
                return false;

            Type elementType = null;

            if (declaredType.IsArray)
            {
                elementType = declaredType.GetElementType();
            }
            else if (declaredType.IsGenericType && declaredType.GetGenericTypeDefinition() == typeof(List<>))
            {
                elementType = declaredType.GetGenericArguments()[0];
            }
            else
            {
                // 如果属性本身就是数组元素（如 data[0]），property.isArray 为 false，不会进入此分支
                return false;
            }

            return elementType != null && elementType.IsEnum;
        }

        private void DrawArray(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect foldoutRect = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label);

            if (!property.isExpanded)
                return;

            EditorGUI.indentLevel++;

            float y = position.y + EditorGUIUtility.singleLineHeight;
            for (int i = 0; i < property.arraySize; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                Rect rect = new(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
                DrawEnum(rect, element, new GUIContent($"Element {i}"));
                y += EditorGUIUtility.singleLineHeight;
            }

            EditorGUI.indentLevel--;
        }

        #endregion

        #region SerializedDictionary Drawing

        private bool IsSerializedDictionary(Type type)
        {
            if (type == null || !type.IsGenericType)
                return false;

            return type.GetGenericTypeDefinition().FullName == SerializedDictionaryTypeName;
        }

        private void DrawSerializedDictionary(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty listProperty = property.FindPropertyRelative(SerializedDictionaryListName);

            if (listProperty == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            ReorderableList list = GetDictionaryList(property, listProperty, label);
            list.DoList(position);
        }

        private ReorderableList GetDictionaryList(SerializedProperty property, SerializedProperty listProperty, GUIContent label)
        {
            string key = $"{property.serializedObject.targetObject.GetInstanceID()}_{property.propertyPath}";

            if (!_dictionaryLists.TryGetValue(key, out ReorderableList list))
            {
                list = CreateDictionaryList(property, listProperty, label);
                _dictionaryLists[key] = list;
            }

            return list;
        }

        private ReorderableList CreateDictionaryList(SerializedProperty property, SerializedProperty listProperty, GUIContent label)
        {
            ReorderableList list = new(property.serializedObject, listProperty, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, label),
                drawElementCallback = (rect, index, active, focused) => DrawDictionaryElement(rect, listProperty, index),
                elementHeightCallback = index => GetDictionaryElementHeight(listProperty, index),
                onAddCallback = _ => AddDictionaryElement(listProperty),
                onRemoveCallback = reorderableList => RemoveDictionaryElement(listProperty, reorderableList.index)
            };

            return list;
        }

        private void DrawDictionaryElement(Rect rect, SerializedProperty listProperty, int index)
        {
            SerializedProperty element = listProperty.GetArrayElementAtIndex(index);
            SerializedProperty key = element.FindPropertyRelative(SerializedDictionaryKeyName);
            SerializedProperty value = element.FindPropertyRelative(SerializedDictionaryValueName);

            if (key == null || value == null)
                return;

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            Rect keyRect = new(rect.x, rect.y, rect.width, lineHeight);
            DrawEnum(keyRect, key, new GUIContent("Key"));

            float valueHeight = EditorGUI.GetPropertyHeight(value, true);
            Rect valueRect = new(rect.x, rect.y + lineHeight + spacing, rect.width, valueHeight);
            EditorGUI.PropertyField(valueRect, value, new GUIContent("Value"), true);
        }

        private float GetDictionaryElementHeight(SerializedProperty listProperty, int index)
        {
            SerializedProperty element = listProperty.GetArrayElementAtIndex(index);
            SerializedProperty value = element.FindPropertyRelative(SerializedDictionaryValueName);

            float valueHeight = value != null
                ? EditorGUI.GetPropertyHeight(value, true)
                : EditorGUIUtility.singleLineHeight;

            return EditorGUIUtility.singleLineHeight
                   + EditorGUIUtility.standardVerticalSpacing
                   + valueHeight
                   + EditorGUIUtility.standardVerticalSpacing;
        }

        private void AddDictionaryElement(SerializedProperty listProperty)
        {
            int index = listProperty.arraySize;
            listProperty.InsertArrayElementAtIndex(index);

            SerializedProperty element = listProperty.GetArrayElementAtIndex(index);
            SerializedProperty key = element.FindPropertyRelative(SerializedDictionaryKeyName);

            if (key != null && key.propertyType == SerializedPropertyType.Enum)
            {
                key.enumValueIndex = 0;
            }
        }

        private void RemoveDictionaryElement(SerializedProperty listProperty, int index)
        {
            if (index < 0 || index >= listProperty.arraySize)
                return;

            listProperty.DeleteArrayElementAtIndex(index);
        }

        private float CalculateDictionaryHeight(SerializedProperty property)
        {
            SerializedProperty listProperty = property.FindPropertyRelative(SerializedDictionaryListName);
            if (listProperty == null)
                return EditorGUIUtility.singleLineHeight;

            float height = EditorGUIUtility.singleLineHeight; // Header

            for (int i = 0; i < listProperty.arraySize; i++)
            {
                SerializedProperty element = listProperty.GetArrayElementAtIndex(i);
                SerializedProperty value = element.FindPropertyRelative(SerializedDictionaryValueName);

                float valueHeight = value != null
                    ? EditorGUI.GetPropertyHeight(value, true)
                    : EditorGUIUtility.singleLineHeight;

                height += EditorGUIUtility.singleLineHeight
                          + EditorGUIUtility.standardVerticalSpacing
                          + valueHeight
                          + EditorGUIUtility.standardVerticalSpacing;
            }

            return height + EditorGUIUtility.standardVerticalSpacing;
        }

        #endregion

        #region Unsupported Type

        private void DrawUnsupported(Rect position, SerializedProperty property, GUIContent label)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            Rect propertyRect = new(position.x, position.y, position.width, lineHeight);
            Rect helpBoxRect = new(position.x, position.y + lineHeight + spacing, position.width, lineHeight);

            EditorGUI.PropertyField(propertyRect, property, label, false);
            EditorGUI.HelpBox(helpBoxRect, "SearchableEnum 不支持该属性类型。", MessageType.Warning);
        }

        #endregion

        #region Type Resolution (with Caching)

        // 获取属性的声明类型（不做集合解包）
        private Type GetDeclaredType(SerializedProperty property)
        {
            string cacheKey = $"{property.serializedObject.targetObject.GetInstanceID()}_{property.propertyPath}";
            if (_typeCache.TryGetValue(cacheKey, out Type cachedType))
                return cachedType;

            Type type = property.serializedObject.targetObject.GetType();

            if (type.IsEnum)
            {
                _typeCache[cacheKey] = type;
                return type;
            }

            string[] paths = property.propertyPath.Split('.');

            for (int i = 0; i < paths.Length; i++)
            {
                if (type == null)
                    break;

                if (TryResolveSerializedDictionaryEntry(type, paths, ref i, out Type entryType))
                {
                    type = entryType;
                    continue;
                }

                string path = paths[i];

                if (path == "Array")
                    continue;

                if (path.StartsWith("data["))
                {
                    type = ResolveArrayElementType(type);
                    continue;
                }

                FieldInfo field = GetField(type, path);
                if (field != null)
                {
                    type = field.FieldType;
                    continue;
                }

                PropertyInfo prop = GetProperty(type, path);
                if (prop != null)
                {
                    type = prop.PropertyType;
                    continue;
                }

                type = null;
                break;
            }

            _typeCache[cacheKey] = type;
            return type;
        }

        // 仅供内部路径解析使用（处理 data[i] 时的集合元素类型）
        private Type ResolveArrayElementType(Type type)
        {
            if (type.IsArray)
                return type.GetElementType();

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                return type.GetGenericArguments()[0];

            return type;
        }

        // EnumSearchWindow 需要获取具体的枚举类型（数组元素、字典键等）
        private Type GetEnumType(SerializedProperty property)
        {
            Type declaredType = GetDeclaredType(property);

            // 如果声明类型是 List<T> 或 T[]，则提取元素类型
            if (declaredType != null)
            {
                if (declaredType.IsArray)
                    return declaredType.GetElementType();

                if (declaredType.IsGenericType && declaredType.GetGenericTypeDefinition() == typeof(List<>))
                    return declaredType.GetGenericArguments()[0];
            }

            return declaredType;
        }

        private bool TryResolveSerializedDictionaryEntry(Type type, string[] paths, ref int pathIndex, out Type entryType)
        {
            entryType = null;

            if (!TryGetSerializedDictionaryType(type, out Type dictionaryType) ||
                paths[pathIndex] != SerializedDictionaryListName)
            {
                return false;
            }

            int memberIndex = pathIndex + 3;
            if (memberIndex >= paths.Length ||
                paths[pathIndex + 1] != "Array" ||
                !paths[pathIndex + 2].StartsWith("data["))
            {
                return false;
            }

            string memberName = paths[memberIndex];
            Type[] genericArguments = dictionaryType.GetGenericArguments();

            entryType = memberName switch
            {
                SerializedDictionaryKeyName => genericArguments[0],
                SerializedDictionaryValueName => genericArguments[1],
                _ => null
            };

            if (entryType != null)
                pathIndex = memberIndex;

            return entryType != null;
        }

        private bool TryGetSerializedDictionaryType(Type type, out Type dictionaryType)
        {
            dictionaryType = null;

            while (type != null)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition().FullName == SerializedDictionaryTypeName)
                {
                    dictionaryType = type;
                    return true;
                }

                type = type.BaseType;
            }

            return false;
        }

        #endregion

        #region Reflection Helpers

        private FieldInfo GetField(Type type, string name)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            while (type != null)
            {
                FieldInfo field = type.GetField(name, flags);
                if (field != null)
                    return field;

                type = type.BaseType;
            }

            return null;
        }

        private PropertyInfo GetProperty(Type type, string name)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            while (type != null)
            {
                PropertyInfo property = type.GetProperty(name, flags);
                if (property != null)
                    return property;

                type = type.BaseType;
            }

            return null;
        }

        #endregion
    }
}