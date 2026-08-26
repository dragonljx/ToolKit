using System;
using System.Globalization;
using UnityEngine;

namespace ToolKit
{
    /// <summary>
    /// PlayerPrefs 数据读写工具
    /// </summary>
    public static class ToolPrefs
    {
        #region 基础类型读取

        /// <summary>
        /// 获取 Int 类型数据
        /// </summary>
        public static int GetInt(string key, int defaultValue = 0)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                SetInt(key, defaultValue);
                return defaultValue;
            }

            return PlayerPrefs.GetInt(key);
        }


        /// <summary>
        /// 获取 Float 类型数据
        /// </summary>
        public static float GetFloat(string key, float defaultValue = 0)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                SetFloat(key, defaultValue);
                return defaultValue;
            }

            return PlayerPrefs.GetFloat(key);
        }


        /// <summary>
        /// 获取 Bool 类型数据
        /// </summary>
        public static bool GetBool(string key, bool defaultValue = false)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                SetBool(key, defaultValue);
                return defaultValue;
            }

            return PlayerPrefs.GetInt(key) != 0;
        }


        /// <summary>
        /// 获取 String 类型数据
        /// </summary>
        public static string GetString(string key, string defaultValue = "")
        {
            if (!PlayerPrefs.HasKey(key))
            {
                SetString(key, defaultValue);
                return defaultValue;
            }

            return PlayerPrefs.GetString(key);
        }

        #endregion


        #region 基础类型写入

        public static void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
        }


        public static void SetFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
        }


        public static void SetBool(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
        }


        public static void SetString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
        }

        #endregion


        #region Vector类型

        /// <summary>
        /// 获取 Vector2
        /// </summary>
        public static Vector2 GetVector2(string key, Vector2 defaultValue = default)
        {
            string value = GetString(key, string.Empty);

            if (string.IsNullOrEmpty(value))
            {
                SetVector2(key, defaultValue);
                return defaultValue;
            }


            string[] data = value.Split(',');

            if (data.Length != 2)
            {
                DLogger.LogError($"Prefs解析Vector2失败:{value}");
                return defaultValue;
            }


            if (float.TryParse(data[0], NumberStyles.Float,
                    CultureInfo.InvariantCulture, out float x) &&
                float.TryParse(data[1], NumberStyles.Float,
                    CultureInfo.InvariantCulture, out float y))
            {
                return new Vector2(x, y);
            }


            DLogger.LogError($"Prefs解析Vector2失败:{value}");
            return defaultValue;
        }


        public static void SetVector2(string key, Vector2 value)
        {
            string data =
                $"{value.x.ToString(CultureInfo.InvariantCulture)}," +
                $"{value.y.ToString(CultureInfo.InvariantCulture)}";

            SetString(key, data);
        }



        /// <summary>
        /// 获取 Vector3
        /// </summary>
        public static Vector3 GetVector3(string key, Vector3 defaultValue = default)
        {
            string value = GetString(key, string.Empty);

            if (string.IsNullOrEmpty(value))
            {
                SetVector3(key, defaultValue);
                return defaultValue;
            }


            string[] data = value.Split(',');

            if (data.Length != 3)
            {
                DLogger.LogError($"Prefs解析Vector3失败:{value}");
                return defaultValue;
            }


            if (float.TryParse(data[0], NumberStyles.Float,
                    CultureInfo.InvariantCulture, out float x) &&
                float.TryParse(data[1], NumberStyles.Float,
                    CultureInfo.InvariantCulture, out float y) &&
                float.TryParse(data[2], NumberStyles.Float,
                    CultureInfo.InvariantCulture, out float z))
            {
                return new Vector3(x, y, z);
            }


            DLogger.LogError($"Prefs解析Vector3失败:{value}");
            return defaultValue;
        }


        public static void SetVector3(string key, Vector3 value)
        {
            string data =
                $"{value.x.ToString(CultureInfo.InvariantCulture)}," +
                $"{value.y.ToString(CultureInfo.InvariantCulture)}," +
                $"{value.z.ToString(CultureInfo.InvariantCulture)}";

            SetString(key, data);
        }

        #endregion


        #region Unity类型


        /// <summary>
        /// 获取 Color
        /// </summary>
        public static Color GetColor(string key, Color defaultValue = default)
        {
            string value = GetString(key, string.Empty);

            if (string.IsNullOrEmpty(value))
            {
                SetColor(key, defaultValue);
                return defaultValue;
            }


            string[] data = value.Split(',');

            if (data.Length != 4)
            {
                return defaultValue;
            }


            if (float.TryParse(data[0], out float r) &&
                float.TryParse(data[1], out float g) &&
                float.TryParse(data[2], out float b) &&
                float.TryParse(data[3], out float a))
            {
                return new Color(r, g, b, a);
            }


            return defaultValue;
        }


        public static void SetColor(string key, Color value)
        {
            SetString(
                key,
                $"{value.r},{value.g},{value.b},{value.a}"
            );
        }


        #endregion


        #region 删除与保存


        /// <summary>
        /// 删除指定Key
        /// </summary>
        public static void DeleteKey(string key)
        {
            PlayerPrefs.DeleteKey(key);
        }


        /// <summary>
        /// 删除全部数据
        /// </summary>
        public static void DeleteAll()
        {
            PlayerPrefs.DeleteAll();
        }


        /// <summary>
        /// 保存数据到磁盘
        /// </summary>
        public static void Save()
        {
            PlayerPrefs.Save();
        }


        #endregion
    }
}