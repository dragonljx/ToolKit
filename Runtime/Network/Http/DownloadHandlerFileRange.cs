using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace ToolKit
{
    /// <summary>
    /// 支持断点续传的下载处理器：边收边写文件，并通过 <c>Range</c> 请求头从断点处继续
    /// <para>用法：作为 <c>UnityWebRequest.downloadHandler</c> 传入，下载完成后<strong>必须手动</strong>调用
    /// <see cref="ManualDispose"/></para>
    /// <code>
    /// UnityWebRequest request = UnityWebRequest.Get(fileUrl);
    /// var handler = new DownloadHandlerFileRange("下载到的路径", request);
    ///
    /// // 订阅后可以拿到文件总大小，用于初始化进度条
    /// handler.StartDownloadEvent += () =>
    /// {
    ///     DLogger.Log("Download", $"文件总大小 {handler.FileSize} 字节");
    /// };
    ///
    /// request.downloadHandler = handler;
    /// yield return request.SendWebRequest();
    ///
    /// // 关键：必须手动清理，否则文件流不会被关闭
    /// handler.ManualDispose();
    /// </code>
    /// <para>更完整的用法（含断点续传、416 处理、临时文件改名）见 <c>HttpDownloader.DownloadPatchSupportResume</c></para>
    /// </summary>
    /// <remarks>
    /// <para>关于断点续传的原理：</para>
    /// <list type="bullet">
    /// <item>构造时若目标文件已存在，就取它的大小并设置请求头 <c>Range: bytes={已下载大小}-</c>，
    /// 只请求剩余部分</item>
    /// <item>收到数据后以追加模式写入同一个文件，因此文件必须<strong>边下边写</strong>，
    /// 中途失败时文件里保留的是「已下载的部分」，下次可继续</item>
    /// <item>服务端若不支持 Range（响应码 200 而非 206），会删掉旧文件从头下载</item>
    /// <item>返回 416（Range 不合法）通常意味着本地文件已完整，调用方应把它当成成功处理</item>
    /// </list>
    /// <para>关于资源释放（最容易出问题的地方）：</para>
    /// <list type="bullet">
    /// <item><strong>必须手动调用 <see cref="ManualDispose"/></strong>，Unity 在销毁
    /// <c>UnityWebRequest</c> 时不会调用本类重写的 <c>Dispose</c>，文件流不会被关闭</item>
    /// <item>析构函数走的是 <c>Dispose(false)</c>，它<strong>不会</strong>清理文件流，
    /// 只把 <c>disposed</c> 置为 true，因此忘记手动清理时依赖 <c>FileStream</c> 自己的终结器兜底</item>
    /// <item>清理过的处理器再次写入会因文件流为 null 而抛异常，不要重复使用同一个实例</item>
    /// </list>
    /// <para>关于 2GB 以上文件：<c>ReceiveContentLength</c> 的 <c>int</c> 参数在超大文件下会溢出，
    /// 因此实现优先从响应头读 <c>Content-Length</c> 并用 <c>long</c> 解析，读不到才退回参数值。</para>
    /// <para>本类会输出较多 <c>DLogger</c> 日志（每次接收回调、每次清理），排查完建议按需降噪。
    /// 参考自：https://gist.github.com/TouiSoraHe/19f2afeee1334cb8e42b3c8f2c283a65</para>
    /// </remarks>
    public class DownloadHandlerFileRange : DownloadHandlerScript, IDisposable
    {
        /// <summary>
        /// 文件正式开始下载事件,此事件触发以后即可获取到文件的总大小
        /// </summary>
        public event System.Action StartDownloadEvent;

        #region 属性

        /// <summary>
        /// 下载速度,单位:KB/S 保留两位小数
        /// <para>每秒采样一次，下载刚开始或长时间无数据时会停留在上一次的值</para>
        /// </summary>
        public float Speed
        {
            get { return ((int)(downloadSpeed / 1024 * 100)) / 100.0f; }
        }

        /// <summary>
        /// 文件的总大小
        /// <para>单位字节，已包含本地续传部分的大小；<see cref="StartDownloadEvent"/> 触发前为 0</para>
        /// </summary>
        public long FileSize
        {
            get { return totalFileSize; }
        }

        /// <summary>
        /// 下载进度[0,1]
        /// <para>总大小未知时返回 0，因此不能用「进度是否为 0」判断是否开始下载</para>
        /// </summary>
        public float DownloadProgress
        {
            get { return GetProgress(); }
        }

        #endregion 属性

        private readonly string path; //文件保存的路径
        private FileStream fileStream;
        private UnityWebRequest unityWebRequest;
        private long localFileSize = 0; //本地已经下载的文件的大小
        private long totalFileSize = 0; //文件的总大小
        private long curFileSize = 0; //当前的文件大小
        private float lastTime = 0; //用作下载速度的时间统计
        private float lastDataSize = 0; //用来作为下载速度的大小统计
        private float downloadSpeed = 0; //下载速度,单位:Byte/S
        private bool disposed = false;

        #region 公共方法

        /// <summary>
        /// 创建一个支持断点续传的下载处理器
        /// <para>构造时会立即打开文件流并设置 <c>Range</c> 请求头，因此要在 <c>SendWebRequest</c> 之前构造</para>
        /// </summary>
        /// <param name="path">文件保存的路径；文件已存在时视为「已下载一部分」，会以追加模式打开并请求剩余部分</param>
        /// <param name="request">要绑定的请求对象，用来读取文件大小与设置断点续传的请求头</param>
        /// <remarks>
        /// <para>关于缓冲区：使用 1MB 的接收缓冲，这是 <c>DownloadHandlerScript</c> 实际会传入的单次最大数据量，
        /// 再大也不会提升吞吐。</para>
        /// <para>关于目录：本方法直接 <c>new FileStream</c>，<strong>不会自动创建父目录</strong>，
        /// 路径所在的文件夹不存在会抛 <c>DirectoryNotFoundException</c>。</para>
        /// <para>关于续传的前提：传入的 <paramref name="path"/> 必须与上次中断时是<strong>同一个文件</strong>，
        /// 否则会把两个不同文件的内容拼在一起。</para>
        /// </remarks>
        public DownloadHandlerFileRange(string path, UnityWebRequest request) : base(new byte[1024 * 1024])
        {
            DLogger.Log(tag: "Download", "下载开始");

            this.path = path;
            fileStream = new FileStream(this.path, FileMode.Append, FileAccess.Write);
            unityWebRequest = request;
            if (File.Exists(path))
            {
                localFileSize = new System.IO.FileInfo(path).Length;
            }

            curFileSize = localFileSize;
            unityWebRequest.SetRequestHeader("Range", "bytes=" + localFileSize + "-");

            DLogger.Log("Download", $"本地文件大小{localFileSize}");
        }

        /// <summary>
        /// 清理资源,该方法没办法重写,只能隐藏,如果想要强制中止下载,并清理资源(UnityWebRequest.Dispose()),该方法并不会被调用,这让人很苦恼
        /// </summary>
        /// <remarks>
        /// <para>Unity 在销毁请求时不会走到这里，所以实际业务代码请调用 <see cref="ManualDispose"/>。</para>
        /// </remarks>
        public override void Dispose()
        {
            DLogger.Log("Download", "Dispose()");
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 标准的 Dispose 实现，按是否需要清理托管资源决定是否关闭文件流
        /// </summary>
        /// <param name="disposing">true 表示由显式调用触发，此时才清理文件流</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;

            if (disposing)
            {
                Clean(); // 清理托管资源
            }

            disposed = true;
        }

        /// <summary>
        /// 手动清理资源，关闭文件流
        /// <para>下载结束后<strong>必须</strong>调用，这是本类唯一可靠的释放入口</para>
        /// <code>
        /// yield return request.SendWebRequest();
        /// handler.ManualDispose();
        /// </code>
        /// </summary>
        /// <remarks>
        /// <para>重复调用是安全的，<see cref="Clean"/> 内部会判空文件流。</para>
        /// <para>清理之后本实例不可再用于下载，再次接收数据会因文件流为 null 而抛异常。</para>
        /// </remarks>
        public void ManualDispose()
        {
            DLogger.Log("Download", "手动关闭");
            Clean();
        }

        #endregion 公共方法

        #region 私有方法

        /// <summary>
        /// 关闭文件流
        /// <para>先 <c>Flush</c> 再 <c>Dispose</c>，确保缓冲区里的数据落到磁盘</para>
        /// </summary>
        private void Clean()
        {
            DLogger.Log("Download", "关闭下载流");

            downloadSpeed = 0.0f;
            if (fileStream != null)
            {
                fileStream.Flush();
                fileStream.Dispose();
                fileStream = null;
            }
        }

        #endregion 私有方法

        #region 私有继承的方法

        /// <summary>
        /// 下载完成后清理资源
        /// </summary>
        protected override void CompleteContent()
        {
            DLogger.Log("Download", "下载完成清理");
            base.CompleteContent();
            Clean();
        }

        /// <summary>
        /// 调用UnityWebRequest.downloadHandler.data属性时,将会调用该方法,用于以byte[]的方式返回下载的数据,目前总是返回null
        /// </summary>
        /// <returns>恒为 null，数据已经写进文件而不是留在内存里</returns>
        protected override byte[] GetData()
        {
            Debug.Log("[Download] GetData()");
            return null;
        }

        /// <summary>
        /// 调用UnityWebRequest.downloadProgress属性时,将会调用该方法,用于返回下载进度
        /// </summary>
        /// <returns>已下载字节数占总大小的比例；总大小未知时返回 0</returns>
        protected override float GetProgress()
        {
            return totalFileSize == 0 ? 0 : ((float)curFileSize) / totalFileSize;
        }

        /// <summary>
        /// 调用UnityWebRequest.downloadHandler.text属性时,将会调用该方法,用于以string的方式返回下载的数据,目前总是返回null
        /// </summary>
        /// <returns>恒为 null，数据已经写进文件而不是留在内存里</returns>
        protected override string GetText()
        {
            return null;
        }

        /// <summary>
        /// 收到响应头后确定文件总大小，并在服务端不支持断点续传时重置本地文件
        /// </summary>
        /// <param name="contentLength">本次响应将要传输的字节数</param>
        /// <remarks>
        /// <para>关于 2GB 以上文件：<paramref name="contentLength"/> 是 <c>int</c>，超过 2GB 会溢出，
        /// 因此优先从响应头读 <c>Content-Length</c> 并用 <c>long</c> 解析，读不到才退回参数值。</para>
        /// <para>关于断点续传被拒：响应码为 200（而非 206）说明服务端忽略了 <c>Range</c> 头，
        /// 此时会删掉已下载的部分从头开始，避免把完整文件追加到旧内容后面。</para>
        /// <para>关于总大小：续传场景下 <c>Content-Length</c> 只是<strong>待下载部分</strong>的大小，
        /// 所以要把 <c>localFileSize</c> 加回去才是完整文件大小。</para>
        /// </remarks>
        //Note:当下载的文件数据大于2G时,该int类型的参数将会数据溢出,所以先自己通过响应头来获取长度,获取不到再使用参数的方式
        protected override void ReceiveContentLength(int contentLength)
        {
            DLogger.Log("Download", $"接收长度{contentLength}");
            string contentLengthStr = unityWebRequest.GetResponseHeader("Content-Length");

            if (!string.IsNullOrEmpty(contentLengthStr))
            {
                try
                {
                    totalFileSize = long.Parse(contentLengthStr);
                }
                catch (System.FormatException e)
                {
                    DLogger.Log("Download", $"获取文件长度失败，{contentLengthStr},{e.Message}");

                    totalFileSize = contentLength;
                }
                catch (System.Exception e)
                {
                    DLogger.Log("Download", $"获取文件长度失败，{contentLengthStr},{e.Message}");

                    totalFileSize = contentLength;
                }
            }
            else
            {
                totalFileSize = contentLength;
            }

            if (unityWebRequest.responseCode == 200)
            {
                DLogger.Log("Download", "服务器不支持 Range 请求，准备清除旧文件");
                Clean();

                if (File.Exists(path))
                {
                    File.Delete(path);
                    fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
                    localFileSize = 0;
                    curFileSize = 0;
                }
            }
            //这里拿到的下载大小是待下载的文件大小,需要加上本地已下载文件的大小才等于总大小
            totalFileSize += localFileSize;
            lastTime = UnityEngine.Time.time;
            lastDataSize = curFileSize;
            if (StartDownloadEvent != null)
            {
                StartDownloadEvent();
            }
        }

        /// <summary>
        /// 接收一块数据并追加写入文件，同时统计下载速度
        /// </summary>
        /// <param name="data">接收缓冲区</param>
        /// <param name="dataLength">本块的有效字节数</param>
        /// <returns>继续接收返回 true；数据异常、响应码错误或写入失败时返回 false 并中止下载</returns>
        /// <remarks>
        /// <para>返回 false 会直接中止请求，因此磁盘写满（<c>IOException</c>）会导致下载中断，
        /// 但已写入的部分会保留在文件里，下次调用可续传。</para>
        /// <para>速度统计每秒采样一次，两次采样之间的 <c>Speed</c> 保持不变。</para>
        /// </remarks>
        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            if (data == null || dataLength == 0 || unityWebRequest.responseCode > 400)
            {
                DLogger.Log("Download", $"接收数据返回错误 数据：{data} 长度：{dataLength} 响应码：{unityWebRequest.responseCode}");

                return false;
            }

            try
            {
                fileStream.Write(data, 0, dataLength);
            }
            catch (IOException ex)
            {
                DLogger.Log("Download", $"写入文件失败：{ex.Message}");
                return false;
            }

            curFileSize += dataLength;
            //统计下载速度
            if (UnityEngine.Time.time - lastTime >= 1.0f)
            {
                downloadSpeed = (curFileSize - lastDataSize) / (UnityEngine.Time.time - lastTime);
                lastTime = UnityEngine.Time.time;
                lastDataSize = curFileSize;
            }

            return true;
        }

        /// <summary>
        /// 终结器，兜底释放非托管资源
        /// <para>注意它调用的是 <c>Dispose(false)</c>，<strong>不会</strong>关闭文件流</para>
        /// </summary>
        ~DownloadHandlerFileRange()
        {
            DLogger.Log("Download", " ~DownloadHandlerFileRange()");
            Dispose(false);
        }

        #endregion 私有继承的方法
    }
}
