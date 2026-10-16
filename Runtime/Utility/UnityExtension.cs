using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace ToolKit
{
    /// <summary>
    /// 通用扩展方法集合：角度与向量互转、集合随机与洗牌、Transform 批量操作
    /// <para>用法：在 <c>using ToolKit;</c> 之后直接以实例方法的形式调用</para>
    /// <code>
    /// // 角度与向量互转
    /// Vector2 dir = 45f.DegToVector();        // (0.707, 0.707)
    /// float deg = dir.VectorToDeg();          // 45
    ///
    /// // 集合随机
    /// Enemy enemy = enemies.GetRandomOne();               // 随机取一个，不删除
    /// Enemy picked = enemies.GetRandomOne(isRemove: true); // 随机取一个并移除（抽奖池）
    /// enemies.Shuffle();                                   // 原地洗牌
    ///
    /// // Transform
    /// transform.ResetTransform();                          // 归零本地位移/旋转/缩放
    /// List&lt;Transform&gt; bones = transform.GetAllChildren("Bone"); // 递归找出所有名字含 Bone 的后代
    /// </code>
    /// <para>大部分方法都是 O(n) 或更差，且不返回新集合（洗牌是原地修改），</para>
    /// <para>在每帧调用的逻辑里使用前请先确认数据规模</para>
    /// </summary>
    /// <remarks>
    /// <para>关于随机方法的空集合行为（容易踩坑，且三个重载不一致）：</para>
    /// <list type="bullet">
    /// <item><see cref="GetRandomOne{T}(List{T}, bool)"/> 与 <see cref="GetRandomOne{T}(T[])"/> 在集合为空时会抛
    /// <c>ArgumentOutOfRangeException</c> / <c>IndexOutOfRangeException</c>，<strong>必须自行判空</strong></item>
    /// <item><see cref="GetRandomOneIndex{T}"/> 在数组为空时<strong>不会抛异常</strong>，而是返回无效索引 0，
    /// 静默错误更难排查</item>
    /// </list>
    /// <para>关于 <see cref="Shuffle{T}(List{T})"/> 的分布：实现是标准 Fisher-Yates 变体，</para>
    /// <para>能保证每种排列等概率出现，可以放心用于抽奖、发牌等场景。</para>
    /// <para>所有随机都基于 <c>UnityEngine.Random</c>，受 <c>Random.InitState</c> 影响，可复现。</para>
    /// </remarks>
    public static class UnityExtension
    {
        /// <summary>
        /// 把二维向量转成弧度角
        /// <para>向量被视为从原点出发的方向，返回该方向与 X 轴正方向的夹角</para>
        /// <code>
        /// float rad = new Vector2(0, 1).VectorToRad(); // 约 1.5708（即 90°）
        /// </code>
        /// </summary>
        /// <param name="thisVec">方向向量；零向量会返回 0</param>
        /// <returns>弧度值，范围为 <c>[-π, π]</c>，逆时针为正</returns>
        public static float VectorToRad(this Vector2 thisVec)
        {
            return Mathf.Atan2(thisVec.y, thisVec.x);
        }

        /// <summary>
        /// 把弧度角转成单位方向向量
        /// <code>
        /// Vector2 dir = (Mathf.PI / 2f).RadToVector(); // 约 (0, 1)
        /// </code>
        /// </summary>
        /// <param name="thisFloat">弧度值，超出 <c>[-π, π]</c> 也能正确处理（三角函数本身是周期函数）</param>
        /// <returns>模长为 1 的方向向量</returns>
        public static Vector2 RadToVector(this float thisFloat)
        {
            return new Vector2(Mathf.Cos(thisFloat), Mathf.Sin(thisFloat));
        }

        /// <summary>
        /// 把二维向量转成角度
        /// <para>与 <see cref="VectorToRad"/> 相同，只是结果换算成度</para>
        /// <code>
        /// float deg = new Vector2(1, 1).VectorToDeg(); // 45
        /// </code>
        /// </summary>
        /// <param name="thisVec">方向向量；零向量会返回 0</param>
        /// <returns>角度值，范围为 <c>[-180, 180]</c>，逆时针为正</returns>
        public static float VectorToDeg(this Vector2 thisVec)
        {
            return Mathf.Atan2(thisVec.y, thisVec.x) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// 把角度转成单位方向向量
        /// <code>
        /// Vector2 dir = 90f.DegToVector(); // 约 (0, 1)
        /// </code>
        /// </summary>
        /// <param name="thisFloat">角度值，超出 <c>[-180, 180]</c> 也能正确处理</param>
        /// <returns>模长为 1 的方向向量</returns>
        public static Vector2 DegToVector(this float thisFloat)
        {
            return new Vector2(Mathf.Cos(thisFloat * Mathf.Deg2Rad), Mathf.Sin(thisFloat * Mathf.Deg2Rad));
        }

        /// <summary>
        /// 添加元素，已存在时跳过
        /// <para>用于维护「不重复列表」这类集合，避免调用方每次都要写一遍 Contains 判断</para>
        /// <code>
        /// List&lt;int&gt; selected = new List&lt;int&gt;();
        /// // Collection&lt;T&gt; 才支持本扩展，List 需要包一层或改用 HashSet
        /// </code>
        /// </summary>
        /// <typeparam name="T">元素类型，按默认相等比较器判断重复</typeparam>
        /// <param name="dic">目标集合，原地修改</param>
        /// <param name="obj">要添加的元素</param>
        /// <returns>本次确实添加了返回 true；元素已存在返回 false</returns>
        /// <remarks>
        /// <para>参数名是 <c>dic</c>，但类型是 <c>Collection&lt;T&gt;</c>，并非字典，命名属于历史遗留。</para>
        /// <para>内部走 <c>Contains</c> 线性查找，每次添加都是 O(n)，大批量去重请改用 <c>HashSet</c>。</para>
        /// </remarks>
        public static bool AddWithout<T>(this Collection<T> dic, T obj)
        {
            if (!dic.Contains(obj))
            {
                dic.Add(obj);
                return true;
            }
            return false;
        }

        #region 数组随机

        /// <summary>
        /// 从列表中随机取一个元素
        /// <code>
        /// // 抽奖池：抽一个就少一个
        /// string reward = rewardPool.GetRandomOne(isRemove: true);
        ///
        /// // 每次都要有结果：不移除
        /// string hint = hintList.GetRandomOne();
        /// </code>
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="list">源列表</param>
        /// <param name="isRemove">是否在取出后从列表中移除该元素，默认 false</param>
        /// <returns>随机选中的元素</returns>
        /// <exception cref="ArgumentOutOfRangeException">列表为空时抛出</exception>
        public static T GetRandomOne<T>(this List<T> list, bool isRemove = false)
        {
            int index = UnityEngine.Random.Range(0, list.Count);
            T obj = list[index];

            if (isRemove)
            {
                list.RemoveAt(index);
            }

            return obj;
        }

        /// <summary>
        /// 从数组中随机取一个元素
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="array">源数组</param>
        /// <returns>随机选中的元素</returns>
        /// <exception cref="IndexOutOfRangeException">数组为空时抛出</exception>
        public static T GetRandomOne<T>(this T[] array)
        {
            return array[UnityEngine.Random.Range(0, array.Length)];
        }

        /// <summary>
        /// 生成一个数组范围内的随机索引
        /// <para>只想拿到下标、自己再决定取什么时用它</para>
        /// </summary>
        /// <typeparam name="T">数组元素类型（仅用于类型推断，方法本身不读数组内容）</typeparam>
        /// <param name="array">用于确定取值范围 <c>[0, array.Length)</c> 的数组</param>
        /// <returns>随机索引；<strong>数组为空时返回 0 这一无效索引，不抛异常</strong></returns>
        public static int GetRandomOneIndex<T>(this T[] array)
        {
            return UnityEngine.Random.Range(0, array.Length);
        }

        /// <summary>
        /// 原地洗牌，打乱列表顺序
        /// <para>采用标准 Fisher-Yates 变体，每种排列出现概率均等，无偏</para>
        /// <code>
        /// deck.Shuffle();
        /// </code>
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="list">要打乱的列表，原地修改，不返回新集合</param>
        public static void Shuffle<T>(this List<T> list)
        {
            //随机交换
            int currentIndex;
            T tempValue;
            for (int i = 0; i < list.Count; i++)
            {
                currentIndex = UnityEngine.Random.Range(0, list.Count - i);
                tempValue = list[currentIndex];
                list[currentIndex] = list[list.Count - 1 - i];
                list[list.Count - 1 - i] = tempValue;
            }
        }

        /// <summary>
        /// 原地洗牌，打乱数组顺序
        /// <para>与列表版本算法相同，都是无偏的 Fisher-Yates 变体</para>
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="array">要打乱的数组，原地修改，不返回新集合</param>
        public static void Shuffle<T>(this T[] array)
        {
            //随机交换
            int currentIndex;
            T tempValue;
            for (int i = 0; i < array.Length; i++)
            {
                currentIndex = UnityEngine.Random.Range(0, array.Length - i);
                tempValue = array[currentIndex];
                array[currentIndex] = array[array.Length - 1 - i];
                array[array.Length - 1 - i] = tempValue;
            }
        }

        #endregion 数组随机

        #region Transform 扩展

        /// <summary>
        /// 复制另一个 Transform 的位置、旋转与缩放
        /// <code>
        /// child.CopyTransform(parent);   // 让子节点对齐到父节点的变换
        /// follower.CopyTransform(target); // 跟随目标
        /// </code>
        /// </summary>
        /// <param name="tran">要被修改的 Transform</param>
        /// <param name="target">数据来源 Transform</param>
        /// <remarks>
        /// <para>注意三种属性的坐标系并不一致：</para>
        /// <list type="bullet">
        /// <item><c>position</c> 与 <c>rotation</c> 是<strong>世界坐标</strong></item>
        /// <item><c>localScale</c> 是<strong>本地缩放</strong></item>
        /// <item>因此父子层级不同时，复制过去的是「世界位置对齐、本地缩放一致」，
        /// 需要完全本地对齐时应改用 <c>ResetTransform</c> 或手动赋值 <c>localPosition</c></item>
        /// </list>
        /// </remarks>
        public static void CopyTransform(this Transform tran, Transform target)
        {
            tran.position = target.position;
            tran.rotation = target.rotation;
            tran.localScale = target.localScale;
        }

        /// <summary>
        /// 一次性设置 Transform 的位置、缩放与旋转
        /// <para>比连写三行赋值更紧凑，适合对象池复用初始化</para>
        /// <code>
        /// pooled.transform.SetTransformAll(Vector3.zero, Vector3.one, Quaternion.identity);
        /// </code>
        /// </summary>
        /// <param name="tran">要被修改的 Transform</param>
        /// <param name="pos">世界坐标位置</param>
        /// <param name="size">本地缩放</param>
        /// <param name="rotate">世界旋转</param>
        /// <remarks>
        /// <para>与 <see cref="CopyTransform"/> 一样，位置和旋转按世界坐标、缩放按本地坐标处理。</para>
        /// </remarks>
        public static void SetTransformAll(this Transform tran, Vector3 pos, Vector3 size, Quaternion rotate)
        {
            tran.position = pos;
            tran.rotation = rotate;
            tran.localScale = size;
        }

        /// <summary>
        /// 把 Transform 归零：位置与旋转重置为原点、缩放重置为 1
        /// <code>
        /// pooled.transform.ResetTransform();
        /// </code>
        /// </summary>
        /// <param name="tran">要被重置的 Transform</param>
        /// <remarks>
        /// <para>与前两个方法不同，本方法操作的是 <strong>本地</strong>坐标（<c>localPosition</c> / <c>localRotation</c>），</para>
        /// <para>因此父节点带位移时，重置后物体仍会随父节点偏移，这正是对象池复用想要的行为。</para>
        /// </remarks>
        public static void ResetTransform(this Transform tran)
        {
            tran.localPosition = Vector3.zero;
            tran.localRotation = Quaternion.identity;
            tran.localScale = Vector3.one;
        }

        /// <summary>
        /// 递归获取所有后代 Transform，可按名称关键字过滤
        /// <para>注意是<strong>所有层级</strong>的后代，不只是直接子节点</para>
        /// <code>
        /// // 找出所有名字里含 "Bone" 的后代
        /// List&lt;Transform&gt; bones = transform.GetAllChildren("Bone");
        ///
        /// // 不带过滤时返回全部后代，数量可能很多
        /// List&lt;Transform&gt; all = transform.GetAllChildren();
        /// </code>
        /// </summary>
        /// <param name="parent">起始父节点，本身不会出现在结果中</param>
        /// <param name="filterName">可选的名称包含关键字（区分大小写）；为空或 null 时返回全部后代</param>
        /// <returns>符合条件的所有后代 Transform，按深度优先顺序排列</returns>
        /// <remarks>
        /// <para>关于遍历策略：即使某个节点自身不匹配，仍会继续递归它的子节点，</para>
        /// <para>因此「中间层名字对不上、叶子节点能对上」的情况也能找到。</para>
        /// <para>每次调用都会新建 <c>List</c> 并完整遍历子树，不要在每帧对同一棵大树反复调用；</para>
        /// <para>层级很深或节点极多时，递归实现有栈溢出风险。</para>
        /// </remarks>
        public static List<Transform> GetAllChildren(this Transform parent, string filterName = null)
        {
            List<Transform> result = new List<Transform>();
            CollectChildren(parent, result, filterName);
            return result;
        }

        /// <summary>
        /// 深度优先递归收集符合条件的后代节点
        /// </summary>
        /// <param name="current">当前遍历到的节点</param>
        /// <param name="list">收集结果的目标列表</param>
        /// <param name="filterName">名称包含关键字，为空时不做过滤</param>
        private static void CollectChildren(Transform current, List<Transform> list, string filterName)
        {
            foreach (Transform child in current)
            {
                if (string.IsNullOrEmpty(filterName) || child.name.Contains(filterName))
                {
                    list.Add(child);
                }

                // 递归遍历子节点
                CollectChildren(child, list, filterName);
            }
        }

        #endregion Transform 扩展
    }
}
