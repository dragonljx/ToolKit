using System.Collections.Generic;
using UnityEngine;

namespace ToolKit
{
    /// <summary>
    /// 与 Unity 的 <c>Mathf</c> 互补的几何与概率计算
    /// <para>用法：全部是静态方法；几何部分处理尺寸适配，概率部分处理带权抽取</para>
    /// <code>
    /// // 把原图尺寸等比缩放到能装进目标框内（不裁切、不变形）
    /// Vector2 fitted = ToolMath.CalculateAspectRatioScale(new Vector2(1920, 1080), new Vector2(512, 512));
    /// // 结果约为 (512, 288)
    ///
    /// // 按权重抽取掉落物，权重之和按 100 计
    /// var dropTable = new Dictionary&lt;uint, float&gt;
    /// {
    ///     { 1001, 70f }, // 普通装备
    ///     { 1002, 25f }, // 稀有装备
    ///     { 1003,  5f }, // 传说装备
    /// };
    ///
    /// int itemId = ToolMath.CustomRandom(dropTable);
    ///
    /// // 为一组候选生成总和为 1 的权重
    /// float[] weights = ToolMath.CreateRandomWeights(3);
    /// </code>
    /// <para>所有随机都基于 <c>UnityEngine.Random</c>，因此受 <c>Random.InitState</c> 影响，可复现</para>
    /// </summary>
    /// <remarks>
    /// <para>关于 <see cref="CustomRandom"/>：</para>
    /// <list type="bullet">
    /// <item>内部固定取 <c>Random.Range(0, 100f)</c>，所以权重是「百分比」语义，总和应正好是 100</item>
    /// <item>总和小于 100 时，落在尾部区间的抽取会返回 -1，请务必判空处理</item>
    /// <item><c>Random.Range</c> 的浮点重载包含上界，恰好抽到 100 时即使权重总和正好是 100 也会返回 -1，
    /// 属于极小概率但确实存在，兜底逻辑不能省</item>
    /// <item>总和大于 100 时不会报错，但累计和超过 100 之后的那部分条目永远抽不到</item>
    /// <item>依赖字典的枚举顺序累加权重，顺序变化会导致相同权重下抽中的分布不同</item>
    /// </list>
    /// <para>关于 <see cref="CreateRandomWeights"/>：只保证总和为 1 与下界约束，</para>
    /// <para>不保证权重排序，也不保证分布均匀——它只是「随机生成一组合法权重」。</para>
    /// </remarks>
    public static class ToolMath
    {
        #region 几何计算

        /// <summary>
        /// 计算等比缩放到目标框内的尺寸（contain 适配，不裁切也不变形）
        /// <para>结果会贴合目标框的至少一条边，且完整落在目标框内</para>
        /// <code>
        /// // 横图放进方框：以宽度为准
        /// Vector2 a = ToolMath.CalculateAspectRatioScale(new Vector2(1920, 1080), new Vector2(512, 512));
        /// // (512, 288)
        ///
        /// // 原图比目标框小：会等比放大填满
        /// Vector2 b = ToolMath.CalculateAspectRatioScale(new Vector2(64, 64), new Vector2(512, 512));
        /// // (512, 512)
        /// </code>
        /// </summary>
        /// <param name="originalSize">原始尺寸（宽, 高）</param>
        /// <param name="targetSize">目标框尺寸（宽, 高）</param>
        /// <returns>缩放后的尺寸，与 <paramref name="originalSize"/> 保持同一宽高比</returns>
        /// <remarks>
        /// <para>注意本方法不知道「运行时长什么样」，只看尺寸比例：</para>
        /// <para>原图比目标框小时同样会放大，需要禁止放大时请在调用方自行比较后取原始尺寸。</para>
        /// <para><paramref name="targetSize"/> 中任一轴为 0 会导致除以 0 得到 <c>Infinity</c> 或 <c>NaN</c>，</para>
        /// <para>本方法不做参数校验。</para>
        /// </remarks>
        public static Vector2 CalculateAspectRatioScale(Vector2 originalSize, Vector2 targetSize)
        {
            float widthScale = originalSize.x / targetSize.x;
            float heightScale = originalSize.y / targetSize.y;

            float scale = Mathf.Max(widthScale, heightScale);
            return originalSize / scale;
        }

        #endregion

        #region 概率与随机

        /// <summary>
        /// 按权重抽取一个结果 ID（权重以 100 为总和的百分比）
        /// <para>用于掉落表、刷怪权重、随机事件等需要控制概率分布的场景</para>
        /// <code>
        /// var rarityTable = new Dictionary&lt;uint, float&gt;
        /// {
        ///     { 1, 70f }, // 70% 普通
        ///     { 2, 25f }, // 25% 稀有
        ///     { 3,  5f }, // 5%  传说
        /// };
        ///
        /// int rarity = ToolMath.CustomRandom(rarityTable);
        /// if (rarity &lt; 0)
        /// {
        ///     // 字典为空或权重总和不足 100，需要兜底
        ///     rarity = 1;
        /// }
        /// </code>
        /// </summary>
        /// <param name="probabilityDic">权重字典（key 结果 ID，value 权重）；权重之和应为 100，允许为 null</param>
        /// <returns>抽中的结果 ID（由 key 转 <c>int</c>）；字典为空或未抽中任何区间时返回 -1</returns>
        public static int CustomRandom(Dictionary<uint, float> probabilityDic)
        {
            if (probabilityDic == null || probabilityDic.Count == 0)
                return -1;

            float randomValue = Random.Range(0, 100.0f);
            float currentValue = 0;

            foreach (var item in probabilityDic)
            {
                currentValue += item.Value;
                if (randomValue < currentValue)
                {
                    return (int)item.Key;
                }
            }

            return -1;
        }

        /// <summary>
        /// 生成一组总和为 1 的随机权重，且每个权重不低于给定下界
        /// <para>适用于需要「随机但可控」的权重分配，例如随机生成一组技能权重</para>
        /// <code>
        /// // 3 个候选，每个至少占 0.1，总和为 1
        /// float[] weights = ToolMath.CreateRandomWeights(3, 0.1f);
        /// // 例如 { 0.42f, 0.31f, 0.27f }
        /// </code>
        /// </summary>
        /// <param name="number">要生成的权重个数</param>
        /// <param name="minValue">每个权重的最小值（默认 0）；必须满足 <c>0 &lt;= minValue &lt; 1/number</c></param>
        /// <returns>长度为 <paramref name="number"/> 的权重数组，总和恒为 1</returns>
        /// <exception cref="System.ArgumentException">
        /// <paramref name="number"/> 不大于 0，或 <paramref name="minValue"/> 落在 <c>[0, 1/number)</c> 之外时抛出
        /// </exception>
        /// <remarks>
        /// <para>生成过程分两步：先在 <c>[0.2, 1)</c> 上取随机数并归一化，再把</para>
        /// <para><c>minValue * number</c> 的份额平均摊回每一项，因此总和的数学保证是精确的，</para>
        /// <para>但浮点误差可能使总和与 1 有极小偏差，需要严格等于 1 时请自行归一化。</para>
        /// </remarks>
        public static float[] CreateRandomWeights(int number, float minValue = 0f)
        {
            if (number <= 0)
                throw new System.ArgumentException("数量必须大于 0");

            if (minValue < 0f || minValue >= 1f / number)
                throw new System.ArgumentException($"minValue 必须在 [0, {1f / number}) 范围内");

            const float RandomMin = 0.2f;
            const float RandomMax = 1f;

            float retainValue = minValue * number;

            float[] randoms = new float[number];
            float sum = 0;

            for (int i = 0; i < number; i++)
            {
                randoms[i] = Random.Range(RandomMin, RandomMax);
                sum += randoms[i];
            }

            // 归一化
            float scale = (1f - retainValue) / sum;

            for (int i = 0; i < number; i++)
            {
                randoms[i] = randoms[i] * scale + minValue;
            }

            return randoms;
        }

        #endregion
    }
}
