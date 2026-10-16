using UnityEngine;

namespace ToolKit
{
    /// <summary>
    /// MonoBehaviour 单例基类，支持场景实例查找、自动创建与重复实例清理
    /// <para>用法：继承后无需写任何样板代码，通过 <c>YourClass.Instance</c> 访问；也可以手动挂到场景物体上</para>
    /// <code>
    /// // 方式一：只写一个类，场景里不用挂东西，首次访问时自动创建
    /// public class AudioManager : SingletonMono&lt;AudioManager&gt;
    /// {
    ///     public void PlaySfx(string name) { /* ... */ }
    /// }
    ///
    /// AudioManager.Instance.PlaySfx("click");
    ///
    /// // 方式二：手动挂到场景物体上，可以在 Inspector 里配置参数
    /// // 此时会自动复用场景里的实例，不会重复创建
    ///
    /// // 需要额外初始化逻辑时，重写 Awake 必须调用 base.Awake()
    /// public class UIManager : SingletonMono&lt;UIManager&gt;
    /// {
    ///     protected override void Awake()
    ///     {
    ///         base.Awake(); // 漏掉这行，单例就不会被注册
    ///         BuildUIRoot();
    ///     }
    /// }
    /// </code>
    /// <para>首次访问 <see cref="Instance"/> 时会先在场景里查找已有实例；</para>
    /// <para>找不到才新建一个名为 <c>(Singleton)类型名</c> 的 GameObject 并挂上组件</para>
    /// </summary>
    /// <remarks>
    /// <para>关于 <c>isDontDestroy</c>：</para>
    /// <list type="bullet">
    /// <item>该字段是 Inspector 上的开关，只有手动挂在场景物体上时才方便勾选</item>
    /// <item><strong>自动创建的实例读不到这个开关</strong>——新建组件的字段取初始值 <c>false</c>，
    /// 因此自动创建的单例默认会随场景切换被销毁</item>
    /// <item>希望自动创建的单例跨场景常驻，需要把字段初始值改成 <c>true</c>，或在首次访问前先手动放置一个实例</item>
    /// </list>
    /// <para>关于 <see cref="Awake"/> 与 <see cref="OnDestroy"/>：</para>
    /// <list type="bullet">
    /// <item>两者都是 <c>protected virtual</c>，子类重写时<strong>必须先调用 <c>base.Awake()</c> / <c>base.OnDestroy()</c></strong>，
    /// 否则单例注册或注销会失效</item>
    /// <item>场景中出现多个实例时，后加载的会被 <c>Destroy</c> 掉并输出警告，只保留第一个</item>
    /// </list>
    /// <para>关于线程：<see cref="Instance"/> 的 getter 虽然加了锁，但创建过程会调用
    /// <c>FindObjectOfType</c> / <c>new GameObject</c> 等 Unity 主线程专有 API，</para>
    /// <para>从后台线程首次访问会直接抛异常，必须保证首次访问发生在主线程。</para>
    /// <para>关于同类型多实例：查找时若发现场景中存在多个实例，会记录错误日志并跳过
    /// <c>DontDestroyOnLoad</c> 的处理，<see cref="Instance"/> 仍会指向查找到的第一个。</para>
    /// </remarks>
    /// <typeparam name="T">单例类型，必须继承自本类</typeparam>
    public abstract class SingletonMono<T> : MonoBehaviour where T : SingletonMono<T>
    {
        internal static T _instance;

        private static readonly object _lock = new object();

        [SerializeField] private bool isDontDestroy = false;          // 是否设为不销毁

        /// <summary>
        /// 单例实例，为 null 时自动查找或创建
        /// <para>首次访问会触发场景查找 + 按需创建，因此不能在 <c>Awake</c> 之前的更早阶段访问</para>
        /// </summary>
        /// <returns>该类型的单例实例；场景中存在多个实例时会返回第一个并记录错误日志</returns>
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            CreateInstance();
                        }
                    }
                }

                return _instance;
            }

            protected set
            {
                _instance = value;
            }
        }

        /// <summary>
        /// 查找场景中的现有实例，找不到则新建一个挂载本组件的 GameObject
        /// <para>由 <see cref="Instance"/> 的 getter 在持有锁的情况下调用，不要手动调用</para>
        /// </summary>
        /// <remarks>
        /// <para>关于多实例处理：查到多个实例时只记录错误日志并提前返回，</para>
        /// <para>此时 <c>_instance</c> 已被赋值为第一个查到的实例，但不会对它执行 <c>DontDestroyOnLoad</c>。</para>
        /// <para>关于自动创建的命名：新建的 GameObject 名为 <c>(Singleton)类型名</c>，便于在 Hierarchy 中辨认。</para>
        /// </remarks>
        private static void CreateInstance()
        {
#if UNITY_2022_2_OR_NEWER

            _instance = FindFirstObjectByType<T>();

            T[] instances = FindObjectsByType<T>(
                FindObjectsSortMode.None
            );

#else

            _instance = FindObjectOfType<T>();

            T[] instances = FindObjectsOfType<T>();

#endif

            // 检查多个实例
            if (instances.Length > 1)
            {
                Debug.LogError(
                    $"[Singleton] 场景存在多个 {typeof(T).Name} 单例实例!"
                );

                return;
            }

            // 场景没有实例，自动创建
            if (_instance == null)
            {
                GameObject obj = new GameObject(
                    "(Singleton)" + typeof(T).Name
                );

                _instance = obj.AddComponent<T>();
                if (_instance.isDontDestroy)
                {
                    DontDestroyOnLoad(obj);
                }
            }
            else
            {
                if (_instance.isDontDestroy)
                {
                    DontDestroyOnLoad(_instance.gameObject);
                }
            }
        }

        /// <summary>
        /// 注册当前实例为单例，按配置决定是否跨场景常驻
        /// <para>子类重写时<strong>必须调用 <c>base.Awake()</c></strong>，否则单例不会注册</para>
        /// </summary>
        /// <remarks>
        /// <para>当场景中已存在实例且不是自己时，会输出警告并销毁自己所在的 GameObject，
        /// 这是多实例保护，属于正常行为而非错误。</para>
        /// </remarks>
        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                if (isDontDestroy)
                {
                    DontDestroyOnLoad(gameObject);

                }
            }
            else if (_instance != this)
            {
                Debug.LogWarning(
                    $"[Singleton] 销毁重复实例 {typeof(T).Name}"
                );

                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 注销单例引用，避免留下已销毁对象的悬空引用
        /// <para>子类重写时<strong>必须调用 <c>base.OnDestroy()</c></strong>，否则下次访问会拿到已销毁的实例</para>
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
