using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

#if NEWTONSOFT_JSON
using Newtonsoft.Json;
#endif

namespace ToolKit
{
    /// <summary>
    /// 文件工具类
    /// 提供 Json、文本、二进制、目录、Unity资源文件操作
    /// </summary>
    public static class ToolFile
    {
        #region Json 文件操作

        public static void SaveJson<T>(string path, T jsonStructure)
        {
            try
            {
                string json = SerializeJson(jsonStructure);
                WriteText(path, json);
            }
            catch (Exception e)
            {
                DLogger.LogError($"保存Json失败: {path}\n{e}");
            }
        }

        public static bool ReadJson<T>(string path, ref T jsonStructure)
        {
            try
            {
                if (!File.Exists(path))
                {
                    SaveJson(path, jsonStructure);
                    return false;
                }

                string json = ReadText(path);
                if (string.IsNullOrWhiteSpace(json))
                    return false;

                jsonStructure = DeserializeJson<T>(json);
                return true;
            }
            catch (Exception e)
            {
                DLogger.LogError($"读取Json失败: {path}\n{e}");
                return false;
            }
        }

        private static string SerializeJson<T>(T obj)
        {
#if NEWTONSOFT_JSON
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
#else
            return JsonUtility.ToJson(obj, true);
#endif
        }

        private static T DeserializeJson<T>(string json)
        {
#if NEWTONSOFT_JSON
            return JsonConvert.DeserializeObject<T>(json);
#else
            return JsonUtility.FromJson<T>(json);
#endif
        }

        #endregion

        #region 文本文件操作

        public static string ReadText(string path)
        {
            try
            {
                return File.Exists(path)
                    ? File.ReadAllText(path, Encoding.UTF8)
                    : string.Empty;
            }
            catch (Exception e)
            {
                DLogger.LogError($"读取文本失败: {path}\n{e}");
                return string.Empty;
            }
        }

        public static void WriteText(string path, string text)
        {
            if (text == null)
            {
                DLogger.LogError($"写入文本失败，内容为空: {path}");
                return;
            }

            WriteBytes(path, Encoding.UTF8.GetBytes(text));
        }

        #endregion

        #region 二进制文件操作

        public static byte[] ReadBytes(string path)
        {
            try
            {
                return File.Exists(path) ? File.ReadAllBytes(path) : null;
            }
            catch (Exception e)
            {
                DLogger.LogError($"读取文件失败: {path}\n{e}");
                return null;
            }
        }

        public static void WriteBytes(string path, byte[] data)
        {
            if (data == null)
            {
                DLogger.LogError($"写入文件失败，数据为空: {path}");
                return;
            }

            try
            {
                EnsureDirectory(path);

                string tempPath = path + ".tmp";
                File.WriteAllBytes(tempPath, data);
                File.Copy(tempPath, path, true);
                File.Delete(tempPath);

                DLogger.Log($"写入文件: {path}");
            }
            catch (Exception e)
            {
                DLogger.LogError($"写入文件失败: {path}\n{e}");
            }
        }

        #endregion

        #region 文件夹操作

        private static void EnsureDirectory(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }

        public static DirectoryInfo GetFolder(string path)
        {
            return Directory.Exists(path)
                ? new DirectoryInfo(path)
                : Directory.CreateDirectory(path);
        }

        public static bool CreateFolder(string path)
        {
            if (Directory.Exists(path))
                return false;

            Directory.CreateDirectory(path);
            return true;
        }

        #endregion

        #region Unity 资源加载

        public static Texture2D LoadTexture(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return null;

                byte[] data = File.ReadAllBytes(path);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.LoadImage(data, true);
                texture.name = Path.GetFileNameWithoutExtension(path);

                return texture;
            }
            catch (Exception e)
            {
                Debug.LogError($"加载图片失败: {path}\n{e}");
                return null;
            }
        }

        public static IEnumerator ReadStreamingAssets(string localPath, Action<DownloadHandler> callback)
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, localPath);
            using (UnityWebRequest request = UnityWebRequest.Get(fullPath))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    DLogger.LogError($"读取StreamingAssets失败: {request.error}");
                    yield break;
                }

                callback?.Invoke(request.downloadHandler);
            }
        }

        #endregion
    }
}