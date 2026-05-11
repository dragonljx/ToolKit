#if NEWTONSOFT_JSON
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endif
using System;
using System.IO;
using UnityEngine;

namespace ToolKits
{
#if NEWTONSOFT_JSON
    public static class Json
    {
        public static void RedJson<T>(ref T data, string jsonName = "Settings")where T : new()
        {
            var path = Path.Combine(Application.persistentDataPath, $"{jsonName}.json");

            string jsonStr = ToolKits.ToolFile.ReadStringFile(path);

            if (string.IsNullOrEmpty(jsonStr))
            {
                if (data == null)
                    data = new T();

                jsonStr = JsonConvert.SerializeObject(data, Formatting.Indented,CreateSettings());
                ToolKits.ToolFile.WriteFile(path, jsonStr);
            }
            else
            {
                try
                {
 
                    data = JsonConvert.DeserializeObject<T>(jsonStr,CreateSettings());
                }
                catch (Exception ex)
                {
                    TLog.LogError($"JSON 反序列化失败: {path}, 错误: {ex.Message}");
                    // 如果反序列化失败，用默认对象代替，避免 data 为 null
                    data = new T();
                }
            }
        }

        public static void SaveJson<T>(T data, string jsonName = "Settings")
        {
            if (data != null)
            {
                var path = Path.Combine(Application.persistentDataPath, $"{jsonName}.json");
                var jsonStr = JsonConvert.SerializeObject(data,CreateSettings());
                ToolFile.WriteFile(path, jsonStr);
            }
            else
            {
                TLog.LogError("保存数据未初始化！");
            }
        }

        static JsonSerializerSettings CreateSettings()
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple // 避免程序集全名太长
            };
            return settings;
        }
    }
#endif
}