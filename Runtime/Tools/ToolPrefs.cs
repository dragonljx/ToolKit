using System;
using System.Globalization;
using UnityEngine;

namespace ToolKit
{
    /// <summary>
    /// PlayerPrefs 的类型化读写封装，补充 Unity 原生接口缺失的 <c>bool</c>、<c>Vector2</c>、<c>Vector3</c>、<c>Color</c>
    /// <para>用法：每种类型都有 Get / GetOrCreate / Set 三个方法，分别对应「取值」「取值并落库」「写入」</para>
    /// <code>
    /// // 常规读写
    /// ToolPrefs.SetInt("Level", 3);
    /// int level = ToolPrefs.GetInt("Level", defaultValue: 1);
    ///
    /// // bool 是包装成 int 存储的，不需要额外处理
    /// ToolPrefs.SetBool("MusicOn", true);
    /// bool musicOn = ToolPrefs.GetBool("MusicOn", defaultValue: true);
    ///
    /// // 复合类型会被序列化成 CSV 字符串
    /// ToolPrefs.SetVector3("SpawnPos", new Vector3(1, 0, 5));
    /// Vector3 spawn = ToolPrefs.GetVector3("SpawnPos", Vector3.zero);
    ///
    /// // GetOrCreate 在键不存在时先写入默认值，适合在设置界面初始化控件
    /// Vector3 pos = ToolPrefs.GetOrCreateVector3("SpawnPos", Vector3.zero);
    ///
    /// // 写入后需要落盘才可靠
    /// ToolPrefs.Save();
    /// </code>
    /// <para>复合类型统一存成逗号分隔的字符串，并且固定用 <c>InvariantCulture</c> 格式化，</para>
    /// <para>因此切换系统语言不会导致小数点变成逗号而解析失败</para>
    /// </summary>
    /// <remarks>
    /// <para>关于 Get 与 GetOrCreate 的区别：</para>
    /// <list type="bullet">
    /// <item><c>Get</c> 在键不存在时只返回传入的默认值，<strong>不写入</strong>，不会产生副作用</item>
    /// <item><c>GetOrCreate</c> 在键不存在时先把默认值写进去再返回，之后 <c>HasKey</c> 即为 true</item>
    /// <item>需要区分「用户从未设置过」和「用户设置成了默认值」时，必须用 <c>GetOrCreate</c> 或自行调用 <c>PlayerPrefs.HasKey</c></item>
    /// </list>
    /// <para>关于 <c>bool</c>：底层是 <c>PlayerPrefs.SetInt(key, 0/1)</c>，</para>
    /// <para>读取时按「非 0 即 true」判断，因此同一个键混用 <c>SetInt</c> 与 <c>GetBool</c> 不会报错，但可读性差，建议不要混用。</para>
    /// <para>关于落盘：<c>Set*</c> 只改内存，Unity 在 <c>OnApplicationQuit</c> 时会自动 <c>Save</c>，</para>
    /// <para>但崩溃或强杀进程时可能丢失，重要数据写入后应立刻调用 <see cref="Save"/>。</para>
    /// </remarks>
    public static class ToolPrefs
    {
        #region 基础类型读取

        /// <summary>
        /// 获取 Int 类型数据（不存在时返回默认值，不写入）
        /// </summary>
        /// <param name="key">键名，不存在时返回 <paramref name="defaultValue"/></param>
        /// <param name="defaultValue">键不存在时返回的值，默认 0</param>
        /// <returns>已存储的整数，或 <paramref name="defaultValue"/></returns>
        public static int GetInt(string key, int defaultValue = 0)
        {
            return PlayerPrefs.GetInt(key, defaultValue);
        }

        /// <summary>
        /// 获取 Int 类型数据（不存在时自动创建）
        /// <para>键不存在时先写入 <paramref name="defaultValue"/> 再返回，之后 <c>HasKey</c> 为 true</para>
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="defaultValue">键不存在时写入并返回的值，默认 0</param>
        /// <returns>已存储的整数，或刚写入的 <paramref name="defaultValue"/></returns>
        public static int GetOrCreateInt(string key, int defaultValue = 0)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                SetInt(key, defaultValue);
            }
            return PlayerPrefs.GetInt(key);
        }


        /// <summary>
        /// 获取 Float 类型数据（不存在时返回默认值，不写入）
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="defaultValue">键不存在时返回的值，默认 0</param>
        /// <returns>已存储的浮点数，或 <paramref name="defaultValue"/></returns>
        public static float GetFloat(string key, float defaultValue = 0)
        {
            return PlayerPrefs.GetFloat(key, defaultValue);
        }

        /// <summary>
        /// 获取 Float 类型数据（不存在时自动创建）
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="defaultValue">键不存在时写入并返回的值，默认 0</param>
        /// <returns>已存储的浮点数，或刚写入的 <paramref name="defaultValue"/></returns>
        public static float GetOrCreateFloat(string key, float defaultValue = 0)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                SetFloat(key, defaultValue);
            }
            return PlayerPrefs.GetFloat(key);
        }


        /// <summary>
        /// 获取 Bool 类型数据（不存在时返回默认值，不写入）
        /// <para>底层读取的是 int，按「非 0 即 true」解释</para>
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="defaultValue">键不存在时返回的值，默认 false</param>
        /// <returns>已存储的布尔值，或 <paramref name="defaultValue"/></returns>
        public static bool GetBool(string key, bool defaultValue = false)
        {
            if (!PlayerPrefs.HasKey(key))
                return defaultValue;

            return PlayerPrefs.GetInt(key) != 0;
        }

        /// <summary>
        /// 获取 Bool 类型数据（不存在时自动创建）
        /// <para>键不存在时先写入 <c>0</c> 或 <c>1</c> 再读回，因此键不存在时会额外产生一次磁盘写入</para>
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="defaultValue">键不存在时写入并返回的值，默认 false</param>
        /// <returns>已存储的布尔值，或刚写入的 <paramref name="defaultValue"/></returns>
        public static bool GetOrCreateBool(string key, bool defaultValue = false)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                SetBool(key, defaultValue);
            }
            return PlayerPrefs.GetInt(key) != 0;
        }


        /// <summary>
        /// 获取 String 类型数据（不存在时返回默认值，不写入）
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="defaultValue">键不存在时返回的值，默认空串</param>
        /// <returns>已存储的字符串，或 <paramref name="defaultValue"/></returns>
        public static string GetString(string key, string defaultValue = "")
        {
            return PlayerPrefs.GetString(key, defaultValue);
        }

        /// <summary>
        /// 获取 String 类型数据（不存在时自动创建）
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="defaultValue">键不存在时写入并返回的值，默认空串</param>
        /// <returns>已存储的字符串，或刚写入的 <paramref name="defaultValue"/></returns>
        public static string GetOrCreateString(string key, string defaultValue = "")
        {
            if (!PlayerPrefs.HasKey(key))
            {
                SetString(key, defaultValue);
            }
            return PlayerPrefs.GetString(key);
        }

        #endregion


        #region 基础类型写入

        /// <summary>
        /// 写入 Int 类型数据（只改内存，需要落盘时调用 <see cref="Save"/>）
        /// </summary>
        /// <param name="key">键名，已存在时直接覆盖</param>
        /// <param name="value">要写入的整数</param>
        public static void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
        }


        /// <summary>
        /// 写入 Float 类型数据（只改内存，需要落盘时调用 <see cref="Save"/>）
        /// </summary>
        /// <param name="key">键名，已存在时直接覆盖</param>
        /// <param name="value">要写入的浮点数</param>
        public static void SetFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
        }


        /// <summary>
        /// 写入 Bool 类型数据，底层存为 <c>0</c> / <c>1</c>
        /// </summary>
        /// <param name="key">键名，已存在时直接覆盖</param>
        /// <param name="value">要写入的布尔值</param>
        public static void SetBool(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
        }


        /// <summary>
        /// 写入 String 类型数据（只改内存，需要落盘时调用 <see cref="Save"/>）
        /// </summary>
        /// <param name="key">键名，已存在时直接覆盖</param>
        /// <param name="value">要写入的字符串</param>
        public static void SetString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
        }

        #endregion


        #region Vector类型

        /// <summary>
        /// 获取 Vector2（不存在时返回默认值）
        /// <para>存储格式为 <c>"x,y"</c>，解析失败会记录错误日志并返回默认值</para>
        /// <code>
        /// Vector2 pos = ToolPrefs.GetVector2("LastPos", Vector2.zero);
        /// </code>
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="defaultValue">键不存在或解析失败时返回的值</param>
        /// <returns>解析出的 Vector2，或 <paramref name="defaultValue"/></returns>
        public static Vector2 GetVector2(string key, Vector2 defaultValue = default)
        {
            string value = GetString(key, string.Empty);

            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }

            string[] data = value.Split(',');

            if (data.Length != 2)
            {
                DLogger.LogError($"Prefs解析Vector2失败: {value}");
                return defaultValue;
            }

            if (float.TryParse(data[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                float.TryParse(data[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
            {
                return new Vector2(x, y);
            }

            DLogger.LogError($"Prefs解析Vector2失败: {value}");
            return defaultValue;
        }

        /// <summary>
        /// 获取 Vector2（不存在时自动创建）
        /// <para>与整数版本不同，键不存在时直接返回 <paramref name="defaultValue"/>，不走一遍序列化再解析</para>
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="defaultValue">键不存在时写入并返回的值</param>
        /// <returns>已存储的 Vector2，或刚写入的 <paramref name="defaultValue"/></returns>
        public static Vector2 GetOrCreateVector2(string key, Vector2 defaultValue = default)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                SetVector2(key, defaultValue);
                return defaultValue;
            }
            return GetVector2(key, defaultValue);
        }


        /// <summary>
        /// 写入 Vector2，序列化为 <c>"x,y"</c> 字符串
        /// <para>使用 <c>InvariantCulture</c>，不受系统区域设置的小数点符号影响</para>
        /// </summary>
        /// <param name="key">键名，已存在时直接覆盖</param>
        /// <param name="value">要写入的 Vector2</param>
        public static void SetVector2(string key, Vector2 value)
        {
            string data =
                $"{value.x.ToString(CultureInfo.InvariantCulture)}," +
                $"{value.y.ToString(CultureInfo.InvariantCulture)}";

            SetString(key, data);
        }



        /// <summary>
        /// 获取 Vector3（不存在时返回默认值）
        /// <para>存储格式为 <c>"x,y,z"</c>，解析失败会记录错误日志并返回默认值</para>
        /// <code>
        /// Vector3 spawn = ToolPrefs.GetVector3("SpawnPos", Vector3.zero);
        /// </code>
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="defaultValue">键不存在或解析失败时返回的值</param>
        /// <returns>解析出的 Vector3，或 <paramref name="defaultValue"/></returns>
        public static Vector3 GetVector3(string key, Vector3 defaultValue = default)
        {
            string value = GetString(key, string.Empty);

            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }

            string[] data = value.Split(',');

            if (data.Length != 3)
            {
                DLogger.LogError($"Prefs解析Vector3失败: {value}");
                return defaultValue;
            }

            if (float.TryParse(data[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                float.TryParse(data[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) &&
                float.TryParse(data[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
            {
                return new Vector3(x, y, z);
            }

            DLogger.LogError($"Prefs解析Vector3失败: {value}");
            return defaultValue;
        }

        /// <summary>
        /// 获取 Vector3（不存在时自动创建）
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="defaultValue">键不存在时写入并返回的值</param>
        /// <returns>已存储的 Vector3，或刚写入的 <paramref name="defaultValue"/></returns>
        public static Vector3 GetOrCreateVector3(string key, Vector3 defaultValue = default)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                SetVector3(key, defaultValue);
                return defaultValue;
            }
            return GetVector3(key, defaultValue);
        }


        /// <summary>
        /// 写入 Vector3，序列化为 <c>"x,y,z"</c> 字符串
        /// </summary>
        /// <param name="key">键名，已存在时直接覆盖</param>
        /// <param name="value">要写入的 Vector3</param>
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
        /// 获取 Color（不存在时返回默认值）
        /// <para>存储格式为 <c>"r,g,b,a"</c>，各分量按 0~1 的浮点存储</para>
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="defaultValue">键不存在或解析失败时返回的值</param>
        /// <returns>解析出的 Color，或 <paramref name="defaultValue"/></returns>
        /// <remarks>
        /// <para>解析失败时<strong>静默返回默认值而不打日志</strong>，与 Vector2 / Vector3 的行为不一致。</para>
        /// <para>需要排查存档里颜色值异常时，请手动检查该键的字符串内容。</para>
        /// </remarks>
        public static Color GetColor(string key, Color defaultValue = default)
        {
            string value = GetString(key, string.Empty);

            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }

            string[] data = value.Split(',');

            if (data.Length != 4)
            {
                return defaultValue;
            }

            if (float.TryParse(data[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float r) &&
                float.TryParse(data[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float g) &&
                float.TryParse(data[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float b) &&
                float.TryParse(data[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float a))
            {
                return new Color(r, g, b, a);
            }

            return defaultValue;
        }

        /// <summary>
        /// 获取 Color（不存在时自动创建）
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="defaultValue">键不存在时写入并返回的值</param>
        /// <returns>已存储的 Color，或刚写入的 <paramref name="defaultValue"/></returns>
        public static Color GetOrCreateColor(string key, Color defaultValue = default)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                SetColor(key, defaultValue);
                return defaultValue;
            }
            return GetColor(key, defaultValue);
        }


        /// <summary>
        /// 写入 Color，序列化为 <c>"r,g,b,a"</c> 字符串
        /// </summary>
        /// <param name="key">键名，已存在时直接覆盖</param>
        /// <param name="value">要写入的 Color</param>
        public static void SetColor(string key, Color value)
        {
            string data =
                $"{value.r.ToString(CultureInfo.InvariantCulture)}," +
                $"{value.g.ToString(CultureInfo.InvariantCulture)}," +
                $"{value.b.ToString(CultureInfo.InvariantCulture)}," +
                $"{value.a.ToString(CultureInfo.InvariantCulture)}";

            SetString(key, data);
        }

        #endregion


        #region 删除与保存

        /// <summary>
        /// 删除指定的键，键不存在时是安全的空操作
        /// </summary>
        /// <param name="key">要删除的键名</param>
        public static void DeleteKey(string key)
        {
            PlayerPrefs.DeleteKey(key);
        }


        /// <summary>
        /// 删除当前应用的全部 PlayerPrefs 数据
        /// <para>影响范围是整个 PlayerPrefs 存储，不只是本工具类写入的键</para>
        /// </summary>
        /// <remarks>
        /// <para>该操作不可撤销且会牵连其他系统写入的键（如 Unity Analytics、第三方 SDK）。</para>
        /// <para>只想清理自己的数据时，请改用 <see cref="DeleteKey"/> 或加统一的键名前缀后逐项删除。</para>
        /// </remarks>
        public static void DeleteAll()
        {
            PlayerPrefs.DeleteAll();
        }


        /// <summary>
        /// 把内存中的数据落盘
        /// <para>Unity 在应用退出时会自动调用，但崩溃或强杀进程时不会，重要数据建议写入后立刻调用</para>
        /// </summary>
        /// <remarks>
        /// <para>落盘是同步 IO，在移动端可能造成几毫秒到几十毫秒的卡顿，</para>
        /// <para>不要在每帧调用的逻辑里频繁 <c>Set* + Save</c>，可以攒到切场景或关界面时统一保存。</para>
        /// </remarks>
        public static void Save()
        {
            PlayerPrefs.Save();
        }

        #endregion
    }
}
