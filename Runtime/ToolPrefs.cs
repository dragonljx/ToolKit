using System;
using UnityEngine;
namespace ToolKit
{
    /// <summary>
    /// 注册表信息读写
    /// </summary>
    public static class ToolPrefs
    {

        public static void GetPrefs(string key, ref int value)
        {

            if (PlayerPrefs.HasKey(key))
            {
                value = PlayerPrefs.GetInt(key);
            }
            else
            {
                SetPrefs(key, value);
            }
        }
        public static void SetPrefs(string name, int value)
        {

            PlayerPrefs.SetInt(name, value);
        }

        public static void GetPrefs(string key, ref float value)
        {
            if (PlayerPrefs.HasKey(key))
            {
                value = PlayerPrefs.GetFloat(key);

            }
            else
            {
                SetPrefs(key, value);

            }
        }
        public static void SetPrefs(string key, float value)
        {

            PlayerPrefs.SetFloat(key, value);

        }

        public static void GetPrefs(string key, ref bool value)
        {
            if (PlayerPrefs.HasKey(key))
            {
                value = PlayerPrefs.GetInt(key) != 0;
            }
            else
            {
                SetPrefs(key, value);
            }
        }
        public static void SetPrefs(string key, bool value)
        {

            PlayerPrefs.SetInt(key, value ? 1 : 0);

        }


        public static void GetPrefs(string key, ref string value)
        {
            if (PlayerPrefs.HasKey(key))
            {
                value = PlayerPrefs.GetString(key);
            }
            else
            {
                SetPrefs(key, value);
            }
        }
        public static void SetPrefs(string key, string value)
        {
            PlayerPrefs.SetString(key, value);

        }

        public static void GetPrefs(string key, ref Vector3 value)
        {

            if (PlayerPrefs.HasKey(key))
            {
                string str = PlayerPrefs.GetString(key);
                string[] data = str.Split("\r\n", StringSplitOptions.None);
                if (data.Length == 3)
                {
                    value = new Vector3(float.Parse(data[0]), float.Parse(data[1]), float.Parse(data[2]));
                }
                else
                {
                    Debug.LogError($"Prefs 解析 Vector3 错误:{str}");
                    value = Vector3.zero;
                }
            }
            else
            {
                SetPrefs(key, value);
            }


        }

        public static void SetPrefs(string name, Vector3 value)
        {
            string x = Convert.ToString(value.x);
            string y = Convert.ToString(value.y);
            string z = Convert.ToString(value.z);
            PlayerPrefs.SetString(name, $"{x}\r\n{y}\r\n{z}");

        }
        public static void GetPrefs(string key, ref Vector2 value)
        {

            if (PlayerPrefs.HasKey(key))
            {
                string str = PlayerPrefs.GetString(key);
                string[] data = str.Split("\r\n", StringSplitOptions.None);
                if (data.Length == 2)
                {
                    value = new Vector2(float.Parse(data[0]), float.Parse(data[1]));
                }
                else
                {
                    Debug.LogError($"Prefs 解析 Vector2 错误:{str}");
                    value = Vector2.zero;
                }
            }
            else
            {
                SetPrefs(key, value);
            }

        }
        public static void SetPrefs(string name, Vector2 value)
        {
            string x = Convert.ToString(value.x);
            string y = Convert.ToString(value.y);
            PlayerPrefs.SetString(name, $"{x}\r\n{y}");
        }
        public static void DelecteAllPrefs()
        {
            PlayerPrefs.DeleteAll();
        }
        public static void DeleteAt(string name)
        {
            PlayerPrefs.DeleteKey(name);
        }
    }
}
