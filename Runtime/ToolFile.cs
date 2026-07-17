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
    public static class ToolFile
    {
        public static void SaveJson<T>(string path, T jsonStructure)
        {
            string str;

#if NEWTONSOFT_JSON
            str = JsonConvert.SerializeObject(jsonStructure);
#else
            str = JsonUtility.ToJson(jsonStructure);
#endif
            WriteFile(path, str);
        }

        public static bool ReadJson<T>(string path, ref T jsonStructure)
        {
            string str = ReadStringFile(path, true);
            try
            {
                if (string.IsNullOrEmpty(str))
                {
                    SaveJson(path, jsonStructure);
                    return false;
                }

#if NEWTONSOFT_JSON
                jsonStructure = JsonConvert.DeserializeObject<T>(str);
#else
                jsonStructure = JsonUtility.FromJson<T>(str);
#endif
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"初始化Json失败: {e.Message}");
                return false;
            }
        }

        #region 文件读写

        /// <summary>
        /// 读取文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="autoCreate"></param>
        /// <returns></returns>
        public static string ReadStringFile(string path, bool autoCreate = false)
        {
            FileStream file = GetFile(path, autoCreate);
            if (file == null)
            {
                return string.Empty;
            }

            try
            {
                using (file) // 确保资源释放
                {
                    byte[] buffer = new byte[file.Length];
                    int read = file.Read(buffer, 0, buffer.Length);
                    return Encoding.UTF8.GetString(buffer, 0, read);
                }
            }
            catch (Exception e)
            {
                TLog.LogError($"文件读取错误: {path}, 错误: {e.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 读取文件
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static byte[] ReadFile(string path, bool autoCreate = false)
        {
            FileStream file = GetFile(path, autoCreate);
            if (file == null)
            {
                return null;
            }

            try
            {
                using (file)
                {
                    byte[] buffer = new byte[file.Length];
                    int read = file.Read(buffer, 0, buffer.Length);
                    if (read == buffer.Length)
                    {
                        return buffer;
                    }

                    byte[] actualData = new byte[read];
                    Buffer.BlockCopy(buffer, 0, actualData, 0, read);
                    return actualData;
                }
            }
            catch (Exception e)
            {
                TLog.LogError($"文件读取错误:{path} {e}");
                return null;
            }
        }

        /// <summary>
        /// 写入文件
        /// </summary>
        /// <param name="str"></param>
        /// <param name="path"></param>
        public static void WriteFile(string path, string str)
        {
            WriteFile(path, System.Text.Encoding.UTF8.GetBytes(str));
        }

        /// <summary>
        /// 写入文件
        /// </summary>
        /// <param name="str"></param>
        /// <param name="path"></param>
        public static void WriteFile(string path, byte[] data)
        {
            if (data == null)
            {
                TLog.LogError($"写入文件失败: 数据为空 -> {path}");
                return;
            }

            FileStream file = GetFile(path, true);
            if (file == null)
            {
                TLog.LogError($"写入文件失败: 无法获取文件流 -> {path}");
                return;
            }

            try
            {
                using (file) // 确保资源释放
                {
                    // 直接清空后写入
                    file.SetLength(0);
                    file.Write(data, 0, data.Length);
                }

                TLog.Log($"写入文件到: {path}");
            }
            catch (Exception ex)
            {
                TLog.LogError($"写入文件失败: {path}, 错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="autoCreate"></param>
        /// <returns></returns>
        public static FileStream GetFile(string path, bool autoCreate)
        {
            FileInfo info = new FileInfo(path);

            try
            {
                if (!string.IsNullOrEmpty(info.DirectoryName) && !Directory.Exists(info.DirectoryName))
                {
                    Directory.CreateDirectory(info.DirectoryName);
                }

                if (info.Exists)
                {
                    return info.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                }
                else if (autoCreate)
                {
                    TLog.LogWarning($"未找到文件，新创建: {path}");
                    return info.Create();
                }
                else
                {
                    TLog.LogWarning($"未找到文件: {path}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                TLog.LogError($"打开文件失败: {path}, 错误: {ex.Message}");
                return null;
            }
        }

        public static DirectoryInfo GetFolder(string path)
        {
            if (Directory.Exists(path))
            {
                DirectoryInfo dir = new DirectoryInfo(path);
                return dir;
            }
            else
            {
                return Directory.CreateDirectory(path);
            }
        }

        /// <summary>
        /// 创建文件夹
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool CreateFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 加载图片
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Texture2D LoadTexture(string path)
        {
            try
            {
                FileInfo file = new FileInfo(path);
                if (!file.Exists)
                    return null;

                byte[] data = File.ReadAllBytes(file.FullName);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(data);
                tex.name = Path.GetFileNameWithoutExtension(file.Name);
                return tex;
            }
            catch (Exception ex)
            {
                Debug.LogError($"加载图片失败: {ex.Message}");
                return null;
            }
        }

        public static IEnumerator ReadStreamingAssets(string localPath, Action<DownloadHandler> endCallback)
        {
            var uri = new System.Uri(Path.Combine(Application.streamingAssetsPath, localPath));
            UnityWebRequest www = UnityWebRequest.Get(uri);
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.ConnectionError)
            {
                TLog.Log(www.error);
            }
            else
            {
                endCallback.Invoke(www.downloadHandler);
            }
        }

        #endregion 文件读写
    }
}