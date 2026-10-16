using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ToolKit.Editor
{
    /// <summary>
    /// 本包在 Unity 顶部菜单栏注册的 <c>Tool Kit/</c> 菜单项集合
    /// <para>用法：直接在菜单栏点选，不需要在代码中调用</para>
    /// <code>
    /// // 菜单结构：
    /// // Tool Kit/
    /// //   ├ 创建新插件         打开创建 UPM Package 的窗口
    /// //   ├ 数值调试窗口        打开运行时数值监视面板
    /// //   ├ 图标合集           浏览与导出 Unity 内置图标
    /// //   └ 打开数据存储目录     在文件管理器中打开 persistentDataPath
    /// //
    /// // GameObject/
    /// //   └ Tool Kit/
    /// //       └ 查找引用该对象的脚本   查找场景中哪些脚本引用了当前选中对象
    /// </code>
    /// <para>菜单路径全部取自 <c>ToolGlobal</c> 的常量，不硬编码字符串，方便统一改名</para>
    /// </summary>
    /// <remarks>
    /// <para>关于菜单排序：前三项通过 <c>priority</c> 取负值（-1003 ~ -1001）排在菜单靠前的位置，
    /// 并按 1 的间隔排列以保留插入空间。</para>
    /// <para>本类只做菜单入口与窗口打开，具体功能都在各自的窗口类里实现。</para>
    /// <para>关于「查找引用该对象的脚本」的性能：它遍历 <c>Resources.FindObjectsOfTypeAll&lt;GameObject&gt;</c>
    /// 并对每个组件创建 <c>SerializedObject</c> 逐个属性检查，属于全场景扫描，
    /// 大项目下可能耗时较久，界面上看不出进度。</para>
    /// </remarks>
    public static class EditorMenu
    {
        /// <summary>
        /// 打开创建新插件窗口
        /// <para>菜单项显示为「创建新插件」</para>
        /// </summary>
        /// <remarks>
        /// <para>方法名 <c>ShowExample</c> 是历史遗留的命名，与实际功能（创建 Package）无关。</para>
        /// <para>用 <c>GetWindow</c> 而非 <c>CreateWindow</c>，因此重复点击会聚焦到已打开的窗口。</para>
        /// </remarks>
        [MenuItem(ToolGlobal.ToolKitMenuName + "创建新插件", priority = -1003)]
        public static void ShowExample()
        {
            CreatePackage wnd = EditorWindow.GetWindow<CreatePackage>();
            wnd.titleContent = new GUIContent("CreatePackage");
        }

        /// <summary>
        /// 打开运行时数值调试窗口
        /// <para>配合 <c>DebugValueWindow.ShowValue</c> 使用，需要先打开窗口再推送数值</para>
        /// </summary>
        [MenuItem(ToolGlobal.ToolKitMenuName + "数值调试窗口", priority = -1002)]
        public static void ShowDebugValueWindow()
        {
            var wnd = EditorWindow.GetWindow<DebugValueWindow>();
            wnd.titleContent = new GUIContent("Debug Value Window");
        }

        /// <summary>
        /// 打开 Unity 内置图标浏览窗口
        /// <para>先取已有的窗口实例，没有时再新建，因此重复点击不会开出多个窗口</para>
        /// </summary>
        /// <remarks>
        /// <para>内部保留了 <c>UNITY_2018</c> 的条件分支：老版本用 <c>GetWindow</c>，
        /// 新版本用 <c>CreateWindow</c> 并手动 <c>ShowUtility</c> 以浮动面板形式显示。</para>
        /// </remarks>
        [MenuItem(ToolGlobal.ToolKitMenuName + "图标合集", priority = -1001)]
        public static void EditorIconsOpen()
        {
#if UNITY_2018
            var w = GetWindow<EditorIcons>("Editor Icons");
#else
            var w = EditorWindow.CreateWindow<EditorIcons>("Editor Icons");
#endif
            w.ShowUtility();
            w.minSize = new Vector2(320, 450);
        }

        /// <summary>
        /// 在系统文件管理器中打开 <c>Application.persistentDataPath</c>
        /// <para>用于快速查看存档、下载缓存等运行时产生的文件</para>
        /// </summary>
        /// <remarks>
        /// <para>通过 <c>System.Diagnostics.Process.Start</c> 调用系统默认程序打开目录，
        /// 路径不存在时不会自动创建，部分平台会静默失败。</para>
        /// <para>注意打开的是<strong>编辑器当前平台</strong>的 persistentDataPath，
        /// 而不是目标设备上的位置。</para>
        /// </remarks>
        [MenuItem(ToolGlobal.ToolKitMenuName + "打开数据存储目录")]
        public static void OpenPersistentDataPath()
        {
            string path = Application.persistentDataPath;
            ToolKit.DLogger.Log("打开路径: " + path);

            System.Diagnostics.Process.Start(path);
        }

        /// <summary>
        /// 一次引用查找的结果记录，保存「哪个对象的哪个组件的哪个字段」引用了目标
        /// </summary>
        private struct ReferencesInfo
        {
            /// <summary>持有引用的 GameObject</summary>
            public GameObject obj;

            /// <summary>持有引用的组件</summary>
            public Component component;

            /// <summary>引用所在的序列化字段</summary>
            public SerializedProperty property;
        }

        /// <summary>
        /// 查找场景中所有引用了当前选中 GameObject 的脚本与字段
        /// <para>菜单项位于 <c>GameObject/Tool Kit/</c>，需要先在 Hierarchy 中选中目标对象</para>
        /// <code>
        /// // 使用方式：
        /// // 1. 在 Hierarchy 中选中要排查的对象
        /// // 2. 右键 → Tool Kit/查找引用该对象的脚本
        /// // 3. 结果以带颜色标记的日志输出在 Console，点击日志可定位到对应对象
        /// </code>
        /// </summary>
        /// <remarks>
        /// <para>关于判定范围：只有当引用的对象<strong>本身就是目标</strong>，
        /// 或是目标身上的组件（含 <c>Transform</c> / <c>RectTransform</c>）时才算命中。</para>
        /// <para>递归引用、通过名字查找的间接依赖都无法被发现，它查的是序列化字段的直接引用。</para>
        /// <para>关于只找到一处时的行为：会额外 <c>PingObject</c> 定位过去，
        /// 多处引用时只在 Console 输出列表，不自动跳转。</para>
        /// <para>关于内部变量命名：方法里的 <c>hasFoundReference</c> 初始为 true、
        /// 每命中一处就置为 false，语义其实是「还没找到任何引用」，
        /// 读代码时不要被名字误导。</para>
        /// <para>关于性能：会对场景内每个组件的每个可见序列化属性做检查，属于全量扫描，
        /// 场景对象很多时可能卡顿；隐藏属性（<c>NextVisible(false)</c>）会被跳过。</para>
        /// </remarks>
        [MenuItem(ToolGlobal.GameObjectMenuName + "查找引用该对象的脚本")]
        private static void FindReferencesFromContext()
        {
            GameObject target = Selection.activeGameObject;
            if (target == null)
            {
                EditorUtility.DisplayDialog("提示", "请先在 Hierarchy 中选择一个对象", "确定");
                return;
            }

            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            List<ReferencesInfo> infos = new List<ReferencesInfo>();
            bool hasFoundReference = true;

            foreach (var obj in allObjects)
            {
                if (obj == target) continue;
                Component[] components = obj.GetComponents<Component>();
                foreach (var component in components)
                {
                    if (component == null)
                        continue;

                    // 使用 SerializedObject 检查序列化字段
                    SerializedObject serializedObject = new SerializedObject(component);
                    SerializedProperty property = serializedObject.GetIterator();

                    while (property.NextVisible(true))
                    {
                        // 检查是否为 GameObject 或 Component 引用
                        if (property.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            Object reference = property.objectReferenceValue;

                            if (reference == null)
                                continue;

                            // 检查是否引用了目标对象或其组件
                            if (IsReferenceToTarget(reference, target))
                            {
                                ReferencesInfo info = new ReferencesInfo() { component = component, obj = obj, property = property };
                                infos.Add(info);

                                hasFoundReference = false;
                                break;
                            }
                        }
                    }
                }
            }
            if (hasFoundReference)
            {
                Debug.Log($"没有找到任何引用{target.name}的对象");
            }
            else
            {

                if (infos.Count == 1)
                {
                    EditorGUIUtility.PingObject(infos[0].obj);
                }
                foreach (var item in infos)
                {
                    Debug.Log(
                        $"<color=#7CFF6B><b>{target.name}</b></color> 被 " +
                        $"<color=#FFD166>{item.obj.name}</color> 上的 " +
                        $"<color=#66D9FF>{item.component.GetType().Name}</color> 组件中的 " +
                        $"<color=#FF7EB6>{item.property.propertyPath}</color> 字段引用",
                        item.obj
                    );
                }
            }
        }

        /// <summary>
        /// 判断一个引用是否指向目标 GameObject
        /// </summary>
        /// <param name="reference">序列化字段里保存的对象引用</param>
        /// <param name="target">要匹配的目标对象</param>
        /// <returns>引用的就是目标本身，或目标身上的组件时返回 true</returns>
        /// <remarks>
        /// <para>逐条列出了 <c>GameObject</c> / <c>Component</c> / <c>Transform</c> / <c>RectTransform</c>
        /// 几种可能，其中后两者其实是 <c>Component</c> 的子类，判断可以合并，
        /// 这里保留展开写法是为了让意图更直白。</para>
        /// <para>引用被销毁对象时 <c>==</c> 会因 Unity 的假 null 语义返回 true（即视为相等），
        /// 这可能让已销毁对象的残留引用被误判为命中。</para>
        /// </remarks>
        private static bool IsReferenceToTarget(Object reference, GameObject target)
        {
            // 直接引用 GameObject
            if (reference is GameObject go && go == target)
                return true;

            // 引用的是目标对象上的组件
            if (reference is Component comp && comp.gameObject == target)
                return true;

            // 引用的 Transform
            if (reference is Transform transform && transform.gameObject == target)
                return true;

            // 引用的 RectTransform
            if (reference is RectTransform rectTransform && rectTransform.gameObject == target)
                return true;

            return false;
        }
    }
}
