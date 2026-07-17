using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace ToolKit
{
    public static class UnityExtension
    {
        /// <summary>
        /// 向量->弧度
        /// </summary>
        /// <param name="thisVec"></param>
        /// <returns></returns>
        public static float VectorToRad(this Vector2 thisVec)
        {
            return Mathf.Atan2(thisVec.y, thisVec.x);
        }

        /// <summary>
        /// 弧度->向量
        /// </summary>
        /// <param name="thisFloat"></param>
        /// <returns></returns>
        public static Vector2 RadToVector(this float thisFloat)
        {
            return new Vector2(Mathf.Cos(thisFloat), Mathf.Sin(thisFloat));
        }

        /// <summary>
        /// 向量->角度
        /// </summary>
        /// <param name="thisVec"></param>
        /// <returns></returns>
        public static float VectorToDeg(this Vector2 thisVec)
        {
            return Mathf.Atan2(thisVec.y, thisVec.x) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// 角度->向量
        /// </summary>
        /// <param name="thisFloat"></param>
        /// <returns></returns>
        public static Vector2 DegToVector(this float thisFloat)
        {
            return new Vector2(Mathf.Cos(thisFloat * Mathf.Deg2Rad), Mathf.Sin(thisFloat * Mathf.Deg2Rad));
        }

        /// <summary>
        /// 在不重复的情况下添加对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dic"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool AddWithout<T>(this Collection<T> dic, T obj)
        {
            if (!dic.Contains(obj))
            {
                dic.Add(obj);
                return true;
            }
            return false;
        }

        #region Debug 扩展

        public static void DebugLoop<K, V>(this IDictionary<K, V> dic)
        {
            foreach (var item in dic)
            {
                TLog.Log($"Key: {item.Key}  Value: {item.Value}");
            }
        }

        public static void DebugLoop<T>(this IEnumerable<T> strList)
        {
            foreach (var item in strList)
            {
                TLog.Log(item);
            }
        }

        public static void DebugLoop<T>(this IEnumerable<T> strList, Func<T, string> customCall = null)
        {
            if (customCall != null)
            {
                foreach (var item in strList)
                {
                    string str = customCall.Invoke(item);
                    TLog.Log(str);
                }
            }
            else
            {
                foreach (var item in strList)
                {
                    TLog.Log(item);
                }
            }
        }

        #endregion Debug 扩展

        #region 数组随机

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

        public static T GetRandomOne<T>(this T[] array)
        {
            return array[UnityEngine.Random.Range(0, array.Length)];
        }

        public static int GetRandomOneIndex<T>(this T[] array)
        {
            return UnityEngine.Random.Range(0, array.Length);
        }

        /// <summary>
        /// 洗牌算法
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
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
        /// 洗牌算法
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
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
        /// 复制 Transform
        /// </summary>
        /// <param name="tran"></param>
        /// <param name="target"></param>
        public static void CopyTransform(this Transform tran, Transform target)
        {
            tran.position = target.position;
            tran.rotation = target.rotation;
            tran.localScale = target.localScale;
        }

        /// <summary>
        /// 设置 Transform
        /// </summary>
        /// <param name="tran"></param>
        /// <param name="target"></param>
        public static void SetTransformAll(this Transform tran, Vector3 pos, Vector3 size, Quaternion rotate)
        {
            tran.position = pos;
            tran.rotation = rotate;
            tran.localScale = size;
        }

        /// <summary>
        /// 清零 Transform
        /// </summary>
        /// <param name="tran"></param>
        public static void ResetTransform(this Transform tran)
        {
            tran.localPosition = Vector3.zero;
            tran.localRotation = Quaternion.identity;
            tran.localScale = Vector3.one;
        }

        /// <summary>
        /// 获取所有子 Transform，可按名称过滤。
        /// </summary>
        /// <param name="parent">父对象</param>
        /// <param name="filterName">可选的名称包含关键字</param>
        /// <returns>符合条件的子 Transform 列表</returns>
        public static List<Transform> GetAllChildren(this Transform parent, string filterName = null)
        {
            List<Transform> result = new List<Transform>();
            CollectChildren(parent, result, filterName);
            return result;
        }

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