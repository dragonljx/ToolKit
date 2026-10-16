using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ToolKit.Editor
{
    /// <summary>
    /// Button 渲染工具类
    /// <para>为 ButtonDrawer、PropertyButtonDrawer 提供统一的按钮渲染逻辑</para>
    /// </summary>
    public static class ButtonRenderUtility
    {
        #region 缓存管理

        /// <summary>
        /// 方法反射缓存，key: TypeFullName.MethodName
        /// </summary>
        private static readonly Dictionary<string, MethodInfo> MethodCache = new Dictionary<string, MethodInfo>();

        /// <summary>
        /// 清除所有缓存（域重载时自动调用）
        /// </summary>
        [InitializeOnLoadMethod]
        private static void ClearCacheOnDomainReload()
        {
            MethodCache.Clear();
        }

        /// <summary>
        /// 手动清除缓存
        /// </summary>
        public static void ClearCache()
        {
            MethodCache.Clear();
        }

        #endregion

        #region 颜色转换

        /// <summary>
        /// 将 ButtonColor 枚举转换为 Unity Color
        /// </summary>
        public static Color GetButtonColor(ButtonColor color)
        {
            switch (color)
            {
                case ButtonColor.Green:
                    return new Color(0.5f, 1f, 0.5f);
                case ButtonColor.Red:
                    return new Color(1f, 0.5f, 0.5f);
                case ButtonColor.Yellow:
                    return new Color(1f, 1f, 0.5f);
                case ButtonColor.Blue:
                    return new Color(0.5f, 0.7f, 1f);
                case ButtonColor.White:
                default:
                    return Color.white;
            }
        }

        #endregion

        #region 方法查找

        /// <summary>
        /// 查找方法（支持实例方法和静态方法，支持 public/private/protected）
        /// </summary>
        /// <param name="targetType">目标类型</param>
        /// <param name="methodName">方法名</param>
        /// <param name="useCache">是否使用缓存（默认 true）</param>
        /// <returns>找到的方法，未找到则返回 null</returns>
        public static MethodInfo FindMethod(Type targetType, string methodName, bool useCache = true)
        {
            if (targetType == null || string.IsNullOrEmpty(methodName))
                return null;

            string cacheKey = $"{targetType.FullName}.{methodName}";

            // 尝试从缓存获取
            if (useCache && MethodCache.TryGetValue(cacheKey, out MethodInfo cachedMethod))
            {
                return cachedMethod;
            }

            // 查找实例方法
            MethodInfo method = targetType.GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            // 如果没找到，尝试查找静态方法
            if (method == null)
            {
                method = targetType.GetMethod(methodName,
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            }

            // 缓存结果（即使是 null 也缓存，避免重复查找）
            if (useCache)
            {
                MethodCache[cacheKey] = method;
            }

            return method;
        }

        /// <summary>
        /// 查找带参数签名的方法
        /// </summary>
        public static MethodInfo FindMethod(Type targetType, string methodName, Type[] parameterTypes, bool useCache = true)
        {
            if (targetType == null || string.IsNullOrEmpty(methodName))
                return null;

            string cacheKey = $"{targetType.FullName}.{methodName}({string.Join(",", Array.ConvertAll(parameterTypes ?? Array.Empty<Type>(), t => t.Name))})";

            if (useCache && MethodCache.TryGetValue(cacheKey, out MethodInfo cachedMethod))
            {
                return cachedMethod;
            }

            MethodInfo method = targetType.GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, parameterTypes ?? Type.EmptyTypes, null);

            if (method == null)
            {
                method = targetType.GetMethod(methodName,
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null, parameterTypes ?? Type.EmptyTypes, null);
            }

            if (useCache)
            {
                MethodCache[cacheKey] = method;
            }

            return method;
        }

        #endregion

        #region 按钮绘制

        /// <summary>
        /// 绘制按钮（标准样式）
        /// </summary>
        /// <param name="rect">按钮矩形区域</param>
        /// <param name="text">按钮文本</param>
        /// <param name="color">按钮颜色</param>
        /// <returns>如果按钮被点击返回 true</returns>
        public static bool DrawButton(Rect rect, string text, ButtonColor color)
        {
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = GetButtonColor(color);

            bool clicked = GUI.Button(rect, text);

            GUI.backgroundColor = originalColor;

            return clicked;
        }

        /// <summary>
        /// 绘制按钮（带 GUIStyle）
        /// </summary>
        public static bool DrawButton(Rect rect, string text, ButtonColor color, GUIStyle style)
        {
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = GetButtonColor(color);

            bool clicked = GUI.Button(rect, text, style);

            GUI.backgroundColor = originalColor;

            return clicked;
        }

        /// <summary>
        /// 绘制按钮（带 GUIContent）
        /// </summary>
        public static bool DrawButton(Rect rect, GUIContent content, ButtonColor color)
        {
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = GetButtonColor(color);

            bool clicked = GUI.Button(rect, content);

            GUI.backgroundColor = originalColor;

            return clicked;
        }

        #endregion

        #region 方法调用

        /// <summary>
        /// 调用方法（带异常处理和 Dirty 标记）
        /// </summary>
        /// <param name="method">要调用的方法</param>
        /// <param name="target">目标对象（静态方法传 null）</param>
        /// <param name="parameters">方法参数（无参数传 null）</param>
        /// <returns>调用是否成功</returns>
        public static bool InvokeMethod(MethodInfo method, object target, object[] parameters = null)
        {
            if (method == null)
            {
                Debug.LogError("尝试调用 null 方法");
                return false;
            }

            try
            {
                // 调用方法
                if (method.IsStatic)
                {
                    method.Invoke(null, parameters);
                }
                else
                {
                    if (target == null)
                    {
                        Debug.LogError($"尝试调用实例方法 {method.Name}，但目标对象为 null");
                        return false;
                    }
                    method.Invoke(target, parameters);
                }

                // 标记对象为 Dirty（触发序列化和保存）
                if (target is UnityEngine.Object unityObject)
                {
                    EditorUtility.SetDirty(unityObject);
                }

                return true;
            }
            catch (TargetInvocationException e)
            {
                // 展开内部异常
                Debug.LogError($"调用方法 {method.Name} 时出错:\n{e.InnerException ?? e}");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"调用方法 {method.Name} 时出错:\n{e}");
                return false;
            }
        }

        /// <summary>
        /// 调用方法并返回结果
        /// </summary>
        public static bool InvokeMethod<TResult>(MethodInfo method, object target, object[] parameters, out TResult result)
        {
            result = default;

            if (method == null)
            {
                Debug.LogError("尝试调用 null 方法");
                return false;
            }

            try
            {
                object returnValue;

                if (method.IsStatic)
                {
                    returnValue = method.Invoke(null, parameters);
                }
                else
                {
                    if (target == null)
                    {
                        Debug.LogError($"尝试调用实例方法 {method.Name}，但目标对象为 null");
                        return false;
                    }
                    returnValue = method.Invoke(target, parameters);
                }

                if (returnValue is TResult typedResult)
                {
                    result = typedResult;
                }

                if (target is UnityEngine.Object unityObject)
                {
                    EditorUtility.SetDirty(unityObject);
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"调用方法 {method.Name} 时出错:\n{e.InnerException ?? e}");
                return false;
            }
        }

        #endregion

        #region 参数值与参数绘制（Rect 版）

        /// <summary>
        /// 参数字段之间的竖直间距
        /// </summary>
        public const float ButtonParamSpacing = 2f;

        /// <summary>
        /// 获取参数类型的默认值
        /// </summary>
        public static object GetDefaultParameterValue(Type type)
        {
            if (type == null)
                return null;

            if (type == typeof(string)) return string.Empty;
            if (type == typeof(Color)) return Color.white;
            if (type == typeof(AnimationCurve)) return AnimationCurve.Linear(0f, 0f, 1f, 1f);
            if (type == typeof(Gradient)) return new Gradient();
            if (type.IsEnum) return Enum.GetValues(type).GetValue(0);
            if (type.IsValueType) return Activator.CreateInstance(type);
            return null;
        }

        /// <summary>
        /// 将值转换为目标参数类型
        /// <para>用于把特性上以 object 形式传入的默认值适配到方法签名，例如 int 字面量转换到 float 参数</para>
        /// </summary>
        /// <param name="value">原始值</param>
        /// <param name="targetType">目标参数类型</param>
        /// <param name="success">是否转换成功</param>
        public static object CoerceParameterValue(object value, Type targetType, out bool success)
        {
            success = true;

            if (targetType == null)
            {
                success = false;
                return value;
            }

            if (value == null)
                return targetType.IsValueType ? GetDefaultParameterValue(targetType) : null;

            if (targetType.IsInstanceOfType(value))
                return value;

            if (targetType.IsEnum)
            {
                if (value is string enumName)
                {
                    try
                    {
                        return Enum.Parse(targetType, enumName, true);
                    }
                    catch
                    {
                        success = false;
                        return GetDefaultParameterValue(targetType);
                    }
                }

                try
                {
                    return Enum.ToObject(targetType, value);
                }
                catch
                {
                    success = false;
                    return GetDefaultParameterValue(targetType);
                }
            }

            try
            {
                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                success = false;
                return GetDefaultParameterValue(targetType);
            }
        }

        /// <summary>
        /// 绘制单个参数输入字段（Rect 版）
        /// </summary>
        /// <returns>编辑后的值；不支持的类型返回原值</returns>
        public static object DrawParameterField(Rect rect, Type type, string name, object currentValue)
        {
            if (type == typeof(int))
                return EditorGUI.IntField(rect, name, currentValue is int intValue ? intValue : 0);
            if (type == typeof(float))
                return EditorGUI.FloatField(rect, name, currentValue is float floatValue ? floatValue : 0f);
            if (type == typeof(double))
                return EditorGUI.DoubleField(rect, name, currentValue is double doubleValue ? doubleValue : 0d);
            if (type == typeof(string))
                return EditorGUI.TextField(rect, name, currentValue as string ?? string.Empty);
            if (type == typeof(bool))
                return EditorGUI.Toggle(rect, name, currentValue is bool boolValue && boolValue);
            if (type.IsEnum)
                return EditorGUI.EnumPopup(rect, name,
                    currentValue as Enum ?? (Enum)Enum.GetValues(type).GetValue(0));

            if (type == typeof(Vector2))
                return EditorGUI.Vector2Field(rect, name, currentValue is Vector2 v2 ? v2 : Vector2.zero);
            if (type == typeof(Vector3))
                return EditorGUI.Vector3Field(rect, name, currentValue is Vector3 v3 ? v3 : Vector3.zero);
            if (type == typeof(Vector4))
                return EditorGUI.Vector4Field(rect, name, currentValue is Vector4 v4 ? v4 : Vector4.zero);
            if (type == typeof(Color))
                return EditorGUI.ColorField(rect, name, currentValue is Color color ? color : Color.white);
            if (type == typeof(Rect))
                return EditorGUI.RectField(rect, name, currentValue is Rect rectValue ? rectValue : default);
            if (type == typeof(AnimationCurve))
                return EditorGUI.CurveField(rect, name,
                    currentValue as AnimationCurve ?? AnimationCurve.Linear(0f, 0f, 1f, 1f));
            if (type == typeof(Gradient))
                return EditorGUI.GradientField(rect, name, currentValue as Gradient ?? new Gradient());

            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                return EditorGUI.ObjectField(rect, name, currentValue as UnityEngine.Object, type, true);

            EditorGUI.LabelField(rect, name, $"不支持的参数类型: {type.Name}");
            return currentValue;
        }

        /// <summary>
        /// 获取单个参数字段在 Inspector 中占用的高度
        /// </summary>
        public static float GetParameterHeight(Type type)
        {
            return IsMultiLineParameter(type)
                ? EditorGUIUtility.singleLineHeight * 2f
                : EditorGUIUtility.singleLineHeight;
        }

        /// <summary>
        /// 该类型的参数是否需要两行高度（Vector4 / Rect 会画成两行两组）
        /// </summary>
        private static bool IsMultiLineParameter(Type type)
        {
            return type == typeof(Vector4) || type == typeof(Rect);
        }

        /// <summary>
        /// 获取方法参数在 Inspector 中占用的总高度（含末尾间距，作为与按钮之间的分隔）
        /// </summary>
        public static float GetParametersHeight(ParameterInfo[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
                return 0f;

            float total = 0f;
            for (int i = 0; i < parameters.Length; i++)
                total += GetParameterHeight(parameters[i].ParameterType) + ButtonParamSpacing;

            return total;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 检查方法签名是否匹配（用于验证参数类型）
        /// </summary>
        public static bool IsMethodSignatureMatch(MethodInfo method, Type[] expectedParamTypes)
        {
            if (method == null)
                return false;

            ParameterInfo[] parameters = method.GetParameters();

            if (parameters.Length != (expectedParamTypes?.Length ?? 0))
                return false;

            if (expectedParamTypes == null)
                return true;

            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].ParameterType != expectedParamTypes[i])
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 获取方法的参数信息
        /// </summary>
        public static ParameterInfo[] GetMethodParameters(MethodInfo method)
        {
            return method?.GetParameters() ?? Array.Empty<ParameterInfo>();
        }

        #endregion
    }
}
