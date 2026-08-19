using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ToolKit.Editor
{
    [CustomEditor(typeof(UnityEngine.Object), true)]
    public class ButtonDrawer : UnityEditor.Editor
    {
        private readonly Dictionary<MethodInfo, object[]> methodArguments = new();

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var targetObj = target;
            var methods = targetObj.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            var buttons = methods
                .Select(m => new { Method = m, Attribute = m.GetCustomAttribute<ButtonAttribute>() })
                .Where(x => x.Attribute != null)
                .OrderBy(x => x.Attribute.Order)
                .ToList();

            var groups = buttons.Where(x => !string.IsNullOrEmpty(x.Attribute.Group)).GroupBy(x => x.Attribute.Group);
            var noGroupButtons = buttons.Where(x => string.IsNullOrEmpty(x.Attribute.Group));

            foreach (var button in noGroupButtons)
            {
                EditorGUILayout.Space();
                DrawButton(targetObj, button.Method, button.Attribute);
            }

            foreach (var group in groups)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(group.Key, EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                foreach (var item in group)
                    DrawButton(targetObj, item.Method, item.Attribute);
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawButton(UnityEngine.Object targetObj, MethodInfo method, ButtonAttribute attribute)
        {
            var title = string.IsNullOrEmpty(attribute.Name) ? method.Name : attribute.Name;
            var parameters = method.GetParameters();
            object[] args = null;
            if (parameters.Length > 0)
            {
                GUILayout.BeginVertical("box");
                string label = string.IsNullOrEmpty(attribute.Name) ? method.Name: attribute.Name ;
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            }

            if (parameters.Length > 0)
            {
                if (!methodArguments.TryGetValue(method, out args))
                {
                    args = parameters.Select(p => GetDefaultValue(p.ParameterType)).ToArray();
                    methodArguments.Add(method, args);
                }

                for (int i = 0; i < parameters.Length; i++)
                    args[i] = DrawParameter(parameters[i].ParameterType, parameters[i].Name, args[i]);
            }

            GUI.backgroundColor = GetButtonColor(attribute.Color);

            if (GUILayout.Button(title))
            {
                try
                {
                    method.Invoke(method.IsStatic ? null : targetObj, args);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Button执行失败:{method.Name}\n{e}");
                }
            }

            GUI.backgroundColor = Color.white;
            if (parameters.Length > 0)
            {
                GUILayout.EndVertical();
            }
        }

        private object DrawParameter(Type type, string name, object value)
        {
            if (type == typeof(int)) return EditorGUILayout.IntField(name, (int)value);
            if (type == typeof(float)) return EditorGUILayout.FloatField(name, (float)value);
            if (type == typeof(string)) return EditorGUILayout.TextField(name, (string)value);
            if (type == typeof(bool)) return EditorGUILayout.Toggle(name, (bool)value);
            if (type.IsEnum) return EditorGUILayout.EnumPopup(name, (Enum)value);

            EditorGUILayout.LabelField(name, $"不支持类型:{type.Name}");
            return value;
        }

        private object GetDefaultValue(Type type)
        {
            if (type == typeof(string)) return "";
            if (type == typeof(int)) return 0;
            if (type == typeof(float)) return 0f;
            if (type == typeof(bool)) return false;
            if (type.IsEnum) return Enum.GetValues(type).GetValue(0);
            if (type.IsValueType) return Activator.CreateInstance(type);
            return null;
        }

        private Color GetButtonColor(ButtonColor color) => color switch
        {
            ButtonColor.Green => Color.green,
            ButtonColor.Red => Color.red,
            ButtonColor.Yellow => Color.yellow,
            ButtonColor.Blue => Color.blue,
            ButtonColor.White => Color.white,
            _ => Color.white
        };
    }
}