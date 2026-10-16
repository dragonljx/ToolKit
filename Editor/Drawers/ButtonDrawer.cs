using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ToolKit.Editor
{
    /// <summary>
    /// 自定义 Inspector，为带有 ButtonAttribute 的方法绘制可点击按钮，并支持参数输入。
    /// </summary>
    [CustomEditor(typeof(UnityEngine.Object), true)]
    public class ButtonDrawer : UnityEditor.Editor
    {
        // 缓存：每种类型的方法信息 + 参数默认值，避免每帧反射
        private static readonly Dictionary<Type, List<ButtonMethod>> MethodCache = new();

        // 存储当前实例的参数临时值（用于有参数的方法）
        private readonly Dictionary<MethodInfo, object[]> _methodArgs = new();

        // 用于标记是否需要重新扫描方法（当目标对象发生变化时）
        private bool _needsRefresh = true;
        private List<ButtonMethod> _cachedMethods;

        private void OnEnable()
        {
            _needsRefresh = true; // 切换目标时强制刷新
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            // 延迟加载并缓存方法信息（性能优化）
            if (_needsRefresh || _cachedMethods == null)
            {
                RefreshMethodCache();
                _needsRefresh = false;
            }

            if (_cachedMethods == null || _cachedMethods.Count == 0)
                return;

            // 按 Group 分组
            var groups = _cachedMethods.Where(m => !string.IsNullOrEmpty(m.Attribute.Group))
                                       .GroupBy(m => m.Attribute.Group);
            var noGroupMethods = _cachedMethods.Where(m => string.IsNullOrEmpty(m.Attribute.Group));

            // 绘制无分组按钮
            foreach (var method in noGroupMethods)
            {
                EditorGUILayout.Space(method.Attribute.Spacing);  // 使用特性的 Spacing 值
                DrawButton(method);
            }

            // 绘制分组按钮（水平排列）
            foreach (var group in groups)
            {
                var groupMethods = group.ToList();
                if (groupMethods.Count == 0) continue;

                // 使用第一个按钮的 Spacing（组内按钮应该有相同的间距）
                EditorGUILayout.Space(groupMethods[0].Attribute.Spacing);
                EditorGUILayout.LabelField(group.Key, EditorStyles.boldLabel);

                // 使用水平布局，但自动换行（当按钮过多时）
                EditorGUILayout.BeginHorizontal();
                foreach (var method in groupMethods)
                {
                    DrawButton(method);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// 刷新当前目标类型的方法缓存
        /// </summary>
        private void RefreshMethodCache()
        {
            var targetType = target.GetType();

            if (!MethodCache.TryGetValue(targetType, out var methods))
            {
                // 首次扫描该类型，缓存所有符合条件的 MethodInfo
                methods = targetType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                BindingFlags.Instance | BindingFlags.Static)
                                    .Select(m => new
                                    {
                                        Method = m,
                                        Attribute = m.GetCustomAttribute<ButtonAttribute>()
                                    })
                                    .Where(x => x.Attribute != null)
                                    .OrderBy(x => x.Attribute.Order)
                                    .Select(x => new ButtonMethod(x.Method, x.Attribute))
                                    .ToList();

                MethodCache[targetType] = methods;
            }

            _cachedMethods = methods;
        }

        /// <summary>
        /// 绘制单个按钮（包含参数输入区域）
        /// </summary>
        private void DrawButton(ButtonMethod method)
        {
            var attr = method.Attribute;
            var mi = method.MethodInfo;
            var parameters = ButtonRenderUtility.GetMethodParameters(mi);
            var hasParams = parameters.Length > 0;

            // 检查 ButtonMode，判断按钮是否应该启用
            bool isEnabled = IsButtonEnabled(attr.Mode);

            // 按钮颜色（使用工具类）
            GUI.backgroundColor = ButtonRenderUtility.GetButtonColor(attr.Color);

            // 按钮标题（使用自定义名称或方法名）
            var title = string.IsNullOrEmpty(attr.Name) ? mi.Name : attr.Name;

            // 如果有参数，则用 Box 包裹参数输入区，并显示方法名作为标题
            if (hasParams)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

                // 获取或创建参数值缓存
                if (!_methodArgs.TryGetValue(mi, out var args))
                {
                    args = parameters.Select(p => GetDefaultValue(p.ParameterType)).ToArray();
                    _methodArgs[mi] = args;
                }

                // 禁用状态下参数字段也不可编辑
                using (new EditorGUI.DisabledScope(!isEnabled))
                {
                    // 绘制每个参数输入字段
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var param = parameters[i];
                        args[i] = DrawParameterField(param.ParameterType, param.Name, args[i]);
                    }

                    // 按钮放在参数下方，使用特性指定的高度和宽度
                    GUILayoutOption[] options = GetButtonLayoutOptions(attr);
                    if (GUILayout.Button("Click", options))
                    {
                        ButtonRenderUtility.InvokeMethod(mi, target, args);
                    }
                }

                EditorGUILayout.EndVertical();
            }
            else
            {
                // 无参数：直接绘制一个按钮
                using (new EditorGUI.DisabledScope(!isEnabled))
                {
                    GUILayoutOption[] options = GetButtonLayoutOptions(attr);
                    if (GUILayout.Button(title, options))
                    {
                        ButtonRenderUtility.InvokeMethod(mi, target, null);
                    }
                }
            }

            // 恢复颜色
            GUI.backgroundColor = Color.white;
        }

        /// <summary>
        /// 根据 ButtonMode 判断按钮是否应该启用
        /// </summary>
        private bool IsButtonEnabled(ButtonMode mode)
        {
            return mode switch
            {
                ButtonMode.AlwaysEnabled => true,
                ButtonMode.EnabledInPlayMode => Application.isPlaying,
                ButtonMode.DisabledInPlayMode => !Application.isPlaying,
                _ => true
            };
        }

        /// <summary>
        /// 根据特性配置生成 GUILayout 选项（高度和宽度）
        /// </summary>
        private GUILayoutOption[] GetButtonLayoutOptions(ButtonAttribute attr)
        {
            var options = new List<GUILayoutOption>();

            // 高度
            if (attr.ButtonHeight > 0)
            {
                options.Add(GUILayout.Height(attr.ButtonHeight));
            }

            // 宽度
            if (attr.ButtonWidth > 0)
            {
                // 固定宽度
                options.Add(GUILayout.Width(attr.ButtonWidth));
            }
            else if (attr.ButtonWidth == 0)
            {
                // 自适应宽度
                options.Add(GUILayout.ExpandWidth(false));
            }
            // else: -1 = 占满整行（默认行为，不需要添加选项）

            return options.ToArray();
        }

        /// <summary>
        /// 绘制单个参数输入字段（支持更多类型）
        /// </summary>
        private object DrawParameterField(Type type, string name, object currentValue)
        {
            // 基本类型
            if (type == typeof(int))
                return EditorGUILayout.IntField(name, (int)currentValue);
            if (type == typeof(float))
                return EditorGUILayout.FloatField(name, (float)currentValue);
            if (type == typeof(string))
                return EditorGUILayout.TextField(name, (string)currentValue);
            if (type == typeof(bool))
                return EditorGUILayout.Toggle(name, (bool)currentValue);
            if (type.IsEnum)
                return EditorGUILayout.EnumPopup(name, (Enum)currentValue);

            // Unity 常用类型扩展
            if (type == typeof(Vector2))
                return EditorGUILayout.Vector2Field(name, (Vector2)currentValue);
            if (type == typeof(Vector3))
                return EditorGUILayout.Vector3Field(name, (Vector3)currentValue);
            if (type == typeof(Vector4))
                return EditorGUILayout.Vector4Field(name, (Vector4)currentValue);
            if (type == typeof(Color))
                return EditorGUILayout.ColorField(name, (Color)currentValue);
            if (type == typeof(AnimationCurve))
                return EditorGUILayout.CurveField(name, (AnimationCurve)currentValue);
            if (type == typeof(Gradient))
                return EditorGUILayout.GradientField(name, (Gradient)currentValue);
            if (type == typeof(UnityEngine.Object) || type.IsSubclassOf(typeof(UnityEngine.Object)))
                return EditorGUILayout.ObjectField(name, (UnityEngine.Object)currentValue, type, true);

            // 其他类型（如结构体）尝试用默认字段绘制
            try
            {
                // 使用 Unity 的默认 Inspector 绘制（仅支持可序列化类型）
                return EditorGUILayout.PropertyField(FindPropertyInTarget(name), true);
            }
            catch
            {
                EditorGUILayout.LabelField(name, $"不支持的参数类型: {type.Name}");
                return currentValue;
            }
        }

        /// <summary>
        /// 辅助：在目标对象上查找属性（用于复杂类型绘制）
        /// </summary>
        private SerializedProperty FindPropertyInTarget(string propertyName)
        {
            // 由于没有对应序列化属性，简单返回一个虚拟属性（实际不推荐，仅做兼容）
            // 更好的做法是让用户将参数封装为可序列化类，此处简化为直接返回 null
            return null;
        }

        /// <summary>
        /// 获取参数类型的默认值
        /// </summary>
        private object GetDefaultValue(Type type)
        {
            if (type == typeof(string)) return "";
            if (type == typeof(int)) return 0;
            if (type == typeof(float)) return 0f;
            if (type == typeof(bool)) return false;
            if (type.IsEnum) return Enum.GetValues(type).GetValue(0);
            if (type == typeof(Vector2)) return Vector2.zero;
            if (type == typeof(Vector3)) return Vector3.zero;
            if (type == typeof(Vector4)) return Vector4.zero;
            if (type == typeof(Color)) return Color.white;
            if (type == typeof(AnimationCurve)) return AnimationCurve.Linear(0, 0, 1, 1);
            if (type == typeof(Gradient)) return new Gradient();
            if (type.IsValueType) return Activator.CreateInstance(type);
            return null;
        }

        /// <summary>
        /// 内部包装类,缓存方法和属性
        /// </summary>
        private class ButtonMethod
        {
            public MethodInfo MethodInfo { get; }
            public ButtonAttribute Attribute { get; }

            public ButtonMethod(MethodInfo method, ButtonAttribute attr)
            {
                MethodInfo = method;
                Attribute = attr;
            }
        }
    }
}