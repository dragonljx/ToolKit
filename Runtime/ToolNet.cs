using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ToolKits
{
    public class PostResultString
    {
        public string Message;
        public bool Success = false;
    }

    public class PostResultBytes
    {
        public byte[] Data;
        public bool Success = false;
    }

    public static class ToolNet
    {
        const string logTag = "ToolNet";
        /// <summary>
        /// 获取IPV4
        /// </summary>
        /// <returns></returns>
        public static (List<IPAddress> ipAddresses, bool succeed) GetIPV4()
        {
            List<IPAddress> ipaddressList = new List<IPAddress>();
            string name = Dns.GetHostName();
            IPAddress[] ipadrlist = Dns.GetHostAddresses(name);
            foreach (IPAddress ipa in ipadrlist)
            {
                if (ipa.AddressFamily == AddressFamily.InterNetwork)
                {
                    ipaddressList.Add(ipa);
                }
            }

            if (ipaddressList.Count > 0)
            {
                return (ipaddressList, true);
            }
            else
            {
                return (ipaddressList, false);
            }
        }


        public static IEnumerator GetJson(string url, Action<bool, string> callback, string accessToken = "")
        {
            using var request = UnityWebRequest.Get(url);

            if (accessToken != "")
            {
                request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            }

            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                callback?.Invoke(true, request.downloadHandler.text);
            }
            else
            {
                callback?.Invoke(false, request.error);
                //Debug.LogError("Error: " + request.error);
            }
        }

        /// <summary>
        /// Http Delete
        /// </summary>
        /// <param name="url"></param>
        /// <param name="callback"></param>
        /// <param name="accessToken">访问令牌，如果没有则不填写</param>
        /// <returns></returns>
        public static IEnumerator Delete(string url, Action<bool, string> callback)
        {
            using var request = UnityWebRequest.Delete(url);

            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                callback?.Invoke(true, request.result.ToString());
            }
            else
            {
                callback?.Invoke(false, request.error);
                //Debug.LogError("Error: " + request.error);
            }
        }

        public static void Post(string urlPath, WWWForm form, Action<UnityWebRequest> callback)
        {
            TimerDispatcher.RunCoroutine(PostCoroutine(urlPath, form, callback));
        }

        public static IEnumerator PostCoroutine(string urlPath, WWWForm form, Action<UnityWebRequest> callback)
        {
            UnityWebRequest request = UnityWebRequest.Post(urlPath, form);

            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                callback?.Invoke(request);
            }
            else
            {
                TLog.LogError(logTag,$"请求失败！\n" +
                                     $"错误信息: {request.error}\n" +
                                     $"HTTP响应码: {request.responseCode}\n" +
                                     $"返回数据（如果有的话）: {request.downloadHandler.text}");
            }
        }

        public static IEnumerator Post(string urlPath, string jsonData, PostResultBytes resultBytes)
        {
            using UnityWebRequest request = UnityWebRequest.PostWwwForm(urlPath, "POST");

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("accept", "application/json");

            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            yield return request.SendWebRequest();
            resultBytes.Success = request.result == UnityWebRequest.Result.Success;
            if (resultBytes.Success)
            {
                resultBytes.Data = request.downloadHandler.data;
            }
            else
            {
                TLog.LogError(logTag,"Error: " + request.error);
            }
        }

        public static IEnumerator Post(string urlPath, string jsonData, PostResultString resultString)
        {
            using UnityWebRequest request = UnityWebRequest.PostWwwForm(urlPath, "POST");

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("accept", "application/json");

            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            yield return request.SendWebRequest();
            resultString.Success = request.result == UnityWebRequest.Result.Success;
            if (resultString.Success)
            {
                resultString.Message = request.downloadHandler.text;
            }
            else
            {
                TLog.LogError(logTag,"Error: " + request.error + urlPath + jsonData);
            }
        }

        public static IEnumerator Post(string urlPath, string jsonData, Action<bool, UnityWebRequest> callback)
        {
            using UnityWebRequest request = UnityWebRequest.PostWwwForm(urlPath, "POST");

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("accept", "application/json");

            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                callback?.Invoke(true, request);
            }
            else
            {
                callback?.Invoke(false, request);
                TLog.LogError(logTag,"Error: " + request.error);
            }
        }

        /// <summary>
        /// 使用Http Get 下载数据
        /// </summary>
        /// <param name="urlPath"></param>
        /// <param name="onSuccess"></param>
        /// <param name="onError"></param>
        /// <returns></returns>
        public static IEnumerator DownloadDataUnity(string urlPath, System.Action<byte[]> onSuccess,
            System.Action<string> onError = null)
        {
            UnityWebRequest request = UnityWebRequest.Get(urlPath);

            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                TLog.LogError(logTag,request.error);
                onError?.Invoke(request.error);
            }
            else
            {
                onSuccess?.Invoke(request.downloadHandler.data);
            }
        }

        public static Stream DownloadDataHttp(string urlPath)
        {
            try
            {
                HttpWebRequest request = WebRequest.Create(urlPath) as HttpWebRequest;
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                Stream sr = response.GetResponseStream();

                return sr;
            }
            catch (Exception e)
            {
                TLog.LogError(logTag,"Http Gain:" + e);
            }

            return null;
        }

        public static bool AcceptAllCertifications(object sender,
            System.Security.Cryptography.X509Certificates.X509Certificate certification,
            System.Security.Cryptography.X509Certificates.X509Chain chain,
            System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return sslPolicyErrors == SslPolicyErrors.None;
        }

        public static void PostStream(
            string url,
            string jsonBody,
            Action<byte[]> onChunk,
            Action onComplete,
            Action<string> onError,
            Action<UnityWebRequest> configHeaders = null
        )
        {
            TimerDispatcher.RunCoroutine(
                PostStreamCoroutine(url, jsonBody, onChunk, onComplete, onError, configHeaders));
        }

        public static IEnumerator PostStreamCoroutine(
            string url,
            string jsonBody,
            Action<byte[]> onChunk,
            Action onComplete,
            Action<string> onError,
            Action<UnityWebRequest> configHeaders = null
        )
        {
            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);


            // Upload
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.uploadHandler.contentType = "application/json";

            // Stream download
            request.downloadHandler = new StreamDownloadHandler(onChunk);

            // Header hook
            configHeaders?.Invoke(request);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                TLog.Log(logTag,request.downloadHandler.text);
                onComplete?.Invoke();
            }
            else
            {
                onError?.Invoke(request.error);
            }
        }
    }

    public class WebReqSkipCert : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return false;
        }
    }
}