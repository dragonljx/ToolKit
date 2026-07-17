using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine.Networking;


namespace ToolKit
{

    public static class ToolNet
    {
        private const string LogTag = "ToolNet";


        #region IP

        public static List<IPAddress> GetIPv4()
        {
            List<IPAddress> result = new();


            string host = Dns.GetHostName();

            foreach (var ip in Dns.GetHostAddresses(host))
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    result.Add(ip);
                }
            }


            return result;
        }

        #endregion



        #region GET


        public static IEnumerator Get(string url, Action<NetResult<string>> callback, string token = null)
        {

            using UnityWebRequest request =
                UnityWebRequest.Get(url);


            SetToken(request, token);


            yield return request.SendWebRequest();


            callback?.Invoke(
                CreateResult<string>(
                    request,
                    request.downloadHandler.text
                )
            );
        }


        #endregion



        #region POST JSON


        public static IEnumerator PostJson(string url, string json, Action<NetResult<string>> callback)
        {

            using UnityWebRequest request =
                CreateJsonPost(url, json);


            yield return request.SendWebRequest();


            callback?.Invoke(
                CreateResult<string>(
                    request,
                    request.downloadHandler.text
                )
            );
        }



        public static IEnumerator PostJsonBytes(string url, string json, Action<NetResult<byte[]>> callback)
        {

            using UnityWebRequest request =
                CreateJsonPost(url, json);


            yield return request.SendWebRequest();


            callback?.Invoke(
                CreateResult<byte[]>(
                    request,
                    request.downloadHandler.data
                )
            );
        }



        private static UnityWebRequest CreateJsonPost(string url, string json)
        {

            UnityWebRequest request =
                new UnityWebRequest(
                    url,
                    UnityWebRequest.kHttpVerbPOST
                );


            byte[] body =
                System.Text.Encoding.UTF8.GetBytes(json);


            request.uploadHandler =
                new UploadHandlerRaw(body);


            request.downloadHandler =
                new DownloadHandlerBuffer();


            request.SetRequestHeader(
                "Content-Type",
                "application/json"
            );


            request.SetRequestHeader(
                "Accept",
                "application/json"
            );


            return request;
        }


        #endregion




        #region DELETE


        public static IEnumerator Delete(string url, Action<NetResult<string>> callback)
        {

            using UnityWebRequest request =
                UnityWebRequest.Delete(url);


            yield return request.SendWebRequest();


            callback?.Invoke(
                CreateResult<string>(
                    request,
                    request.result.ToString()
                )
            );
        }


        #endregion




        #region Download


        public static IEnumerator Download(string url, Action<byte[]> success, Action<string> error = null)
        {

            using UnityWebRequest request =
                UnityWebRequest.Get(url);


            yield return request.SendWebRequest();


            if (request.result ==
                UnityWebRequest.Result.Success)
            {
                success?.Invoke(
                    request.downloadHandler.data
                );
            }
            else
            {
                error?.Invoke(request.error);
            }
        }


        #endregion




        #region Stream


        public static IEnumerator PostStream(string url, string json, Action<byte[]> chunk, Action complete, Action<string> error)
        {

            using UnityWebRequest request =
                new UnityWebRequest(
                    url,
                    "POST"
                );


            request.uploadHandler =
                new UploadHandlerRaw(
                    System.Text.Encoding.UTF8.GetBytes(json)
                );


            request.uploadHandler.contentType =
                "application/json";


            request.downloadHandler =
                new StreamDownloadHandler(chunk);



            yield return request.SendWebRequest();


            if (request.result ==
                UnityWebRequest.Result.Success)
            {
                complete?.Invoke();
            }
            else
            {
                error?.Invoke(request.error);
            }
        }


        #endregion



        #region Helper



        private static void SetToken(UnityWebRequest request, string token)
        {
            if (!string.IsNullOrEmpty(token))
            {
                request.SetRequestHeader(
                    "Authorization",
                    $"Bearer {token}"
                );
            }
        }



        private static NetResult<T> CreateResult<T>(UnityWebRequest request, T data)
        {

            return new NetResult<T>()
            {
                Success =
                    request.result ==
                    UnityWebRequest.Result.Success,

                Error =
                    request.error,

                Data =
                    data
            };
        }


        #endregion

    }

}