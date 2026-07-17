using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ToolKit.Editor
{
    [CustomEditor(typeof(UnityEngine.Object), true)]
    public class ButtonDrawer : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            UnityEngine.Object targetObj = target;

            MethodInfo[] methods = targetObj.GetType().
                GetMethods(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance |
                    BindingFlags.Static
                );

            var buttons = methods
                .Where(m => m.GetCustomAttribute<ButtonAttribute>() != null)
                .OrderBy(m => m.GetCustomAttribute<ButtonAttribute>().Order);

            foreach (var method in buttons)
            {
                DrawButton(targetObj, method);
            }
        }



        private void DrawButton(UnityEngine.Object targetObj, MethodInfo method)
        {

            ButtonAttribute attribute = method.GetCustomAttribute<ButtonAttribute>();

            string title = string.IsNullOrEmpty(attribute.Name) ? method.Name : attribute.Name;

            GUI.backgroundColor = attribute.Color;

            if (GUILayout.Button(title))
            {
                try
                {
                    ExecuteMethod(targetObj, method);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Button执行失败:{method.Name}\n{e}");
                }
            }


            GUI.backgroundColor = Color.white;
        }

        private void ExecuteMethod(UnityEngine.Object targetObj, MethodInfo method)
        {

            ParameterInfo[] parameters = method.GetParameters();

            // 无参数
            if (parameters.Length == 0)
            {
                method.Invoke(method.IsStatic ? null : targetObj, null);

                return;
            }

            object[] args = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                args[i] =
                    GetDefaultValue(parameters[i].ParameterType);
            }

            method.Invoke(method.IsStatic ? null : targetObj, args);
        }

        private object GetDefaultValue(Type type)
        {

            if (type == typeof(string))
                return "";

            if (type == typeof(int))
                return 0;

            if (type == typeof(float))
                return 0f;


            if (type == typeof(bool))
                return false;

            if (type.IsValueType)
                return Activator.CreateInstance(type);

            return null;
        }

    }

}