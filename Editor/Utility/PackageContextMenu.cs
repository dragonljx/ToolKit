using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace ToolKit.Editor
{
    /// <summary>
    /// 在 Assets 右键菜单里提供「卸载插件包」入口，直接把选中的 Package 从项目中移除
    /// <para>用法：在 Project 窗口里选中 <c>Packages</c> 下的某个包文件夹，右键选择卸载</para>
    /// <code>
    /// // 操作步骤：
    /// // 1. Project 窗口切到 Packages 视图（或展开 Packages 节点）
    /// // 2. 选中要卸载的包文件夹，例如 Packages/com.dragon.toolkit
    /// // 3. 右键 → 卸载插件包
    /// //
    /// // 菜单项只会在「恰好选中一个 Packages 下的资源」时出现，
    /// // 选中多个或选中的是 Assets 下的资源时菜单会置灰
    /// </code>
    /// <para>卸载通过 <c>PackageManager.Client.Remove</c> 完成，等价于直接编辑
    /// <c>manifest.json</c> 后等待包管理器刷新</para>
    /// </summary>
    /// <remarks>
    /// <para>关于可用范围：内嵌包与注册表包、Git 包都可以卸载，判断依据是
    /// <c>Packages/包名</c> 这一虚拟路径，而不是它在磁盘上是否真实存在。</para>
    /// <list type="bullet">
    /// <item>内嵌包直接位于项目的 <c>Packages/</c> 目录下，路径与磁盘一致</item>
    /// <item>来自注册表或 Git 的包，其内容实际在 <c>Library/PackageCache</c> 下，
    /// <c>Packages/xxx</c> 只是 Unity 的虚拟路径</item>
    /// <item>两类包都能通过 <c>AssetDatabase.GetAssetPath</c> 拿到统一形式的路径，
    /// 因此不需要（也不能）用文件系统 API 判断有效性</item>
    /// </list>
    /// <para>关于包名解析：取路径按 <c>/</c> 切分后的第二段，
    /// 因此 <c>Packages/com.xxx/Something</c> 这类深层选中也能正确取出包名。</para>
    /// <para>关于阻塞：执行时会空转等待 <c>Client.Remove</c> 请求完成，<strong>期间整个编辑器界面会卡住</strong>，
    /// 包越大卡得越久。按 Unity 的惯例应该用 <c>EditorApplication.update</c> 轮询异步处理，
    /// 本实现为了代码简短选择了同步等待。</para>
    /// <para>卸载是<strong>不可撤销</strong>的操作，会直接改动 <c>manifest.json</c>，
    /// 若项目未纳入版本控制需要谨慎使用。</para>
    /// </remarks>
    public class PackageContextMenu
    {
        /// <summary>
        /// 校验函数，控制菜单项是否可用
        /// </summary>
        /// <returns>恰好选中一个位于 <c>Packages/</c> 下的资源时返回 true，菜单项才可点击</returns>
        /// <remarks>
        /// <para>Unity 识别的是 <c>[MenuItem]</c> 的第二个参数 <c>validate</c> 为 true，方法名本身不重要。</para>
        /// <para>校验函数会被频繁调用（选中变化、菜单展开时），因此实现里只做了轻量的路径前缀判断。</para>
        /// </remarks>
        // Validate：控制菜单显示
        [MenuItem("Assets/卸载插件包", true)]
        private static bool ValidatePackage()
        {
            Object[] selectedObjects = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
            if (selectedObjects.Length != 1) return false;
            string path = AssetDatabase.GetAssetPath(selectedObjects[0]);
            return path.StartsWith("Packages/");
        }

        /// <summary>
        /// 执行卸载：解析出包名后调用 PackageManager 移除该包
        /// </summary>
        /// <remarks>
        /// <para>不使用返回值，成功与失败都只体现在日志里。</para>
        /// <para>关于有效性判断：这里<strong>不能</strong>用 <c>Directory.Exists</c> 校验路径。
        /// 注册表包与 Git 包的 <c>Packages/包名</c> 是虚拟路径，磁盘上并不存在，
        /// 用文件系统检查会把它们误判为无效而拒绝卸载。因此改为校验路径形态：
        /// 必须以 <c>Packages/</c> 开头且能解析出非空的包名。</para>
        /// </remarks>
        // 点击菜单执行
        [MenuItem("Assets/卸载插件包", false)]
        private static void DoSomethingWithPackage()
        {
            Object[] selectedObjects = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
            if (selectedObjects.Length != 1) return;
            string path = AssetDatabase.GetAssetPath(selectedObjects[0]);

            string[] parts = path.Split('/');
            if (parts.Length < 2 || parts[0] != "Packages" || string.IsNullOrEmpty(parts[1]))
            {
                Debug.LogError("请选择一个 Packages 下的插件包文件夹");
                return;
            }

            string packageName = parts[1];

            RemoveRequest removeRequest = Client.Remove(packageName);
            // 等待卸载请求完成
            while (!removeRequest.IsCompleted)
            {
            }

            // 检查卸载请求的状态
            if (removeRequest.Status == StatusCode.Success)
            {
                Debug.Log("包已成功卸载: " + packageName);
            }
            else
            {
                Debug.LogError("卸载包失败: " + removeRequest.Error.message);
            }
        }
    }
}
