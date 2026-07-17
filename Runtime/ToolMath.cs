
using System.Collections.Generic;
using UnityEngine;

namespace ToolKit
{

    public static class ToolMath
    {

        /// <summary>
        /// 计算宽高比缩放
        /// </summary>
        /// <param name="originalSize"></param>
        /// <param name="targetSize"></param>
        /// <returns></returns>
        public static Vector2 CalculateAspectRatioScale(Vector2 originalSize, Vector2 targetSize)
        {
            float widthScale = originalSize.x / targetSize.x;
            float heightScale = originalSize.y / targetSize.y;

            float scale = Mathf.Max(widthScale, heightScale);
            return originalSize / scale;
        }

        /// <summary>
        /// 定制概率随机数 value 总值 100 从大到小
        /// </summary>
        /// <param name="probabilityDic"></param>
        /// <returns></returns>
        public static int CustomRandom(Dictionary<uint, float> probabilityDic)
        {
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
        /// 创建总和为1的随机数组
        /// </summary>
        /// <param name="number">返回多少个随机数</param>
        /// <param name="minValue">每个随机数最小多少</param>
        /// <returns></returns>
        public static float[] CreateRandomNumbers(int number, float minValue = 0)
        {
            float retainValue = minValue * number;

            float[] randoms = new float[number];
            float sum = 0;
            for (int i = 0; i < number; i++)
            {
                randoms[i] = Random.Range(0.2f, 1f);
                sum += randoms[i];
            }

            sum *= 1 / (1 - retainValue);

            for (int i = 0; i < number; i++)
            {
                randoms[i] /= sum;
            }

            return randoms;
        }

    }
}
