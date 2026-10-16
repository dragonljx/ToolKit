using System;

namespace ToolKit
{
    /// <summary>
    /// 普通 C# 类的懒加载全局单例基类
    /// <para>用法：继承后把构造函数设为 <c>private</c>，通过 <c>YourClass.Instance</c> 访问</para>
    /// <code>
    /// public sealed class GameConfig : Singleton&lt;GameConfig&gt;
    /// {
    ///     // 构造函数是私有的，外部只能走 Instance
    ///     private GameConfig()
    ///     {
    ///     }
    ///
    ///     public int MaxLevel { get; private set; } = 99;
    ///
    ///     // 需要读取 Unity API 或做耗时初始化时，重写 Init 并由外部显式调用
    ///     public override void Init()
    ///     {
    ///         MaxLevel = LoadFromDisk();
    ///     }
    /// }
    ///
    /// // 使用：首次访问 Instance 时自动构造
    /// int maxLevel = GameConfig.Instance.MaxLevel;
    ///
    /// // Init 不会自动执行，需要显式调用一次
    /// GameConfig.Instance.Init();
    /// </code>
    /// <para>实例在首次访问 <see cref="Instance"/> 时通过反射创建，创建过程加了双检锁，</para>
    /// <para>即使多个线程同时首次访问也只会构造出一个实例</para>
    /// </summary>
    /// <remarks>
    /// <para>关于 <see cref="Init"/>（最容易踩的坑）：</para>
    /// <list type="bullet">
    /// <item><see cref="Init"/> <strong>不会被自动调用</strong>，基类里它是一个空的虚方法，纯粹是给子类留的钩子</item>
    /// <item>需要初始化逻辑时，必须在使用前显式调用一次 <c>YourClass.Instance.Init()</c>，
    /// 且要自己保证不会被重复调用（基类不做幂等保护）</item>
    /// <item>构造函数本身是安全的初始化位置，只有依赖 Unity API 或外部数据时才需要 <see cref="Init"/></item>
    /// </list>
    /// <para>关于构造：使用 <c>Activator.CreateInstance</c> 的非公开重载，因此 <c>private</c> / <c>protected</c>
    /// 构造函数都能正常创建；但<strong>必须存在无参构造函数</strong>，否则首次访问会抛异常。</para>
    /// <para>关于生命周期：实例创建后不会被释放，一直存活到域重载（进入 / 退出播放模式）。</para>
    /// <para>在关闭了 Domain Reload 的项目里，静态字段会跨播放模式保留，</para>
    /// <para>编辑器下反复进出播放模式可能拿到上一次的旧实例，需要注意状态清理。</para>
    /// <para>本类不依赖 Unity，可以在编辑器脚本、纯 C# 环境（含后台线程）中使用；
    /// 需要挂在 GameObject 上的单例请改用 <see cref="SingletonMono{T}"/>。</para>
    /// </remarks>
    public abstract class Singleton<T> where T : class
    {
        private static T instance;

        private static readonly object Lock = new object();


        /// <summary>
        /// 全局唯一实例，首次访问时按需创建
        /// <para>线程安全；构造过程中抛出的异常不会被吞掉，会直接向调用方传播</para>
        /// </summary>
        /// <returns>该类型的单例实例，始终非 null（除非构造函数抛异常）</returns>
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (Lock)
                    {
                        if (instance == null)
                        {
                            instance = Activator.CreateInstance(
                                typeof(T),
                                true
                            ) as T;
                        }
                    }
                }

                return instance;
            }
        }


        /// <summary>
        /// 初始化钩子，供子类重写
        /// <para><strong>基类不会自动调用它</strong>，需要在使用前显式调用一次 <c>Instance.Init()</c></para>
        /// <para>基类实现为空，没有重写需求时可以不写</para>
        /// </summary>
        /// <remarks>
        /// <para>基类不做「只执行一次」的保护，重复调用会重复执行子类的初始化逻辑；</para>
        /// <para>需要在多处安全调用时，请自行在子类里加一个 <c>bool</c> 标记。</para>
        /// </remarks>
        public virtual void Init()
        {
        }
    }
}
