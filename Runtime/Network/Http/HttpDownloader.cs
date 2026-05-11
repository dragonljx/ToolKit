using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace ToolKits
{
    public class HttpDownloader
    {
        private DownloadHandlerFileRange downloadHandler;
        public Action<bool> FinishedCallback;
        public Action StartCallback;
        private const string Tag = "Download";
        public bool IsDownloadStarted { get; private set; } = false;
        public string LocalPath { get; private set; } = "";
        private string currentFileUrl;
        private string currentFullPath;

        public HttpDownloader(string fileUrl, string fileName = "")
        {
            currentFileUrl = fileUrl;
            bool isFileNameProvided = fileName != "";
            string name = Path.GetFileNameWithoutExtension(isFileNameProvided ? fileName : fileUrl);
            string extension = Path.GetExtension(isFileNameProvided ? fileName : fileUrl);
            currentFullPath = Path.Combine(Application.persistentDataPath, $"{name}.tmp");
            LocalPath = Path.Combine(Application.persistentDataPath, $"{name}{extension}");
        }

        public IEnumerator DownloadPatchSupportResume(bool forceDownload = false)
        {
            if (IsDownloadStarted)
            {
                TLog.Log(Tag, "当前下载任务进行中");
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
                    TLog.Log(Tag, "请求文件本地已存在");
                    IsDownloadStarted = false;
                    OnFinished(true);
                    yield break;
                }
            }
            else
            {
                if (File.Exists(LocalPath))
                {
                    TLog.Log(Tag, "强制重新下载，清理本地视频。");
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

            TLog.Log(Tag, "请求开始");

            yield return request.SendWebRequest();

            TLog.Log(Tag, $"请求完成 {request.isDone} {request.error}");

            if (request.isHttpError)
            {
                // 有2种方式：
                // 1. 下载的时候，文件名用.tmp这种临时的名字，下载成功之后rename，这样就不会让已经下载完毕的文件再去走下载
                // 2. 不适用临时文件名，那么在去请求Range的时候，可能就会遇到416Range非法，毕竟本地文件的Size已经等于文件总Length了
                // 这个时候就会遇到416，Range参数不对，我们认为是文件已经全部下载完成了，所以Range不对，这个时候不算错误
                if (request.responseCode != 416)
                {
                    isError = true;
                    TLog.Log(Tag, $"请求失败 ：{request.responseCode}");
                }
            }
            else if (!string.IsNullOrEmpty(request.error))
            {
                TLog.Log(Tag, $"请求失败 ：{request.error}");
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
                        File.Move(currentFullPath,  LocalPath);

                    }
                    catch (Exception e)
                    {
                        TLog.LogError(Tag,$"文件移动失败：{e.Message}");
                        isError = true;
                    }
                }
            }

            IsDownloadStarted = false;
            OnFinished(!isError);
        }

        private void OnFinished(bool isDownloadCompleted)
        {
            FinishedCallback?.Invoke(isDownloadCompleted);
        }

        private void OnStartDownload()
        {
            StartCallback?.Invoke();
        }

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