using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace ToolKit
{
    /// <summary>
    /// 带断点续传的文件下载器，下载过程中先写 <c>.tmp</c> 临时文件，成功后再改名为正式文件
    /// <para>用法：构造时指定 URL，然后启动 <c>DownloadPatchSupportResume</c> 协程，通过回调获知结果</para>
    /// <code>
    /// // 文件名不传时从 URL 推导，下载到 Application.persistentDataPath
    /// var downloader = new HttpDownloader(patchUrl);
    ///
    /// downloader.StartCallback = () => DLogger.Log("Download", "开始下载");
    /// downloader.FinishedCallback = success =>
    /// {
    ///     if (success)
    ///     {
    ///         DLogger.Log("Download", $"已保存到 {downloader.LocalPath}");
    ///     }
    /// };
    ///
    /// yield return downloader.DownloadPatchSupportResume();
    ///
    /// // 下载过程中可以查询进度
    /// float progress = downloader.GetProgress(); // 0~1，未开始时为 -1
    /// </code>
    /// <para>相比 <c>ToolNet.Download</c>，本类的优势是：只写磁盘不占内存、支持中断后继续、</para>
    /// <para>通过临时文件保证不会把「下了一半的文件」误当成完整文件</para>
    /// </summary>
    /// <remarks>
    /// <para>关于临时文件与改名（本类的核心设计）：</para>
    /// <list type="bullet">
    /// <item>下载期间数据写入 <c>persistentDataPath/{文件名}.tmp</c>，正式文件在成功前一直不存在</item>
    /// <item>下载成功后 <c>.tmp</c> 被 <c>Move</c> 成正式文件，因此「正式文件存在」就等于「下载已完成」</item>
    /// <item>这样设计是为了避免续传时把「已经完整的文件」再请求一次
    /// （此时 Range 起点等于文件长度，服务端会返回 416）</item>
    /// <item>中断后 <c>.tmp</c> 会保留，下次启动同一个下载器即可从断点续传</item>
    /// </list>
    /// <para>关于 <c>forceDownload</c>：传 true 会先删掉正式文件与 <c>.tmp</c>，从头下载；</para>
    /// <para>传 false（默认）时若正式文件已存在则<strong>不做任何网络请求</strong>，直接回调成功。</para>
    /// <para>关于 416 响应码：表示 Range 起点不合法，通常意味着本地文件已经完整，
    /// 因此被<strong>视为成功</strong>而不是错误。</para>
    /// <para>关于并发：同一个实例不能同时跑两个下载，重复启动会记录日志并回调失败。
    /// 需要并行下载请创建多个实例。</para>
    /// <para>关于错误处理：本类只把失败通过回调标志告知，<strong>不抛异常也不提供错误详情</strong>，
    /// 具体原因看 <c>DLogger</c> 的日志输出。下载失败时 <c>.tmp</c> 会保留，可供下次续传。</para>
    /// <para>关于超时与重试：本类都不提供，需要时请在协程外层自行控制。</para>
    /// </remarks>
    public class HttpDownloader
    {
        private DownloadHandlerFileRange downloadHandler;

        /// <summary>
        /// 下载结束回调，参数为是否成功
        /// <para>注意「文件已存在而跳过下载」也会回调 true，无法据此区分「本次真的下载了」</para>
        /// </summary>
        public Action<bool> FinishedCallback;

        /// <summary>
        /// 下载开始回调，在收到响应头、可以确定文件总大小时触发
        /// <para>适合在这个回调里显示进度条，此前 <c>FileSize</c> 还是 0</para>
        /// </summary>
        public Action StartCallback;

        private const string Tag = "Download";

        /// <summary>URL 中推导不出文件名时使用的兜底名称</summary>
        private const string DefaultFileName = "download";

        /// <summary>URL 中标识查询串与片段的字符，解析文件名前需要连同其后的内容一起剥离</summary>
        private static readonly char[] UrlQuerySeparators = { '?', '#' };

        /// <summary>
        /// 当前是否已有下载任务在进行中
        /// <para>用于防止同一个实例被重复启动；下载结束时会被重置为 false</para>
        /// </summary>
        public bool IsDownloadStarted { get; private set; } = false;

        /// <summary>
        /// 下载完成后的正式文件路径
        /// <para>形式为 <c>{persistentDataPath}/{文件名}{扩展名}</c>，构造时即确定</para>
        /// </summary>
        public string LocalPath { get; private set; } = "";

        private string currentFileUrl;
        private string currentFullPath;

        /// <summary>
        /// 创建一个下载器并确定保存路径
        /// <code>
        /// // 文件名从 URL 推导，例如 https://host/patch.zip → patch.zip
        /// var a = new HttpDownloader("https://host/patch.zip");
        ///
        /// // 带查询串也能正确推导：先剥掉 ?v=2，得到 patch.zip
        /// var b = new HttpDownloader("https://host/patch.zip?v=2");
        ///
        /// // 显式指定保存的文件名，优先级高于 URL 推导
        /// var c = new HttpDownloader("https://host/download?id=7", "patch_v2.zip");
        /// </code>
        /// </summary>
        /// <param name="fileUrl">下载地址</param>
        /// <param name="fileName">保存的文件名（含扩展名）；留空或传 null 时从 URL 推导</param>
        /// <remarks>
        /// <para>关于路径拼接：文件一律保存在 <c>Application.persistentDataPath</c> 下，
        /// 不接受自定义目录，需要换位置请自行改造。</para>
        /// <para>关于从 URL 推导文件名：会先剥离查询串（<c>?</c> 之后）与片段（<c>#</c> 之后），
        /// 再取最后一段路径作为文件名，因此 <c>https://host/patch.zip?v=2</c> 能正确得到
        /// <c>patch.zip</c> 而不是带 <c>.zip?v=2</c> 的非法名字。</para>
        /// <para>URL 中提取不到文件名时（例如 <c>https://host/</c>），会回退到固定名称
        /// <c>download</c>，避免拼出指向目录本身的路径。</para>
        /// <para>另外，<paramref name="fileName"/> 若不含扩展名，正式文件也会没有扩展名。</para>
        /// </remarks>
        public HttpDownloader(string fileUrl, string fileName = "")
        {
            currentFileUrl = fileUrl;

            string source = string.IsNullOrEmpty(fileName)
                ? ExtractFileNameFromUrl(fileUrl)
                : fileName;

            string name = Path.GetFileNameWithoutExtension(source);
            string extension = Path.GetExtension(source);

            if (string.IsNullOrEmpty(name))
                name = DefaultFileName;

            currentFullPath = Path.Combine(Application.persistentDataPath, $"{name}.tmp");
            LocalPath = Path.Combine(Application.persistentDataPath, $"{name}{extension}");
        }

        /// <summary>
        /// 从下载地址中取出文件名部分：先剥离查询串与片段，再取最后一段路径
        /// </summary>
        /// <param name="url">完整的下载地址</param>
        /// <returns>URL 中的文件名（含扩展名）；取不到时返回空字符串</returns>
        /// <remarks>
        /// <para>不能直接对 URL 调用 <c>Path.GetFileNameWithoutExtension</c>：
        /// 查询串里的点号会让 <c>Path.GetExtension</c> 把 <c>?v=2</c> 一起当成扩展名。</para>
        /// </remarks>
        private static string ExtractFileNameFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return string.Empty;

            // 去掉 ?query 与 #fragment，否则它们会被 Path.GetExtension 当成扩展名的一部分
            int cutIndex = url.IndexOfAny(UrlQuerySeparators);
            string path = cutIndex >= 0 ? url.Substring(0, cutIndex) : url;

            int lastSlash = path.LastIndexOf('/');
            if (lastSlash >= 0)
                path = path.Substring(lastSlash + 1);

            return path;
        }

        /// <summary>
        /// 启动下载协程，支持断点续传
        /// <code>
        /// var downloader = new HttpDownloader(patchUrl);
        /// downloader.FinishedCallback = success => DLogger.Log("Download", success);
        ///
        /// // 正常下载（已存在则跳过）
        /// yield return downloader.DownloadPatchSupportResume();
        ///
        /// // 强制重新下载，忽略本地已有文件
        /// yield return downloader.DownloadPatchSupportResume(forceDownload: true);
        /// </code>
        /// </summary>
        /// <param name="forceDownload">是否强制重新下载：true 会先删除本地正式文件与临时文件；默认 false（本地已有则直接成功）</param>
        /// <returns>供 <c>StartCoroutine</c> 使用的协程</returns>
        /// <remarks>
        /// <para>关于各条路径的回调结果：</para>
        /// <list type="bullet">
        /// <item>已有任务在进行中 → 回调 <c>false</c>，不做任何事</item>
        /// <item>本地文件已存在且未强制下载 → 回调 <c>true</c>，<strong>不发请求</strong></item>
        /// <item>下载成功（含 416）→ 把 <c>.tmp</c> 改名为正式文件，回调 <c>true</c></item>
        /// <item>网络失败、HTTP 错误或改名失败 → 回调 <c>false</c>，<c>.tmp</c> 保留供续传</item>
        /// </list>
        /// <para>关于手动清理：方法内部已经调用了 <c>ManualDispose</c>，调用方不需要再管下载处理器的释放。</para>
        /// <para>关于文件名冲突：临时文件名固定为 <c>{name}.tmp</c>，
        /// 同时下载两个同名的不同文件会互相覆盖，并行下载时请给不同文件名。</para>
        /// </remarks>
        public IEnumerator DownloadPatchSupportResume(bool forceDownload = false)
        {
            if (IsDownloadStarted)
            {
                DLogger.Log(Tag, "当前下载任务进行中");
                OnFinished(false);
                yield break;
            }

            IsDownloadStarted = true;

            // 这个变量设置为true的地方，使用者就需要自己写自己的错误处理代码了
            bool isError = false;

            if (!forceDownload)
            {
                if (File.Exists(LocalPath))
                {
                    DLogger.Log(Tag, "请求文件本地已存在");
                    IsDownloadStarted = false;
                    OnFinished(true);
                    yield break;
                }
            }
            else
            {
                if (File.Exists(LocalPath))
                {
                    DLogger.Log(Tag, "强制重新下载，清理本地视频。");
                    File.Delete(LocalPath);
                }

                if (File.Exists(currentFullPath))
                {
                    File.Delete(currentFullPath);
                }
            }

            using UnityWebRequest request = UnityWebRequest.Get(currentFileUrl);
            downloadHandler = new DownloadHandlerFileRange(currentFullPath, request);
            request.downloadHandler = downloadHandler;

            downloadHandler.StartDownloadEvent += OnStartDownload;

            DLogger.Log(Tag, "请求开始");

            yield return request.SendWebRequest();

            DLogger.Log(Tag, $"请求完成 {request.isDone} {request.error}");

            if (request.isHttpError)
            {
                // 有2种方式：
                // 1. 下载的时候，文件名用.tmp这种临时的名字，下载成功之后rename，这样就不会让已经下载完毕的文件再去走下载
                // 2. 不适用临时文件名，那么在去请求Range的时候，可能就会遇到416Range非法，毕竟本地文件的Size已经等于文件总Length了
                // 这个时候就会遇到416，Range参数不对，我们认为是文件已经全部下载完成了，所以Range不对，这个时候不算错误
                if (request.responseCode != 416)
                {
                    isError = true;
                    DLogger.Log(Tag, $"请求失败 ：{request.responseCode}");
                }
            }
            else if (!string.IsNullOrEmpty(request.error))
            {
                DLogger.Log(Tag, $"请求失败 ：{request.error}");
                isError = true;
            }

            // 必须要手动清理，哪怕disposeDownloadHandlerOnDispose设置为true
            // DownloadHandlerFileRange.Dispose()是没办法被UnityWebRequest调用的
            downloadHandler.ManualDispose();

            if (!isError)
            {
                if (File.Exists(currentFullPath))
                {
                    try
                    {
                        File.Move(currentFullPath, LocalPath);
                    }
                    catch (Exception e)
                    {
                        DLogger.LogError(Tag, $"文件移动失败：{e.Message}");
                        isError = true;
                    }
                }
            }

            IsDownloadStarted = false;
            OnFinished(!isError);
        }

        /// <summary>
        /// 触发下载结束回调
        /// </summary>
        /// <param name="isDownloadCompleted">本次下载是否成功</param>
        private void OnFinished(bool isDownloadCompleted)
        {
            FinishedCallback?.Invoke(isDownloadCompleted);
        }

        /// <summary>
        /// 触发下载开始回调
        /// <para>由 <c>DownloadHandlerFileRange.StartDownloadEvent</c> 驱动，此时文件总大小已可读取</para>
        /// </summary>
        private void OnStartDownload()
        {
            StartCallback?.Invoke();
        }

        /// <summary>
        /// 查询当前下载进度
        /// <code>
        /// float progress = downloader.GetProgress();
        /// if (progress >= 0)
        /// {
        ///     progressBar.value = progress;
        /// }
        /// </code>
        /// </summary>
        /// <returns>0~1 的进度值；<strong>尚未开始下载时返回 -1</strong>，调用方需先判负</returns>
        /// <remarks>
        /// <para>返回值直接取自下载处理器的进度，而处理器只有在收到响应头之后才知道总大小，
        /// 因此刚启动协程的一小段时间里可能返回 0 或 -1，不能用来判断「是否卡住」。</para>
        /// </remarks>
        public float GetProgress()
        {
            if (downloadHandler != null)
            {
                return downloadHandler.DownloadProgress;
            }

            return -1;
        }
    }
}
