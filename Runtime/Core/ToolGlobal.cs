namespace ToolKit
{
    /// <summary>
    /// 包信息与编辑器菜单路径的全局常量
    /// <para>用法：注册编辑器菜单时拼接菜单路径常量，不要硬编码字符串，避免后续改名漏改</para>
    /// <code>
    /// // 顶层菜单：Tool Kit/我的窗口
    /// [MenuItem(ToolGlobal.ToolKitMenuName + "我的窗口", priority = -1000)]
    /// private static void OpenMyWindow() { }
    ///
    /// // 右键菜单：Assets/Tool Kit/我的命令
    /// [MenuItem(ToolGlobal.AssetMenuName + "我的命令")]
    /// private static void RunMyCommand() { }
    ///
    /// // 日志里带上包版本，便于定位线上问题对应的版本
    /// DLogger.Log("ToolKit", ToolGlobal.Version);
    /// </code>
    /// <para>菜单路径常量都以 <c>/</c> 结尾，拼接时直接接菜单项名称即可</para>
    /// </summary>
    /// <remarks>
    /// <para>关于菜单优先级：</para>
    /// <list type="bullet">
    /// <item>传 <c>priority</c> 为负数可以把菜单项排到该菜单的前面，本包统一使用 -1000 以下</item>
    /// <item>同一菜单下的多个项建议保持 1 以上的间隔，方便以后插入新项而不用改动已有数值</item>
    /// <item>不传 <c>priority</c> 时菜单项会排在默认分组，位于所有自定义优先级项之后</item>
    /// </list>
    /// <para><see cref="Version"/> 是手写常量，不会自动跟随 <c>package.json</c> 的版本号，发版时需要同步修改。</para>
    /// </remarks>
    public static class ToolGlobal
    {
        #region 包信息

        /// <summary>
        /// 包名称（与 package.json 中的 name 保持一致）
        /// </summary>
        public const string PackageName = "com.dragon.toolkit";

        /// <summary>
        /// 包版本
        /// <para>手写常量，不会自动同步 package.json，发版时需要一并修改</para>
        /// </summary>
        public const string Version = "1.0.0";

        #endregion

        #region 菜单路径

        /// <summary>
        /// Assets 菜单路径前缀
        /// <para>用于 Assets 窗口右键菜单，完整形式为 <c>Assets/Tool Kit/菜单项名</c></para>
        /// </summary>
        public const string AssetMenuName = "Assets/Tool Kit/";

        /// <summary>
        /// GameObject 菜单路径前缀
        /// <para>用于 Hierarchy 右键菜单与顶部 GameObject 菜单，完整形式为 <c>GameObject/Tool Kit/菜单项名</c></para>
        /// </summary>
        public const string GameObjectMenuName = "GameObject/Tool Kit/";

        /// <summary>
        /// 顶层菜单路径前缀
        /// <para>用于 Unity 顶部菜单栏，完整形式为 <c>Tool Kit/菜单项名</c></para>
        /// </summary>
        public const string ToolKitMenuName = "Tool Kit/";

        #endregion
    }
}
