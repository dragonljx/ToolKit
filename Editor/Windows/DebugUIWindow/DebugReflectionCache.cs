using System;
using System.Collections.Generic;
using System.Reflection;
using ToolKit;
using UnityEngine;

namespace ToolKit.Editor
{
    /// <summary>
    /// 按类型缓存调试特性的反射结果，每个类型只反射一次
    /// <para>用法：<c>DebugSceneScanner</c> 对场景中每个组件调用 <see cref="Get"/> 取缓存</para>
    /// <code>
    /// // 扫描时的典型用法：同一个类型无论有多少个实例，都只反射一次
    /// TypeDebugInfo info = DebugReflectionCache.Get(behaviour.GetType());
    ///
    /// if (info == null || !info.HasAnyDebugInfo)
    /// {
    ///     continue; // 与调试无关的类型，直接跳过
    /// }
    /// </code>
    /// <para>缓存的意义在于：场景里可能有成百上千个同类组件实例，
    /// 若不缓存会对同一类型反复做反射，每次刷新窗口都很慢</para>
    /// </summary>
    /// <remarks>
    /// <para>关于刷新策略：缓存<strong>没有失效机制</strong>，一个类型被缓存后其条目不会再重建。</para>
    /// <list type="bullet">
    /// <item>正常重编译会触发域重载，静态字段被重置，缓存自然清空，因此日常开发不需要手动干预</item>
    /// <item>如果项目关闭了 Domain Reload，重编译后缓存里会残留指向旧程序集的 <c>MethodInfo</c>，
    /// 此时需要手动调用 <see cref="Clear"/></item>
    /// </list>
    /// <para>关于扫描范围（决定了哪些成员能被发现）：</para>
    /// <list type="bullet">
    /// <item>绑定标志为 <c>Instance | Public | NonPublic</c>，因此<strong>只扫描实例成员</strong>，
    /// 静态字段与静态方法永远不会被收录</item>
    /// <item>由于没有 <c>FlattenHierarchy</c>，基类的私有成员不会被继承到此类型的扫描结果里，
    /// 但基类的 public / protected 成员会被 <c>GetMethods</c> 等返回</item>
    /// <item>属性必须同时可读可写才会被收录，只读属性会被跳过</item>
    /// <item>方法扫描也会看到属性的 get / set 访问器，只是它们通常不带 <c>[DebugButton]</c>，会被属性检查过滤掉</item>
    /// </list>
    /// <para>本类不清理旧类型的缓存条目：脚本重编译后旧 <c>Type</c> 对应的记录会一直留在字典里，
    /// 属于可控的少量泄漏。</para>
    /// </remarks>
    public static class DebugReflectionCache
    {
        /// <summary>类型 → 扫描结果的缓存，键是组件类型，值是其特征汇总</summary>
        private static readonly Dictionary<Type, TypeDebugInfo> _typeCache =
            new Dictionary<Type, TypeDebugInfo>();

        /// <summary>
        /// 扫描成员时使用的绑定标志：实例成员，且包含非公开成员
        /// <para>不含 <c>Static</c>，因此静态成员一律不参与调试扫描</para>
        /// </summary>
        private static readonly BindingFlags _bindingFlags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        /// <summary>
        /// 获取指定类型的调试特性扫描结果，首次访问时反射并缓存
        /// </summary>
        /// <param name="type">要查询的组件类型</param>
        /// <returns>该类型的扫描结果；<paramref name="type"/> 为 null 时返回 null</returns>
        /// <remarks>
        /// <para>返回值<strong>始终非 null</strong>（只要传入的类型不为 null），
        /// 即使该类型没有任何调试特性，也会返回一个两个列表都为空的对象，
        /// 调用方应通过 <c>HasAnyDebugInfo</c> 判断是否值得继续处理。</para>
        /// </remarks>
        public static TypeDebugInfo Get(Type type)
        {
            if (type == null)
                return null;

            if (_typeCache.TryGetValue(type, out TypeDebugInfo cachedInfo))
                return cachedInfo;

            TypeDebugInfo info = BuildTypeDebugInfo(type);
            _typeCache[type] = info;
            return info;
        }

        /// <summary>
        /// 清空全部缓存，下次访问时重新反射
        /// <code>
        /// // 关闭了 Domain Reload 时，重编译后手动刷新一次
        /// DebugReflectionCache.Clear();
        /// </code>
        /// </summary>
        /// <remarks>
        /// <para>目前<strong>没有任何代码调用它</strong>：正常域重载会重置静态字段，已足够。</para>
        /// <para>仅在关闭 Domain Reload 的项目里需要手动调用，
        /// 否则重编译后会用到指向已卸载程序集的过期反射信息。</para>
        /// <para>清空是同步且即时的，但已经取出的 <c>TypeDebugInfo</c> 引用仍然有效，
        /// 因为 <c>DebugWindow</c> 每次刷新都会重新走一遍扫描。</para>
        /// </remarks>
        public static void Clear()
        {
            _typeCache.Clear();
        }

        /// <summary>
        /// 反射一个类型，收集其中所有带调试特性的字段、属性与方法
        /// </summary>
        /// <param name="type">要扫描的组件类型</param>
        /// <returns>填充完成的扫描结果，两个列表按字段 → 属性 → 方法的顺序构建</returns>
        /// <remarks>
        /// <para>只负责「有哪些成员」，不关心场景中有多少实例，这是缓存能生效的前提。</para>
        /// <para>顺序说明：每个列表内部保持反射返回的顺序，而反射顺序在 .NET 中不保证稳定，
        /// 因此成员较多时界面上的排列顺序可能与声明顺序不一致。</para>
        /// </remarks>
        private static TypeDebugInfo BuildTypeDebugInfo(Type type)
        {
            TypeDebugInfo info = new TypeDebugInfo(type);

            // 扫描字段
            foreach (FieldInfo field in type.GetFields(_bindingFlags))
            {
                DebugDataAttribute attribute = field.GetCustomAttribute<DebugDataAttribute>();
                if (attribute != null)
                {
                    info.Members.Add(new DebugMemberInfo(field, attribute));
                    info.HasDebugData = true;
                }
            }

            // 扫描属性（仅支持可读可写）
            foreach (PropertyInfo property in type.GetProperties(_bindingFlags))
            {
                if (!property.CanRead || !property.CanWrite)
                    continue;

                DebugDataAttribute attribute = property.GetCustomAttribute<DebugDataAttribute>();
                if (attribute != null)
                {
                    info.Members.Add(new DebugMemberInfo(property, attribute));
                    info.HasDebugData = true;
                }
            }

            // 扫描方法（支持参数，参数输入参考 ButtonDrawer 绘制）
            foreach (MethodInfo method in type.GetMethods(_bindingFlags))
            {
                DebugButtonAttribute attribute = method.GetCustomAttribute<DebugButtonAttribute>();
                if (attribute != null)
                {
                    info.Methods.Add(new DebugMethodInfo(method, attribute));
                }
            }

            return info;
        }
    }
}
