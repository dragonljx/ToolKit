using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using Object = UnityEngine.Object;

namespace ToolKit
{
    /// <summary>
    /// 可序列化的场景引用：编辑器里显示成好用的 <c>SceneAsset</c> 选择框，构建后靠路径字符串定位场景
    /// <para>用法：直接用本类型替代裸 <c>SceneAsset</c> 字段，字段名和用法都保持一致</para>
    /// <code>
    /// public class LevelLoader : MonoBehaviour
    /// {
    ///     // 在 Inspector 里是一个场景选择框，构建后仍能用
    ///     [SerializeField]
    ///     private SceneSerialized targetScene;
    ///
    ///     public void Load()
    ///     {
    ///         SceneManager.LoadScene(targetScene.ScenePath);
    ///
    ///         // 也可以省掉 ScenePath，隐式转换会取路径
    ///         SceneManager.LoadScene(targetScene);
    ///     }
    /// }
    /// </code>
    /// <para>为什么需要它：Unity 的 <c>SceneAsset</c> 是编辑器专用类型，只存在于 Editor 程序集，</para>
    /// <para>把它序列化进组件后，构建出来的包里的引用会失效，加载时拿不到路径。</para>
    /// <para>本类型通过 <c>ISerializationCallbackReceiver</c> 把路径与名称另存为字符串，绕开这个限制</para>
    /// </summary>
    /// <remarks>
    /// <para>关于两套数据的优先级：</para>
    /// <list type="bullet">
    /// <item>编辑器里 <see cref="ScenePath"/> / <see cref="SceneName"/> 都是<strong>实时从 asset 反查</strong>的，
    /// 字符串字段只是备份</item>
    /// <item>只有 asset 失效（比如场景被移动、重命名、删除）时，才会反过来用字符串去恢复 asset</item>
    /// <item>序列化时 asset 优先并覆写字符串；反序列化时若 asset 无效则用字符串尝试恢复，
    /// 恢复失败会把两者都清空，避免留下一个指向不存在场景的脏路径</item>
    /// <item>正是因为会在 <c>OnBeforeSerialize</c> / <c>OnAfterDeserialize</c> 里调用 <c>MarkAllScenesDirty</c>，
    /// 改动这类字段会让场景处于未保存状态</item>
    /// </list>
    /// <para>关于 <c>OnAfterDeserialize</c> 的延迟：反序列化过程中不能访问 <c>AssetDatabase</c>，
    /// 因此恢复 asset 的动作被挂到下一帧的 <c>EditorApplication.update</c> 上执行，</para>
    /// <para>意味着刚加载完的那一刻 <c>sceneAsset</c> 可能还是 null，但 <c>ScenePath</c> 已经可用。</para>
    /// <para>关于运行时：构建后 <c>UNITY_EDITOR</c> 不成立，所有 asset 相关逻辑被整体剔除，
    /// 只剩两个字符串字段，读 <c>ScenePath</c> 就是一次纯字段访问。</para>
    /// </remarks>
    [Serializable]
    public class SceneSerialized : ISerializationCallbackReceiver
    {
#if UNITY_EDITOR
        /// <summary>
        /// 编辑器里用于选择场景的 asset 引用
        /// <para>仅在编辑器存在，构建时会被剔除；运行时场景路径以 <c>scenePath</c> 字符串为准</para>
        /// </summary>
        // What we use in editor to select the scene
        [SerializeField] private Object sceneAsset;

        /// <summary>
        /// 当前的 <c>sceneAsset</c> 是否是一个有效的场景资源
        /// </summary>
        private bool IsValidSceneAsset
        {
            get
            {
                if (!sceneAsset) return false;

                return sceneAsset is SceneAsset;
            }
        }
#endif

        /// <summary>
        /// 场景路径的持久化存储
        /// <para>只在序列化与反序列化过程中写入，业务代码请统一走 <see cref="ScenePath"/> 属性</para>
        /// </summary>
        // This should only ever be set during serialization/deserialization!
        [SerializeField]
        private string scenePath = string.Empty;


        /// <summary>
        /// 目标场景的资源路径，例如 <c>Assets/Scenes/Main.unity</c>
        /// <para>这是运行时加载场景时该用的值；也能通过隐式转换从 <see cref="SceneSerialized"/> 直接取到</para>
        /// <code>
        /// SceneManager.LoadScene(targetScene.ScenePath);
        ///
        /// // 或者给新场景赋值：编辑器里会同步刷新 Inspector 上的场景选择框
        /// targetScene.ScenePath = "Assets/Scenes/Battle.unity";
        /// </code>
        /// </summary>
        /// <remarks>
        /// <para>关于平台差异：</para>
        /// <list type="bullet">
        /// <item>编辑器下 get 走 <c>AssetDatabase.GetAssetPath</c>，每次访问都有一次 AssetDatabase 查询开销</item>
        /// <item>构建后 get 直接返回序列化的字符串，无任何额外开销</item>
        /// <item>编辑器下若 asset 为空（例如引用的场景被删了），get 返回空字符串而不是旧路径</item>
        /// <item>set 会同时写入字符串与 asset；写入的路径不存在时 asset 保持为 null，路径字符串仍会被保留</item>
        /// </list>
        /// </remarks>
        // Use this when you want to actually have the scene path
        public string ScenePath
        {
            get
            {
#if UNITY_EDITOR
                return GetScenePathFromAsset();
#else
            return scenePath;
#endif
            }
            set
            {
                scenePath = value;
#if UNITY_EDITOR
                sceneAsset = GetSceneAssetFromPath();
#endif
            }
        }

        /// <summary>
        /// 目标场景的名称（不含扩展名），例如 <c>Main</c>
        /// </summary>
        /// <remarks>
        /// <para>关于编辑器下的 set（注意这个坑）：</para>
        /// <list type="bullet">
        /// <item>编辑器下 set 会忽略传入的值，转而用<strong>当前 asset</strong> 反查名称写回 <c>sceneName</c></item>
        /// <item>因此 asset 为空时给 <see cref="SceneName"/> 赋值，结果是把它清成空字符串，
        /// 而不是写入你传的值</item>
        /// <item>需要按名称指定场景时，请改用 <see cref="ScenePath"/>；只有构建后的运行时才应该给 <see cref="SceneName"/> 赋值</item>
        /// </list>
        /// <para>get 与 <see cref="ScenePath"/> 同理：编辑器下从 asset 实时取，构建后读字符串。</para>
        /// </remarks>
        [SerializeField]
        private string sceneName = string.Empty;

        /// <summary>
        /// 目标场景的名称（不含扩展名）
        /// <para>需要把场景名显示在 UI 上时用它，比从 <see cref="ScenePath"/> 手工截取更省事</para>
        /// </summary>
        public string SceneName
        {
            get
            {
#if UNITY_EDITOR
                return GetSceneNameFromAsset();
#else
            return sceneName;
#endif
            }
            set
            {
                sceneName = value;
#if UNITY_EDITOR
                sceneName = GetSceneNameFromPath();
#endif
            }
        }

        /// <summary>
        /// 隐式转换到 <c>string</c>，等价于读取 <see cref="ScenePath"/>
        /// <para>让 <c>SceneManager.LoadScene(sceneRef)</c> 这类直接传字符串的接口能省掉一次属性访问</para>
        /// </summary>
        /// <param name="sceneReference">要取路径的场景引用</param>
        /// <returns>该引用指向的场景资源路径</returns>
        public static implicit operator string(SceneSerialized sceneReference)
        {
            return sceneReference.ScenePath;
        }

        /// <summary>
        /// 序列化前回调，把 asset 与字符串两套数据对齐
        /// <para>由 Unity 的序列化流程调用，业务代码不要手动调用</para>
        /// </summary>
        /// <remarks>
        /// <para>编辑器中会执行 <c>HandleBeforeSerialize</c>：asset 有效则以 asset 为准覆写路径与名称；</para>
        /// <para>asset 失效但字符串还在时，用字符串尝试恢复 asset，恢复不到就把两者都清空。</para>
        /// <para>非编辑器环境本方法是空实现。</para>
        /// </remarks>
        // Called to prepare this data for serialization. Stubbed out when not in editor.
        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            HandleBeforeSerialize();
#endif
        }

        /// <summary>
        /// 反序列化后回调，延迟到下一帧用字符串恢复失效的 asset 引用
        /// <para>由 Unity 的序列化流程调用，业务代码不要手动调用</para>
        /// </summary>
        /// <remarks>
        /// <para>反序列化过程中不允许访问 <c>AssetDatabase</c>，所以实际恢复动作被挂到
        /// <c>EditorApplication.update</c> 上，只执行一次便自行注销。</para>
        /// <para>非编辑器环境本方法是空实现。</para>
        /// </remarks>
        // Called to set up data for deserialization. Stubbed out when not in editor.
        public void OnAfterDeserialize()
        {
#if UNITY_EDITOR
            // We sadly cannot touch assetdatabase during serialization, so defer by a bit.
            EditorApplication.update += HandleAfterDeserialize;
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// 从 <c>scenePath</c> 字符串加载对应的 <c>SceneAsset</c>
        /// </summary>
        /// <returns>加载到的场景资源；路径为空或资源不存在时返回 null</returns>
        private SceneAsset GetSceneAssetFromPath()
        {
            return string.IsNullOrEmpty(scenePath) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        }

        /// <summary>
        /// 从当前的 <c>sceneAsset</c> 反查资源路径
        /// </summary>
        /// <returns>场景资源路径；asset 为空时返回空字符串</returns>
        private string GetScenePathFromAsset()
        {
            return sceneAsset == null ? string.Empty : AssetDatabase.GetAssetPath(sceneAsset);
        }

        /// <summary>
        /// 从当前的 <c>sceneAsset</c> 取场景名称（不含扩展名）
        /// </summary>
        /// <returns>场景名称；asset 为空时返回空字符串</returns>
        private string GetSceneNameFromAsset()
        {
            return sceneAsset == null ? string.Empty : (sceneAsset as SceneAsset)?.name;
        }

        /// <summary>
        /// 用 <c>sceneAsset</c> 做空判断，再从 <c>scenePath</c> 加载资源并取名称
        /// </summary>
        /// <returns>场景名称；asset 为空时返回空字符串</returns>
        /// <remarks>
        /// <para>注意判空与加载用的不是同一个字段：空判断基于 <c>sceneAsset</c>，加载却基于 <c>scenePath</c>。</para>
        /// <para>这正是 <see cref="SceneName"/> 的 set 在 asset 为空时会清空名称的原因。</para>
        /// </remarks>
        private string GetSceneNameFromPath()
        {
            return sceneAsset == null ? string.Empty : AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath).name;
        }

        /// <summary>
        /// 序列化前对齐 asset 与字符串两套数据，并在必要时把场景标记为已修改
        /// </summary>
        private void HandleBeforeSerialize()
        {
            // Asset is invalid but have Path to try and recover from
            if (IsValidSceneAsset == false && string.IsNullOrEmpty(scenePath) == false && string.IsNullOrEmpty(sceneName) == false)
            {
                sceneAsset = GetSceneAssetFromPath();
                if (sceneAsset == null)
                {
                    scenePath = string.Empty;
                    sceneName = string.Empty;
                }

                EditorSceneManager.MarkAllScenesDirty();
            }
            // Asset takes precendence and overwrites Path
            else
            {
                scenePath = GetScenePathFromAsset();
                sceneName = GetSceneNameFromAsset();
            }
        }

        /// <summary>
        /// 延迟执行的 asset 恢复逻辑，用字符串路径尝试找回失效的场景引用
        /// </summary>
        /// <remarks>
        /// <para>进入时会先注销 <c>EditorApplication.update</c> 订阅，因此只会执行一次，不会每帧重复跑。</para>
        /// <para>恢复失败时把路径与名称一并清空，避免留下指向不存在场景的脏数据。</para>
        /// </remarks>
        private void HandleAfterDeserialize()
        {
            EditorApplication.update -= HandleAfterDeserialize;
            // Asset is valid, don't do anything - Path will always be set based on it when it matters
            if (IsValidSceneAsset) return;

            // Asset is invalid but have path to try and recover from
            if (string.IsNullOrEmpty(scenePath) || string.IsNullOrEmpty(sceneName)) return;

            sceneAsset = GetSceneAssetFromPath();
            // No asset found, path was invalid. Make sure we don't carry over the old invalid path
            if (!sceneAsset)
            {
                scenePath = string.Empty;
                sceneName = string.Empty;
            }

            if (!Application.isPlaying) EditorSceneManager.MarkAllScenesDirty();
        }
#endif
    }

}
