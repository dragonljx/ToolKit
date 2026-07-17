using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace ToolKit
{
    /// <summary>
    /// 使用方式:
    /// UnityWebRequest unityWebRequest = new UnityWebRequest("url");
    /// unityWebRequest.downloadHandler = new DownloadHandlerFileRange("文件保存的路径", unityWebRequest);
    /// unityWebRequest.SendWebRequest();
    ///
    /// 参考自： https://gist.github.com/TouiSoraHe/19f2afeee1334cb8e42b3c8f2c283a65
    /// 更完整的使用方式见：DownloadPatchSupportResume()方法
    /// </summary>
    public class DownloadHandlerFileRange : DownloadHandlerScript, IDisposable
    {
        /// <summary>
        /// 文件正式开始下载事件,此事件触发以后即可获取到文件的总大小
        /// </summary>
        public event System.Action StartDownloadEvent;

        #region 属性

        /// <summary>
        /// 下载速度,单位:KB/S 保留两位小数
        /// </summary>
        public float Speed
        {
            get { return ((int)(downloadSpeed / 1024 * 100)) / 100.0f; }
        }

        /// <summary>
        /// 文件的总大小
        /// </summary>
        public long FileSize
        {
            get { return totalFileSize; }
        }

        /// <summary>
        /// 下载进度[0,1]
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
        /// 使用1MB的缓存,在补丁2017.2.1p1中对DownloadHandlerScript的优化中,目前最大传入数据量也仅仅是1024*1024,再多也没用
        /// </summary>
        /// <param name="path">文件保存的路径</param>
        /// <param name="request">UnityWebRequest对象,用来获文件大小,设置断点续传的请求头信息</param>
        public DownloadHandlerFileRange(string path, UnityWebRequest request) : base(new byte[1024 * 1024])
        {
            TLog.Log(tag: "Download", "下载开始");

            this.path = path;
            fileStream = new FileStream(this.path, FileMode.Append, FileAccess.Write);
            unityWebRequest = request;
            if (File.Exists(path))
            {
                localFileSize = new System.IO.FileInfo(path).Length;
            }

            curFileSize = localFileSize;
            unityWebRequest.SetRequestHeader("Range", "bytes=" + localFileSize + "-");

            TLog.Log("Download", $"本地文件大小{localFileSize}");
        }

        /// <summary>
        /// 清理资源,该方法没办法重写,只能隐藏,如果想要强制中止下载,并清理资源(UnityWebRequest.Dispose()),该方法并不会被调用,这让人很苦恼
        /// </summary>
        public override void Dispose()
        {
            TLog.Log("Download", "Dispose()");
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;

            if (disposing)
            {
                Clean(); // 清理托管资源
            }

            disposed = true;
        }

        public void ManualDispose()
        {
            TLog.Log("Download", "手动关闭");
            Clean();
        }

        #endregion 公共方法

        #region 私有方法

        /// <summary>
        /// 关闭文件流
        /// </summary>
        private void Clean()
        {
            TLog.Log("Download", "关闭下载流");

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
            TLog.Log("Download", "下载完成清理");
            base.CompleteContent();
            Clean();
        }

        /// <summary>
        /// 调用UnityWebRequest.downloadHandler.data属性时,将会调用该方法,用于以byte[]的方式返回下载的数据,目前总是返回null
        /// </summary>
        /// <returns></returns>
        protected override byte[] GetData()
        {
            Debug.Log("[Download] GetData()");
            return null;
        }

        /// <summary>
        /// 调用UnityWebRequest.downloadProgress属性时,将会调用该方法,用于返回下载进度
        /// </summary>
        /// <returns></returns>
        protected override float GetProgress()
        {
            return totalFileSize == 0 ? 0 : ((float)curFileSize) / totalFileSize;
        }

        /// <summary>
        /// 调用UnityWebRequest.downloadHandler.text属性时,将会调用该方法,用于以string的方式返回下载的数据,目前总是返回null
        /// </summary>
        /// <returns></returns>
        protected override string GetText()
        {
            return null;
        }

        //Note:当下载的文件数据大于2G时,该int类型的参数将会数据溢出,所以先自己通过响应头来获取长度,获取不到再使用参数的方式
        protected override void ReceiveContentLength(int contentLength)
        {
            TLog.Log("Download", $"接收长度{contentLength}");
            string contentLengthStr = unityWebRequest.GetResponseHeader("Content-Length");

            if (!string.IsNullOrEmpty(contentLengthStr))
            {
                try
                {
                    totalFileSize = long.Parse(contentLengthStr);
                }
                catch (System.FormatException e)
                {
                    TLog.Log("Download", $"获取文件长度失败，{contentLengthStr},{e.Message}");

                    totalFileSize = contentLength;
                }
                catch (System.Exception e)
                {
                    TLog.Log("Download", $"获取文件长度失败，{contentLengthStr},{e.Message}");

                    totalFileSize = contentLength;
                }
            }
            else
            {
                totalFileSize = contentLength;
            }

            if (unityWebRequest.responseCode == 200)
            {
                TLog.Log("Download", "服务器不支持 Range 请求，准备清除旧文件");
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

        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            if (data == null || dataLength == 0 || unityWebRequest.responseCode > 400)
            {
                TLog.Log("Download", $"接收数据返回错误 数据：{data} 长度：{dataLength} 响应码：{unityWebRequest.responseCode}");

                return false;
            }

            try
            {
                fileStream.Write(data, 0, dataLength);
            }
            catch (IOException ex)
            {
                TLog.Log("Download", $"写入文件失败：{ex.Message}");
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

        ~DownloadHandlerFileRange()
        {
            TLog.Log("Download", " ~DownloadHandlerFileRange()");
            Dispose(false);
        }

        #endregion 私有继承的方法
    }
}