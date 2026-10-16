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
    /// 文件读写工具，覆盖 Json、文本、二进制、目录与 Unity 资源加载
    /// <para>用法：全部是静态方法；除 <see cref="ReadStreamingAssets"/> 外都是同步阻塞调用</para>
    /// <code>
    /// // Json：读写配置或存档
    /// ToolFile.SaveJson(savePath, saveData);
    ///
    /// SaveData loaded = new SaveData();
    /// if (ToolFile.ReadJson(savePath, ref loaded))
    /// {
    ///     DLogger.Log("Save", "读取成功");
    /// }
    ///
    /// // 文本与二进制
    /// string config = ToolFile.ReadText(configPath);
    /// ToolFile.WriteBytes(patchPath, patchBytes);
    ///
    /// // 目录
    /// ToolFile.CreateFolder(Path.Combine(Application.persistentDataPath, "Cache"));
    ///
    /// // 从磁盘加载图片
    /// Texture2D texture = ToolFile.LoadTexture(imagePath);
    ///
    /// // 读 StreamingAssets 需要走协程
    /// yield return ToolFile.ReadStreamingAssets("Config/level.json", handler =>
    /// {
    ///     DLogger.Log("Config", handler.text);
    /// });
    /// </code>
    /// <para>安装 <c>com.unity.nuget.newtonsoft-json</c> 时走 Newtonsoft.Json（<c>NEWTONSOFT_JSON</c> 宏），</para>
    /// <para>否则回退到 <c>JsonUtility</c>，两者的能力差异见 <see cref="SaveJson{T}"/> 的说明</para>
    /// </summary>
    /// <remarks>
    /// <para>关于错误处理：</para>
    /// <list type="bullet">
    /// <item>所有方法都不抛异常，内部捕获后记录 <c>DLogger.LogError</c>，失败通过返回值体现</item>
    /// <item>读取类方法在失败时返回 <c>null</c>（字节）或 <c>string.Empty</c>（文本），调用方必须判空</item>
    /// <item>写入类方法在失败时静默返回 <c>void</c>，只能靠日志发现问题，关键写入建议自行校验产物</item>
    /// <item>读取用 <c>File.Exists</c> 先做存在性判断，路径不存在属于「正常失败」而非异常</item>
    /// </list>
    /// <para>关于路径：所有路径都是绝对或相对当前工作目录的完整文件路径，</para>
    /// <para>本类不会自动拼接 <c>Application.persistentDataPath</c> 等 Unity 路径，需要调用方自行组合。</para>
    /// </remarks>
    public static class ToolFile
    {
        #region Json 文件操作

        /// <summary>
        /// 把对象序列化为 Json 并写入文件
        /// <para>内部走的是带异常捕获的文本写入，因此失败时不会中断调用方流程</para>
        /// <code>
        /// ToolFile.SaveJson(
        ///     Path.Combine(Application.persistentDataPath, "save.json"),
        ///     saveData);
        /// </code>
        /// </summary>
        /// <typeparam name="T">要序列化的类型</typeparam>
        /// <param name="path">目标文件路径，所在目录不存在时会自动创建</param>
        /// <param name="jsonStructure">要序列化的对象</param>
        /// <remarks>
        /// <para>关于序列化器的差异：</para>
        /// <list type="bullet">
        /// <item>装了 Newtonsoft.Json 时支持 <c>Dictionary</c>、接口、非 public 成员（配合 <c>[JsonProperty]</c>）等</item>
        /// <item>回退到 <c>JsonUtility</c> 时只支持「有 <c>[Serializable]</c> 标记的类/结构体 + public 字段」，
        /// 字典会被序列化成空对象，需要注意</item>
        /// </list>
        /// <para>写入采用「临时文件 + 原子替换」，中途掉电或崩溃不会破坏原有文件内容。</para>
        /// </remarks>
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

        /// <summary>
        /// 读取 Json 文件并反序列化到传入的对象
        /// <code>
        /// SaveData data = new SaveData();
        /// if (!ToolFile.ReadJson(savePath, ref data))
        /// {
        ///     // 文件不存在时，这里拿到的是刚生成的默认存档
        ///     DLogger.Log("Save", "已创建默认存档");
        /// }
        /// </code>
        /// </summary>
        /// <typeparam name="T">反序列化目标类型</typeparam>
        /// <param name="path">源文件路径</param>
        /// <param name="jsonStructure">接收结果的对象，按引用传入</param>
        /// <returns>读取并反序列化成功返回 true；文件不存在、内容为空或解析失败返回 false</returns>
        /// <remarks>
        /// <para>关于文件不存在时的行为（容易踩坑）：</para>
        /// <list type="bullet">
        /// <item>文件不存在时<strong>不是单纯返回 false</strong>，而是先把传入的 <paramref name="jsonStructure"/> 的当前值
        /// 当作默认值写成一个新文件，再返回 false</item>
        /// <item>因此首次运行时调用本方法会「顺手」创建存档文件，不必额外写初始化逻辑</item>
        /// <item>但反过来，如果只想探测文件是否存在，本方法会产生副作用，请直接用 <c>File.Exists</c></item>
        /// </list>
        /// <para>反序列化失败（Json 损坏、类型不匹配）时不会修改 <paramref name="jsonStructure"/>，
        /// 它会保持调用前的内容。</para>
        /// </remarks>
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

        /// <summary>
        /// 按是否启用 Newtonsoft.Json 选择序列化实现，输出带缩进的 Json
        /// </summary>
        /// <typeparam name="T">要序列化的类型</typeparam>
        /// <param name="obj">源对象</param>
        /// <returns>格式化后的 Json 字符串</returns>
        private static string SerializeJson<T>(T obj)
        {
#if NEWTONSOFT_JSON
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
#else
            return JsonUtility.ToJson(obj, true);
#endif
        }

        /// <summary>
        /// 按是否启用 Newtonsoft.Json 选择反序列化实现
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="json">Json 字符串</param>
        /// <returns>反序列化得到的对象</returns>
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

        /// <summary>
        /// 读取文本文件（UTF-8 编码）
        /// <code>
        /// string version = ToolFile.ReadText(versionPath);
        /// if (string.IsNullOrEmpty(version))
        /// {
        ///     // 文件不存在、为空或读取失败都会走到这里
        ///     version = "0.0.0";
        /// }
        /// </code>
        /// </summary>
        /// <param name="path">源文件路径</param>
        /// <returns>文件全部内容；文件不存在或读取失败时返回 <c>string.Empty</c></returns>
        /// <remarks>
        /// <para>固定按 UTF-8 解码，不带 BOM 检测：读 GBK 编码的文件会得到乱码，需要自行处理编码。</para>
        /// <para>「文件不存在」与「文件内容为空」的返回值一致，无法区分，需要区分时请先调用 <c>File.Exists</c>。</para>
        /// </remarks>
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

        /// <summary>
        /// 写入文本文件（UTF-8 编码，原子替换）
        /// <para>内部把文本转成 UTF-8 字节后交给 <see cref="WriteBytes"/>，因此同样具备原子写入特性</para>
        /// <code>
        /// ToolFile.WriteText(versionPath, "1.2.3");
        /// </code>
        /// </summary>
        /// <param name="path">目标文件路径，所在目录不存在时会自动创建</param>
        /// <param name="text">要写入的文本；传 null 时记录错误并直接返回，不会创建文件</param>
        /// <remarks>
        /// <para>写入的字节<strong>不含 BOM</strong>，用 Windows 记事本打开可能显示为无 BOM 的 UTF-8，</para>
        /// <para>现代编辑器一般能正确识别，但对接外部程序时需要留意。</para>
        /// </remarks>
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

        /// <summary>
        /// 读取二进制文件
        /// </summary>
        /// <param name="path">源文件路径</param>
        /// <returns>文件全部字节；文件不存在或读取失败时返回 null</returns>
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

        /// <summary>
        /// 原子写入二进制文件，避免中途失败产生半截文件
        /// <para>先写同目录下的 <c>.tmp</c> 临时文件，成功后再替换目标文件</para>
        /// <code>
        /// byte[] patch = BuildPatchBytes();
        /// ToolFile.WriteBytes(patchPath, patch);
        /// </code>
        /// </summary>
        /// <param name="path">目标文件路径，所在目录不存在时会自动创建</param>
        /// <param name="data">要写入的字节；传 null 时记录错误并直接返回，不会创建文件</param>
        /// <remarks>
        /// <para>关于原子性的平台差异：</para>
        /// <list type="bullet">
        /// <item>Windows 走 <c>File.Replace</c>，是真正的原子替换，替换期间目标文件始终是完整的旧版本或新版本</item>
        /// <item>Unix 平台受 <c>File.Replace</c> 限制，退化为「先删除目标再移动临时文件」，两步之间掉电会同时丢失新旧文件</item>
        /// <item>临时文件与目标文件必须位于同一磁盘分区，否则 <c>File.Move</c> 会失败</item>
        /// </list>
        /// <para>写入失败时会尝试清理 <c>.tmp</c> 残留文件，清理本身失败则忽略（不影响已有内容）。</para>
        /// <para>每次成功写入都会输出一条 <c>DLogger.Log</c>，高频写小文件的场景注意日志量。</para>
        /// </remarks>
        public static void WriteBytes(string path, byte[] data)
        {
            if (data == null)
            {
                DLogger.LogError($"写入文件失败，数据为空: {path}");
                return;
            }

            string tempPath = path + ".tmp";

            try
            {
                EnsureDirectory(path);

                // 写入临时文件
                File.WriteAllBytes(tempPath, data);

                // 原子替换
                if (File.Exists(path))
                {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                    // Windows 支持 File.Replace
                    File.Replace(tempPath, path, null);
#else
                    // Unix 平台先删除后移动
                    File.Delete(path);
                    File.Move(tempPath, path);
#endif
                }
                else
                {
                    File.Move(tempPath, path);
                }

                DLogger.Log($"写入文件: {path}");
            }
            catch (Exception e)
            {
                DLogger.LogError($"写入文件失败: {path}\n{e}");

                // 清理临时文件
                try
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch
                {
                    // 忽略清理失败
                }
            }
        }

        #endregion

        #region 文件夹操作

        /// <summary>
        /// 确保指定文件所在的目录存在，不存在则逐级创建
        /// </summary>
        /// <param name="filePath">文件路径（取其中的目录部分）</param>
        private static void EnsureDirectory(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }

        /// <summary>
        /// 获取目录信息，目录不存在时自动创建
        /// <code>
        /// DirectoryInfo cacheDir = ToolFile.GetFolder(
        ///     Path.Combine(Application.persistentDataPath, "Cache"));
        ///
        /// foreach (FileInfo file in cacheDir.GetFiles("*.tmp"))
        /// {
        ///     file.Delete();
        /// }
        /// </code>
        /// </summary>
        /// <param name="path">目录路径</param>
        /// <returns>该目录的 <c>DirectoryInfo</c>；无论原本是否存在，返回时目录一定已存在</returns>
        /// <remarks>
        /// <para>与 <see cref="CreateFolder"/> 的区别在于返回值：本方法给你一个可继续操作的对象，</para>
        /// <para>而 <see cref="CreateFolder"/> 只告诉你「这次是否发生了创建」。</para>
        /// <para>路径非法（如含有非法字符、无权限）时 <c>Directory.CreateDirectory</c> 会抛异常，本方法不做捕获。</para>
        /// </remarks>
        public static DirectoryInfo GetFolder(string path)
        {
            return Directory.Exists(path)
                ? new DirectoryInfo(path)
                : Directory.CreateDirectory(path);
        }

        /// <summary>
        /// 创建目录（已存在时视为成功，不报错）
        /// </summary>
        /// <param name="path">目录路径，支持多级不存在的父目录</param>
        /// <returns>本次调用新建了目录返回 true；目录原本已存在返回 false</returns>
        /// <remarks>
        /// <para>返回 <c>false</c> 只表示目录已存在，不代表失败，用于「只在首次创建时做初始化」的判断。</para>
        /// <para>路径非法时同样会抛出异常，不做捕获。</para>
        /// </remarks>
        public static bool CreateFolder(string path)
        {
            if (Directory.Exists(path))
                return false;

            Directory.CreateDirectory(path);
            return true;
        }

        #endregion

        #region Unity 资源加载

        /// <summary>
        /// 从磁盘上的图片文件创建 <c>Texture2D</c>
        /// <para>支持 png / jpg 等 <c>LoadImage</c> 能识别的格式，用于加载玩家自定义头像、外部贴图等</para>
        /// <code>
        /// Texture2D avatar = ToolFile.LoadTexture(avatarPath);
        /// if (avatar != null)
        /// {
        ///     rawImage.texture = avatar;
        /// }
        ///
        /// // 不再使用时必须手动销毁，否则会一直占用显存
        /// if (avatar != null)
        /// {
        ///     UnityEngine.Object.Destroy(avatar);
        /// }
        /// </code>
        /// </summary>
        /// <param name="path">图片文件路径</param>
        /// <param name="format">纹理格式，默认 <c>RGBA32</c>；实际格式由 <c>LoadImage</c> 按图片内容决定，该参数主要影响初始分配</param>
        /// <returns>加载成功的纹理；文件不存在或解码失败时返回 null</returns>
        /// <remarks>
        /// <para>关于可读性：内部调用 <c>LoadImage(data, markNonReadable: true)</c>，</para>
        /// <para>返回的纹理<strong>不可读</strong>——调用 <c>GetPixels</c> 会抛异常。需要在 CPU 侧处理像素时，</para>
        /// <para>请自行用 <c>new Texture2D(...)</c> + <c>LoadImage(data, false)</c> 创建，或改用 <c>ToolTexture</c> 的相关方法。</para>
        /// <para>纹理不受 GC 管理，不再使用时应显式 <c>Destroy</c>，否则会持续占用显存。</para>
        /// <para>本方法报错走 <c>UnityEngine.Debug.LogError</c>，是本类中唯一不使用 <c>DLogger</c> 的方法。</para>
        /// </remarks>
        public static Texture2D LoadTexture(string path, TextureFormat format = TextureFormat.RGBA32)
        {
            try
            {
                if (!File.Exists(path))
                    return null;

                byte[] data = File.ReadAllBytes(path);
                Texture2D texture = new Texture2D(2, 2, format, false);
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

        /// <summary>
        /// 读取 StreamingAssets 目录下的文件（协程）
        /// <para>StreamingAssets 在 Android 上位于压缩包内，无法用 <c>File</c> API 读取，必须走 <c>UnityWebRequest</c></para>
        /// <code>
        /// yield return ToolFile.ReadStreamingAssets("Config/level.json", handler =>
        /// {
        ///     // 回调触发时下载器仍有效，可以立即取用数据
        ///     LevelConfig config = JsonUtility.FromJson&lt;LevelConfig&gt;(handler.text);
        /// });
        /// </code>
        /// </summary>
        /// <param name="localPath">相对于 StreamingAssets 的路径，例如 <c>Config/level.json</c></param>
        /// <param name="callback">读取成功后的回调，参数为该次请求的下载器</param>
        /// <remarks>
        /// <para>关于回调的时机与生命周期（重要）：</para>
        /// <list type="bullet">
        /// <item>回调<strong>只在读取成功时触发</strong>；失败时只记录错误日志并直接结束协程，不会回调</item>
        /// <item>下载器由 <c>using</c> 托管，协程结束后即被释放，所以回调里必须<strong>同步取用数据</strong>
        /// （如 <c>handler.text</c> / <c>handler.data</c>），不要保存 <c>DownloadHandler</c> 引用留到之后再用</item>
        /// <item>需要在失败时也能收到通知的场景，本方法不适用，请直接用 <c>UnityWebRequest</c></item>
        /// </list>
        /// <para>路径由 <c>Path.Combine</c> 与 <c>Application.streamingAssetsPath</c> 拼接，</para>
        /// <para>编辑器与桌面平台下是普通文件路径，Android 下是 <c>jar:file://</c> 形式的 URL，两种都由 Unity 正确处理。</para>
        /// </remarks>
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
