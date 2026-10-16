using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ToolKit;
using UnityEditor;
using UnityEngine;

namespace ToolKit.Editor
{
    public class DebugWindow : EditorWindow
    {
        // 常量定义（保持原有，略作调整）
        private const float CONTENT_PADDING_LEFT = 10f;

        private const float CONTENT_PADDING_RIGHT = 10f;
        private const float CONTENT_PADDING_TOP = 5f;
        private const float CONTENT_PADDING_BOTTOM = 5f;
        private const float COMPONENT_SPACING = 8f;
        private const float ENTRY_SPACING = 2f;

        // 颜色定义（用于美化）
        private static readonly Color ComponentHeaderColor = new Color(0.8f, 0.9f, 1f, 0.5f);

        private static readonly Color MemberLabelColor = new Color(0.7f, 0.7f, 0.7f);
        private static readonly Color PathColor = new Color(0.6f, 0.6f, 0.6f);

        private readonly List<DebugEntry> _memberEntries = new List<DebugEntry>();
        private readonly List<DebugButtonEntry> _buttonEntries = new List<DebugButtonEntry>();

        private string _searchFilter = string.Empty;
        private Vector2 _scrollPosition;
        private readonly Dictionary<string, bool> _foldoutStates = new Dictionary<string, bool>();

        // 缓存层级路径，避免重复计算
        private readonly Dictionary<GameObject, string> _hierarchyPathCache = new Dictionary<GameObject, string>();

        // 缓存带参按钮的参数输入值（跨刷新保留）
        private readonly Dictionary<(int, MethodInfo), object[]> _buttonArgs = new Dictionary<(int, MethodInfo), object[]>();

        [MenuItem(ToolGlobal.ToolKitMenuName + "Debug Window")]
        public static void OpenWindow()
        {
            var window = GetWindow<DebugWindow>("Debug Window");
            window.minSize = new Vector2(350, 400);
        }

        private void OnEnable()
        {
            Refresh();
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }

        private void OnDisable()
        {
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            _foldoutStates.Clear();
            _hierarchyPathCache.Clear();
            _buttonArgs.Clear();
        }

        private void OnHierarchyChanged()
        {
            _hierarchyPathCache.Clear(); // 层级变化时清除缓存
            Refresh();
            Repaint();
        }

        private void Refresh()
        {
            DebugSceneScanner.Scan(_memberEntries, _buttonEntries);
            _hierarchyPathCache.Clear(); // 数据变化后也清理缓存
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawContent();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Space(CONTENT_PADDING_LEFT);

            // 搜索框
            string newSearchFilter = EditorGUILayout.TextField(
                _searchFilter,
                EditorStyles.toolbarSearchField,
                GUILayout.MinWidth(150));
            if (newSearchFilter != _searchFilter)
                _searchFilter = newSearchFilter;

            GUILayout.FlexibleSpace();

            // 刷新按钮
            if (GUILayout.Button(EditorGUIUtility.TrTextContent("Refresh", "重新扫描场景"), EditorStyles.toolbarButton, GUILayout.Width(70)))
                Refresh();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawContent()
        {
            List<DebugEntry> filteredMembers = FilterMembers(_memberEntries);
            List<DebugButtonEntry> filteredButtons = FilterButtons(_buttonEntries);

            // 合并所有含调试信息的组件
            var allComponents = new HashSet<Component>();
            foreach (DebugEntry entry in filteredMembers)
            {
                if (entry.Component != null && entry.Component.gameObject != null)
                    allComponents.Add(entry.Component);
            }
            foreach (DebugButtonEntry entry in filteredButtons)
            {
                if (entry.Component != null && entry.Component.gameObject != null)
                    allComponents.Add(entry.Component);
            }

            List<Component> sortedComponents = allComponents
                .OrderBy(c => GetHierarchyPath(c.gameObject))
                .ThenBy(c => c.GetType().Name)
                .ToList();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // 添加外边框留白
            GUILayout.Space(CONTENT_PADDING_TOP);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(CONTENT_PADDING_LEFT);
            EditorGUILayout.BeginVertical();

            if (sortedComponents.Count == 0)
            {
                DrawEmptyState();
            }
            else
            {
                foreach (Component component in sortedComponents)
                {
                    DrawComponentGroup(component, filteredMembers, filteredButtons);
                }
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(CONTENT_PADDING_RIGHT);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(CONTENT_PADDING_BOTTOM);

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 绘制单个组件分组（包含标题、成员、按钮）
        /// </summary>
        private void DrawComponentGroup(Component component, List<DebugEntry> members, List<DebugButtonEntry> buttons)
        {
            if (component == null || component.gameObject == null)
                return;

            string componentKey = $"{component.gameObject.GetInstanceID()}_{component.GetInstanceID()}";
            if (!_foldoutStates.ContainsKey(componentKey))
                _foldoutStates[componentKey] = true;

            // 使用 helpBox 样式作为组件分组背景
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 绘制标题行（Foldout + 组件类型）
            EditorGUILayout.BeginHorizontal();

            // 自定义 Foldout 样式（加粗、颜色）
            var foldoutStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = EditorStyles.boldLabel.normal.textColor }
            };
            _foldoutStates[componentKey] = EditorGUILayout.Foldout(
                _foldoutStates[componentKey],
                component.GetType().Name,
                true,
                foldoutStyle);

            // 右侧显示层级路径（灰色小字），点击可 Ping GameObject
            var pathStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = PathColor }
            };
            string hierarchyPath = GetHierarchyPath(component.gameObject);
            GUILayout.FlexibleSpace();

            // 使用 Label 并检测点击事件
            Rect labelRect = GUILayoutUtility.GetRect(new GUIContent(hierarchyPath), pathStyle);
            GUI.Label(labelRect, hierarchyPath, pathStyle);

            // 检测点击
            if (Event.current.type == EventType.MouseDown && labelRect.Contains(Event.current.mousePosition))
            {
                Selection.activeGameObject = component.gameObject;
                EditorGUIUtility.PingObject(component.gameObject);
                Event.current.Use();
            }

            EditorGUILayout.EndHorizontal();

            if (!_foldoutStates[componentKey])
            {
                EditorGUILayout.EndVertical();
                return;
            }

            // 内容缩进
            //EditorGUI.indentLevel++;

            // 分隔线
            DrawSeparator();

            // 绘制成员字段
            foreach (DebugEntry entry in members)
            {
                if (entry.Component == component)
                    DrawMemberEntry(entry);
            }

            // 绘制按钮（如果有），按 Order 排序
            List<DebugButtonEntry> componentButtons = buttons
                .Where(b => b.Component == component)
                .OrderBy(b => b.Method.Attribute.Order)
                .ToList();

            if (componentButtons.Count > 0)
            {
                GUILayout.Space(ENTRY_SPACING);
                DrawSeparator();
                GUILayout.Space(ENTRY_SPACING);

                // 无分组按钮

                List<DebugButtonEntry> ungroupedButtons = componentButtons
                .Where(b => string.IsNullOrEmpty(b.Method.Attribute.Group))
                .ToList();

                if (ungroupedButtons.Count > 0)
                {
                    DrawButtonEntriesHorizontal(ungroupedButtons);
                }

                // 分组按钮：显示分组标题
                var groups = componentButtons
                    .Where(b => !string.IsNullOrEmpty(b.Method.Attribute.Group))
                    .GroupBy(b => b.Method.Attribute.Group);

                foreach (var group in groups)
                {
                    EditorGUILayout.LabelField(
                        group.Key,
                        EditorStyles.boldLabel);

                    DrawButtonEntriesHorizontal(group.ToList());
                }
            }

            //EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
            GUILayout.Space(COMPONENT_SPACING);
        }

        private void DrawButtonEntriesHorizontal(List<DebugButtonEntry> entries)
        {
            const float buttonHeight = 21f;
            const float buttonSpacing = 5f;
            const float minButtonWidth = 80f;

            Rect tempRect = EditorGUILayout.GetControlRect(false, 0f);

            float availableWidth = EditorGUIUtility.currentViewWidth
                                   - CONTENT_PADDING_LEFT
                                   - CONTENT_PADDING_RIGHT
                                   - 40f;

            List<DebugButtonEntry> currentRow = new List<DebugButtonEntry>();
            float currentRowWidth = 0f;

            foreach (DebugButtonEntry entry in entries)
            {
                if (entry.Method.HasParameters)
                {
                    FlushButtonRow( currentRow, availableWidth, buttonHeight, buttonSpacing);

                    currentRow.Clear();
                    currentRowWidth = 0f;

                    DrawButtonEntry(entry);
                    continue;
                }

                float buttonWidth = Mathf.Max(
                    minButtonWidth,
                    GUI.skin.button.CalcSize(
                        new GUIContent(entry.MethodName)).x + 20f);

                float requiredWidth = currentRow.Count == 0
                    ? buttonWidth
                    : buttonWidth + buttonSpacing;

                if (currentRowWidth + requiredWidth > availableWidth)
                {
                    FlushButtonRow( currentRow, availableWidth, buttonHeight, buttonSpacing);

                    currentRow.Clear();
                    currentRowWidth = 0f;
                }

                currentRow.Add(entry);

                currentRowWidth += currentRow.Count == 1
                    ? buttonWidth
                    : buttonWidth + buttonSpacing;
            }

            FlushButtonRow( currentRow, availableWidth, buttonHeight, buttonSpacing);
        }

        private void FlushButtonRow( List<DebugButtonEntry> entries, float availableWidth, float buttonHeight, float buttonSpacing)
        {
            if (entries.Count == 0)
                return;

            Rect rowRect = EditorGUILayout.GetControlRect(
                false,
                buttonHeight);

            rowRect.x += EditorGUI.indentLevel<0 ? 0 : EditorGUI.indentLevel * 15f;
            rowRect.width -= EditorGUI.indentLevel < 0 ? 0 : EditorGUI.indentLevel * 15f;

            // 计算所有按钮平均应该占多宽
            float totalSpacing = buttonSpacing * (entries.Count - 1);
            float buttonWidth = (rowRect.width - totalSpacing) / entries.Count;

            float x = rowRect.x;

            foreach (DebugButtonEntry entry in entries)
            {
                Rect buttonRect = new Rect(
                    x,
                    rowRect.y,
                    buttonWidth,
                    buttonHeight);

                DrawButtonInRect(buttonRect, entry);

                x += buttonWidth + buttonSpacing;
            }

            GUILayout.Space(3f);
        }

        private void DrawButtonInRect( Rect rect, DebugButtonEntry entry)
        {
            DebugMethodInfo method = entry.Method;

            Color oldBg = GUI.backgroundColor;
            GUI.backgroundColor = GetButtonColor(method.Attribute.Color);

            if (GUI.Button(rect, entry.MethodName))
            {
                method.Invoke(entry.Component, null);
            }

            GUI.backgroundColor = oldBg;
        }

        /// <summary>
        /// 绘制成员字段行，采用两列布局并处理值类型
        /// </summary>
        private void DrawMemberEntry(DebugEntry entry)
        {
            EditorGUILayout.BeginHorizontal();

            // 标签列（40% 宽度，灰色字体）
            float totalWidth = EditorGUIUtility.currentViewWidth - CONTENT_PADDING_LEFT - CONTENT_PADDING_RIGHT - 30; // 减去滚动条和内边距估算
            float labelWidth = totalWidth * 0.4f;
            float valueWidth = totalWidth * 0.6f;

            var labelStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = MemberLabelColor },
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false
            };
            EditorGUILayout.LabelField(entry.MemberName, labelStyle, GUILayout.Width(labelWidth));

            // 值列（60% 宽度，根据类型绘制控件）
            object currentValue = entry.Member.GetValue(entry.Component);
            object newValue = DrawValueField(currentValue, entry.Member.ValueType, valueWidth);

            if (!Equals(newValue, currentValue))
            {
                Undo.RecordObject(entry.Component, "Debug Change");
                entry.Member.SetValue(entry.Component, newValue);
                EditorUtility.SetDirty(entry.Component);
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(ENTRY_SPACING);
        }

        /// <summary>
        /// 绘制调试按钮行
        /// </summary>
        private void DrawButtonEntry(DebugButtonEntry entry)
        {
            DebugMethodInfo method = entry.Method;

            // 按钮颜色（参考 ButtonDrawer）
            Color oldBg = GUI.backgroundColor;
            GUI.backgroundColor = GetButtonColor(method.Attribute.Color);

            // 无参方法：直接绘制单个按钮
            if (!method.HasParameters)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(EditorGUI.indentLevel * 15f); // 与成员对齐
                if (GUILayout.Button(entry.MethodName, GUILayout.Height(24), GUILayout.ExpandWidth(true)))
                {
                    method.Invoke(entry.Component, null);
                }
                EditorGUILayout.EndHorizontal();
                GUI.backgroundColor = oldBg;
                return;
            }

            // 有参方法：参考 ButtonDrawer，用 box 包裹参数输入区与按钮
            object[] args = GetOrCreateArgs(entry);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(entry.MethodName, EditorStyles.boldLabel);

            ParameterInfo[] parameters = method.Parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(parameters[i].Name, GUILayout.Width(80));
                args[i] = DrawValueField(args[i], parameters[i].ParameterType);
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Invoke", GUILayout.Height(24)))
            {
                method.Invoke(entry.Component, args);
            }

            EditorGUILayout.EndVertical();

            GUI.backgroundColor = oldBg;
        }

        /// <summary>
        /// 获取或创建某按钮的参数输入缓存（跨刷新保留）。
        /// </summary>
        private object[] GetOrCreateArgs(DebugButtonEntry entry)
        {
            var key = (entry.Component.GetInstanceID(), entry.Method.Method);
            if (_buttonArgs.TryGetValue(key, out object[] args))
                return args;

            args = entry.Method.Parameters
                .Select(p => GetDefaultValue(p.ParameterType))
                .ToArray();
            _buttonArgs[key] = args;
            return args;
        }

        /// <summary>
        /// 获取参数类型的默认值。
        /// </summary>
        private static object GetDefaultValue(Type type)
        {
            if (type == typeof(string)) return "";
            if (type == typeof(int)) return 0;
            if (type == typeof(float)) return 0f;
            if (type == typeof(double)) return 0d;
            if (type == typeof(long)) return 0L;
            if (type == typeof(bool)) return false;
            if (type.IsEnum) return Enum.GetValues(type).GetValue(0);
            if (type == typeof(Vector2)) return Vector2.zero;
            if (type == typeof(Vector3)) return Vector3.zero;
            if (type == typeof(Vector4)) return Vector4.zero;
            if (type == typeof(Color)) return Color.white;
            if (type.IsValueType) return Activator.CreateInstance(type);
            return null;
        }

        /// <summary>
        /// 将 ButtonColor 枚举映射到 Unity Color（参考 ButtonDrawer）。
        /// </summary>
        private static Color GetButtonColor(ButtonColor color) => color switch
        {
            ButtonColor.Green => new Color(0.3f, 0.8f, 0.3f),
            ButtonColor.Red => new Color(0.9f, 0.2f, 0.2f),
            ButtonColor.Yellow => new Color(0.9f, 0.8f, 0.1f),
            ButtonColor.Blue => new Color(0.2f, 0.5f, 0.9f),
            ButtonColor.White => Color.white,
            _ => Color.white
        };

        /// <summary>
        /// 绘制值字段，支持多种类型
        /// </summary>
        private object DrawValueField(object value, Type valueType, float width = 0)
        {
            var options = width > 0 ? new[] { GUILayout.Width(width) } : new GUILayoutOption[] { };

            if (valueType == typeof(int))
                return EditorGUILayout.IntField(value != null ? (int)value : 0, options);
            if (valueType == typeof(float))
                return EditorGUILayout.FloatField(value != null ? (float)value : 0f, options);
            if (valueType == typeof(double))
                return EditorGUILayout.DoubleField(value != null ? (double)value : 0d, options);
            if (valueType == typeof(long))
                return EditorGUILayout.LongField(value != null ? (long)value : 0L, options);
            if (valueType == typeof(bool))
                return EditorGUILayout.Toggle(value != null && (bool)value, options);
            if (valueType == typeof(string))
                return EditorGUILayout.TextField(value != null ? (string)value : "", options);
            if (valueType == typeof(Vector2))
                return EditorGUILayout.Vector2Field("", value != null ? (Vector2)value : Vector2.zero, options);
            if (valueType == typeof(Vector3))
                return EditorGUILayout.Vector3Field("", value != null ? (Vector3)value : Vector3.zero, options);
            if (valueType == typeof(Vector4))
                return EditorGUILayout.Vector4Field("", value != null ? (Vector4)value : Vector4.zero, options);
            if (valueType == typeof(Color))
                return EditorGUILayout.ColorField(value != null ? (Color)value : Color.white, options);
            if (valueType.IsEnum)
                return EditorGUILayout.EnumPopup(value as Enum ?? (Enum)Enum.ToObject(valueType, 0), options);
            if (typeof(UnityEngine.Object).IsAssignableFrom(valueType))
                return EditorGUILayout.ObjectField(value as UnityEngine.Object, valueType, true, options);

            // 不支持的类型显示提示
            EditorGUILayout.LabelField($"Unsupported: {valueType.Name}", EditorStyles.miniLabel, options);
            return value;
        }

        /// <summary>
        /// 绘制水平分隔线
        /// </summary>
        private void DrawSeparator()
        {
            GUILayout.Space(2f);
            Rect rect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.4f));
            GUILayout.Space(2f);
        }

        /// <summary>
        /// 绘制无数据提示
        /// </summary>
        private void DrawEmptyState()
        {
            GUILayout.Space(20);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(10);
            GUILayout.Label("没有找到调试数据", EditorStyles.boldLabel);
            GUILayout.Space(5);
            GUILayout.Label("请检查场景中是否有使用 [DebugData] 或 [DebugButton] 属性的组件。", EditorStyles.miniLabel);
            GUILayout.Space(10);
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(20);
        }

        private List<DebugEntry> FilterMembers(List<DebugEntry> entries)
        {
            if (string.IsNullOrEmpty(_searchFilter))
                return entries;

            string lowerFilter = _searchFilter.ToLower();
            return entries.Where(e =>
                e.GameObjectName.ToLower().Contains(lowerFilter) ||
                e.ComponentTypeName.ToLower().Contains(lowerFilter) ||
                e.MemberName.ToLower().Contains(lowerFilter) ||
                GetHierarchyPath(e.Component.gameObject).ToLower().Contains(lowerFilter)
            ).ToList();
        }

        private List<DebugButtonEntry> FilterButtons(List<DebugButtonEntry> entries)
        {
            if (string.IsNullOrEmpty(_searchFilter))
                return entries;

            string lowerFilter = _searchFilter.ToLower();
            return entries.Where(e =>
                e.GameObjectName.ToLower().Contains(lowerFilter) ||
                e.ComponentTypeName.ToLower().Contains(lowerFilter) ||
                e.MethodName.ToLower().Contains(lowerFilter) ||
                GetHierarchyPath(e.Component.gameObject).ToLower().Contains(lowerFilter)
            ).ToList();
        }

        private string GetHierarchyPath(GameObject gameObject)
        {
            if (gameObject == null) return "(已销毁)";

            if (_hierarchyPathCache.TryGetValue(gameObject, out string cachedPath))
                return cachedPath;

            string path = gameObject.name;
            Transform parent = gameObject.transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            _hierarchyPathCache[gameObject] = path;
            return path;
        }
    }
}