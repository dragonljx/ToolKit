using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ToolKit.Editor
{
    /// <summary>
    /// PropertyButtonAttribute 的属性绘制器
    /// <para>基于 PropertyDrawer，即使有 CustomEditor 也会被调用</para>
    /// <para>方法是否带参数由反射自动识别，带参数时会在按钮上方生成可编辑的参数输入字段</para>
    /// </summary>
    [CustomPropertyDrawer(typeof(PropertyButtonAttribute), true)]
    public class PropertyButtonDrawer : PropertyDrawer
    {
        /// <summary>块与块之间的竖直间距</summary>
        private const float BlockSpacing = 2f;

        /// <summary>按钮与属性横向并排时，按钮与属性之间的间距</summary>
        private const float HorizontalGap = 4f;

        /// <summary>
        /// 横向并排（Left / Right）且未指定宽度时按钮的默认宽度
        /// </summary>
        private const float DefaultHorizontalButtonWidth = 80f;

        /// <summary>
        /// 参数值缓存的清理阈值：超过该条数时剔除目标对象已销毁的条目
        /// </summary>
        private const int ParamsCachePruneThreshold = 128;

        /// <summary>
        /// 参数值缓存，key 为 "实例ID|属性路径|方法名"
        /// <para>必须带上实例标识，否则同一类型的多个对象会共用同一组参数值而互相串档</para>
        /// </summary>
        private static readonly Dictionary<string, ParamEntry> ParamsCache = new();

        /// <summary>
        /// 域重载时清空缓存（关闭 Domain Reload 时静态字典不会被自动清空）
        /// </summary>
        [InitializeOnLoadMethod]
        private static void ClearCacheOnDomainReload()
        {
            ParamsCache.Clear();
        }

        #region 高度计算

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            PropertyButtonAttribute attr = (PropertyButtonAttribute)attribute;

            MethodInfo method = ButtonRenderUtility.FindMethod(
                property.serializedObject.targetObject.GetType(), attr.MethodName);

            // 方法未找到时只需要容纳错误提示
            if (method == null)
                return EditorGUIUtility.singleLineHeight * 2f + BlockSpacing;

            float propertyHeight = GetPropertyBlockHeight(property, label, attr);
            float paramHeight = ButtonRenderUtility.GetParametersHeight(
                ButtonRenderUtility.GetMethodParameters(method));

            switch (attr.Position)
            {
                case ButtonPosition.Above:
                default:
                    return paramHeight + attr.ButtonHeight
                        + (attr.DrawProperty ? BlockSpacing + propertyHeight : 0f);

                case ButtonPosition.Below:
                    return (attr.DrawProperty ? propertyHeight + BlockSpacing : 0f)
                        + paramHeight + attr.ButtonHeight;

                case ButtonPosition.Left:
                case ButtonPosition.Right:
                    return paramHeight + Mathf.Max(propertyHeight, attr.ButtonHeight);
            }
        }

        /// <summary>
        /// 属性块自身占用的高度（0 表示不绘制属性）
        /// </summary>
        private static float GetPropertyBlockHeight(SerializedProperty property, GUIContent label, PropertyButtonAttribute attr)
        {
            return attr.DrawProperty ? EditorGUI.GetPropertyHeight(property, label, true) : 0f;
        }

        #endregion

        #region 绘制

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            PropertyButtonAttribute attr = (PropertyButtonAttribute)attribute;

            // 获取目标对象
            UnityEngine.Object targetObject = property.serializedObject.targetObject;
            MethodInfo method = ButtonRenderUtility.FindMethod(targetObject.GetType(), attr.MethodName);

            if (method == null)
            {
                EditorGUI.HelpBox(position, $"未找到方法: {attr.MethodName}", MessageType.Error);
                return;
            }

            ParameterInfo[] parameters = ButtonRenderUtility.GetMethodParameters(method);
            object[] paramValues = parameters.Length > 0
                ? GetParamValues(attr, property, targetObject, parameters)
                : null;

            float propertyHeight = GetPropertyBlockHeight(property, label, attr);
            float width = position.width;
            float y = position.y;

            // 除 Below 之外的布局都把参数区放在最上方（Left/Right 时参数区独占整行，避免与横向分割争空间）
            if (paramValues != null && attr.Position != ButtonPosition.Below)
                DrawParamBlock(position.x, ref y, width, parameters, paramValues);

            switch (attr.Position)
            {
                case ButtonPosition.Above:
                default:
                    DrawButtonBlock(attr, method, targetObject, paramValues,
                        new Rect(position.x, y, ResolveButtonWidth(attr.ButtonText, attr.ButtonWidth, width, false), attr.ButtonHeight));
                    y += attr.ButtonHeight;

                    if (attr.DrawProperty)
                        EditorGUI.PropertyField(
                            new Rect(position.x, y + BlockSpacing, width, propertyHeight), property, label, true);
                    break;

                case ButtonPosition.Below:
                    if (attr.DrawProperty)
                    {
                        EditorGUI.PropertyField(new Rect(position.x, y, width, propertyHeight), property, label, true);
                        y += propertyHeight + BlockSpacing;
                    }

                    if (paramValues != null)
                        DrawParamBlock(position.x, ref y, width, parameters, paramValues);

                    DrawButtonBlock(attr, method, targetObject, paramValues,
                        new Rect(position.x, y, ResolveButtonWidth(attr.ButtonText, attr.ButtonWidth, width, false), attr.ButtonHeight));
                    break;

                case ButtonPosition.Left:
                case ButtonPosition.Right:
                    float buttonWidth = ResolveButtonWidth(attr.ButtonText, attr.ButtonWidth, width, true);
                    float propertyWidth = Mathf.Max(0f, width - buttonWidth - HorizontalGap);

                    Rect buttonRect;
                    Rect propertyRect;

                    if (attr.Position == ButtonPosition.Left)
                    {
                        buttonRect = new Rect(position.x, y, buttonWidth, attr.ButtonHeight);
                        propertyRect = new Rect(position.x + buttonWidth + HorizontalGap, y, propertyWidth, propertyHeight);
                    }
                    else
                    {
                        propertyRect = new Rect(position.x, y, propertyWidth, propertyHeight);
                        buttonRect = new Rect(propertyRect.xMax + HorizontalGap, y, buttonWidth, attr.ButtonHeight);
                    }

                    DrawButtonBlock(attr, method, targetObject, paramValues, buttonRect);

                    if (attr.DrawProperty)
                        EditorGUI.PropertyField(propertyRect, property, label, true);
                    break;
            }
        }

        /// <summary>
        /// 绘制参数输入区，并把 <paramref name="y"/> 推进到参数区之后
        /// </summary>
        private static void DrawParamBlock(float x, ref float y, float width, ParameterInfo[] parameters, object[] values)
        {
            float lineY = y;

            for (int i = 0; i < parameters.Length; i++)
            {
                Type parameterType = parameters[i].ParameterType;
                float parameterHeight = ButtonRenderUtility.GetParameterHeight(parameterType);

                Rect rect = new Rect(x, lineY, width, parameterHeight);
                values[i] = ButtonRenderUtility.DrawParameterField(
                    rect, parameterType, parameters[i].Name, values[i]);

                lineY += parameterHeight + ButtonRenderUtility.ButtonParamSpacing;
            }

            // 与 GetParametersHeight 保持一致：末尾间距计入参数区
            y = lineY;
        }

        /// <summary>
        /// 绘制按钮，被点击时调用目标方法
        /// </summary>
        private static void DrawButtonBlock(
            PropertyButtonAttribute attr, MethodInfo method, object targetObject, object[] paramValues, Rect buttonRect)
        {
            if (ButtonRenderUtility.DrawButton(buttonRect, attr.ButtonText, attr.Color))
                ButtonRenderUtility.InvokeMethod(method, targetObject, paramValues);
        }

        /// <summary>
        /// 解析按钮宽度
        /// </summary>
        /// <param name="text">按钮文本（自适应宽度时需要）</param>
        /// <param name="configured">特性上配置的宽度：&lt;0 用默认，0 自适应，&gt;0 固定</param>
        /// <param name="available">可用总宽度</param>
        /// <param name="horizontalSplit">是否与属性横向并排</param>
        private static float ResolveButtonWidth(string text, float configured, float available, bool horizontalSplit)
        {
            if (configured > 0f)
                return Mathf.Min(configured, available);

            if (configured == 0f)
                return Mathf.Min(EditorStyles.miniButton.CalcSize(new GUIContent(text)).x + 8f, available);

            // configured < 0：Above/Below 占满整行；Left/Right 没有"整行"可占，退回默认宽度
            return horizontalSplit ? Mathf.Min(DefaultHorizontalButtonWidth, available) : available;
        }

        #endregion

        #region 参数值缓存

        /// <summary>
        /// 参数值缓存条目
        /// <para>用弱引用持有目标对象，既能在对象销毁后判定条目失效，又不会阻止对象被回收</para>
        /// </summary>
        private sealed class ParamEntry
        {
            private readonly WeakReference<UnityEngine.Object> _target;

            public readonly object[] Values;

            public ParamEntry(UnityEngine.Object target, object[] values)
            {
                _target = new WeakReference<UnityEngine.Object>(target);
                Values = values;
            }

            /// <summary>目标对象是否仍然存活（弱引用同时挡掉了实例 ID 被复用的情况）</summary>
            public bool IsAlive => _target.TryGetTarget(out UnityEngine.Object target) && target != null;
        }

        private static void PruneCacheIfNeeded()
        {
            if (ParamsCache.Count < ParamsCachePruneThreshold)
                return;

            List<string> deadKeys = null;
            foreach (KeyValuePair<string, ParamEntry> pair in ParamsCache)
            {
                if (pair.Value.IsAlive)
                    continue;

                deadKeys ??= new List<string>();
                deadKeys.Add(pair.Key);
            }

            if (deadKeys == null)
                return;

            foreach (string key in deadKeys)
                ParamsCache.Remove(key);
        }

        /// <summary>
        /// 获取（或初始化）某个按钮的参数值
        /// </summary>
        private static object[] GetParamValues(
            PropertyButtonAttribute attr, SerializedProperty property, UnityEngine.Object target, ParameterInfo[] parameters)
        {
            string cacheKey = $"{target.GetInstanceID()}|{property.propertyPath}|{attr.MethodName}";

            if (ParamsCache.TryGetValue(cacheKey, out ParamEntry entry) && entry.IsAlive)
                return entry.Values;

            PruneCacheIfNeeded();

            object[] values = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];

                if (i >= attr.DefaultParams.Length)
                {
                    values[i] = ButtonRenderUtility.GetDefaultParameterValue(parameter.ParameterType);
                    continue;
                }

                values[i] = ButtonRenderUtility.CoerceParameterValue(
                    attr.DefaultParams[i], parameter.ParameterType, out bool success);

                if (!success)
                {
                    Debug.LogWarning(
                        $"[PropertyButton] {target.GetType().Name}.{attr.MethodName} 的第 {i + 1} 个参数" +
                        $"（{parameter.ParameterType.Name} {parameter.Name}）默认值类型不匹配，已改用类型默认值。");
                }
            }

            ParamsCache[cacheKey] = new ParamEntry(target, values);
            return values;
        }

        #endregion
    }
}
