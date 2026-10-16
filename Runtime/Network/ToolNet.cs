using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine.Networking;


namespace ToolKit
{
    /// <summary>
    /// 基于 <c>UnityWebRequest</c> 的网络请求工具，提供 GET / POST / DELETE / 下载 / 流式响应
    /// <para>用法：所有请求方法都是协程，需要通过 <c>StartCoroutine</c> 启动；结果通过回调返回</para>
    /// <code>
    /// // GET 请求
    /// StartCoroutine(ToolNet.Get(url, result =>
    /// {
    ///     if (result.Success)
    ///     {
    ///         DLogger.Log("Network", result.Data);
    ///     }
    ///     else
    ///     {
    ///         DLogger.LogError("Network", result.Error);
    ///     }
    /// }));
    ///
    /// // 带鉴权的 GET
    /// StartCoroutine(ToolNet.Get(url, OnResult, token: "your-token"));
    ///
    /// // JSON POST
    /// string json = JsonUtility.ToJson(payload);
    /// StartCoroutine(ToolNet.PostJson(url, json, result => DLogger.Log(result.Data)));
    ///
    /// // 下载文件到字节数组
    /// StartCoroutine(ToolNet.Download(fileUrl,
    ///     bytes => ToolFile.WriteBytes(savePath, bytes),
    ///     error => DLogger.LogError("Network", error)));
    /// </code>
    /// <para>所有接口都不抛异常：网络层的错误会被折叠进 <c>NetResult.Success</c> 与 <c>NetResult.Error</c>，</para>
    /// <para>调用方先判 <c>Success</c> 再使用 <c>Data</c> 即可</para>
    /// </summary>
    /// <remarks>
    /// <para>关于回调时机：无论成功还是失败回调<strong>都会被调用</strong>，
    /// 因此不能用「回调有没有进来」判断请求是否成功，必须检查 <c>Success</c>。</para>
    /// <para>关于鉴权：只有 <see cref="Get"/> 提供 <c>token</c> 参数，会以 <c>Authorization: Bearer</c> 头发出；</para>
    /// <para>POST 与 DELETE 没有该参数，需要鉴权的接口请自行改用 <c>UnityWebRequest</c> 或后续扩展本类。</para>
    /// <para>关于超时：本类不设置超时，默认跟随 <c>UnityWebRequest.timeout</c> 的全局行为（0 表示不超时），</para>
    /// <para>请求长期挂起时协程会一直等待，需要超时控制请自行设置。</para>
    /// <para>关于大文件：<see cref="Download"/> 会把整个响应体读进内存，</para>
    /// <para>体积较大的补丁包请改用 <c>HttpDownloader</c>，它支持断点续传和分块写入。</para>
    /// </remarks>
    public static class ToolNet
    {
        #region IP

        /// <summary>
        /// 获取本机所有 IPv4 地址
        /// <code>
        /// List&lt;IPAddress&gt; addresses = ToolNet.GetIPv4();
        /// foreach (IPAddress ip in addresses)
        /// {
        ///     DLogger.Log("Network", ip.ToString());
        /// }
        /// </code>
        /// </summary>
        /// <returns>本机 IPv4 地址列表；解析失败时返回空列表（不会返回 null）</returns>
        /// <remarks>
        /// <para>实现是先取主机名再反查地址，属于<strong>同步阻塞</strong>调用，DNS 响应慢时会卡住调用线程。</para>
        /// <para>结果里可能同时包含回环地址 <c>127.0.0.1</c>、局域网地址和虚拟网卡地址，</para>
        /// <para>需要挑选「对外可用的那个」时得自行过滤，本方法不做筛选，也不保证顺序。</para>
        /// <para>返回的是全部 IPv4 地址，不包含 IPv6。</para>
        /// </remarks>
        public static List<IPAddress> GetIPv4()
        {
            List<IPAddress> result = new List<IPAddress>();

            try
            {
                string host = Dns.GetHostName();

                foreach (var ip in Dns.GetHostAddresses(host))
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        result.Add(ip);
                    }
                }
            }
            catch (Exception e)
            {
                DLogger.LogError($"获取IP地址失败: {e}");
            }

            return result;
        }

        #endregion



        #region GET

        /// <summary>
        /// 发起 HTTP GET 请求，把响应体作为文本返回
        /// <code>
        /// StartCoroutine(ToolNet.Get(configUrl, result =>
        /// {
        ///     if (!result.Success)
        ///     {
        ///         DLogger.LogError("Network", result.Error);
        ///         return;
        ///     }
        ///
        ///     Config config = JsonUtility.FromJson&lt;Config&gt;(result.Data);
        /// }));
        /// </code>
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="callback">完成回调，成功与失败都会调用，需判 <c>Success</c>；传 null 时请求照常发出</param>
        /// <param name="token">Bearer 令牌，非空时会加上 <c>Authorization</c> 请求头；默认不加</param>
        /// <returns>供 <c>StartCoroutine</c> 使用的协程</returns>
        /// <remarks>
        /// <para>失败时 <c>Data</c> 仍会被填充为下载器的文本内容，通常是空串或服务端返回的错误页，
        /// 不要在不判 <c>Success</c> 的情况下解析它。</para>
        /// <para>响应文本按 UTF-8 解码，服务端返回其他编码时可能出现乱码。</para>
        /// </remarks>
        public static IEnumerator Get(string url, Action<NetResult<string>> callback, string token = null)
        {
            using UnityWebRequest request = UnityWebRequest.Get(url);

            SetToken(request, token);

            yield return request.SendWebRequest();

            callback?.Invoke(CreateResult<string>(request, request.downloadHandler.text));
        }

        #endregion



        #region POST JSON

        /// <summary>
        /// 发起 JSON POST 请求，把响应体作为文本返回
        /// <code>
        /// string body = JsonUtility.ToJson(new LoginRequest { user = "a", pwd = "b" });
        ///
        /// StartCoroutine(ToolNet.PostJson(loginUrl, body, result =>
        /// {
        ///     if (result.Success)
        ///     {
        ///         DLogger.Log("Network", result.Data);
        ///     }
        /// }));
        /// </code>
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="json">请求体，需已是合法的 JSON 字符串；不会做转义或校验</param>
        /// <param name="callback">完成回调，成功与失败都会调用</param>
        /// <returns>供 <c>StartCoroutine</c> 使用的协程</returns>
        /// <remarks>
        /// <para>请求头固定为 <c>Content-Type: application/json</c> 与 <c>Accept: application/json</c>。</para>
        /// <para>没有 <c>token</c> 参数，需要鉴权的接口得自行改造。</para>
        /// <para>请求体按 UTF-8 编码后一次性提交，没有分块上传能力。</para>
        /// </remarks>
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



        /// <summary>
        /// 发起 JSON POST 请求，把响应体作为原始字节返回
        /// <para>用于响应不是文本的场景，例如接口返回 Protobuf、MessagePack 或图片二进制</para>
        /// <code>
        /// StartCoroutine(ToolNet.PostJsonBytes(apiUrl, json, result =>
        /// {
        ///     if (result.Success)
        ///     {
        ///         byte[] payload = result.Data;
        ///     }
        /// }));
        /// </code>
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="json">请求体，需已是合法的 JSON 字符串</param>
        /// <param name="callback">完成回调，成功与失败都会调用</param>
        /// <returns>供 <c>StartCoroutine</c> 使用的协程</returns>
        /// <remarks>
        /// <para>与文本版本共用同一套请求构造逻辑，只有取值方式不同（<c>data</c> 而非 <c>text</c>）。</para>
        /// <para>失败时 <c>Data</c> 可能是空数组而不是 null，判空逻辑请同时检查长度。</para>
        /// </remarks>
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



        /// <summary>
        /// 构造一个 JSON POST 请求：JSON 请求体 + 缓冲区响应 + JSON 请求头
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="json">请求体 JSON 字符串，按 UTF-8 编码</param>
        /// <returns>尚未发送的请求对象，由调用方负责释放（两个 POST 方法都用 <c>using</c> 接管）</returns>
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


        /// <summary>
        /// 发起 HTTP DELETE 请求
        /// <code>
        /// StartCoroutine(ToolNet.Delete($"{apiUrl}/items/{id}", result =>
        /// {
        ///     DLogger.Log("Network", $"删除{(result.Success ? "成功" : "失败")}");
        /// }));
        /// </code>
        /// </summary>
        /// <param name="url">请求地址，通常需要带上要删除资源的 ID</param>
        /// <param name="callback">完成回调，成功与失败都会调用</param>
        /// <returns>供 <c>StartCoroutine</c> 使用的协程</returns>
        /// <remarks>
        /// <para>关于 <c>Data</c> 字段（与直觉不同）：它<strong>不是响应体内容</strong>，
        /// 而是 <c>UnityWebRequest.Result</c> 枚举的字符串形式（如 <c>"Success"</c>）。</para>
        /// <para>原因是 <c>UnityWebRequest.Delete</c> 不挂载下载处理器，取不到响应体。</para>
        /// <para>需要读取服务端返回内容的场景请改用 <c>UnityWebRequest</c> 自行构造请求。</para>
        /// <para>同样没有 <c>token</c> 参数。</para>
        /// </remarks>
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


        /// <summary>
        /// 下载文件到字节数组
        /// <para>成功与失败走两条独立的回调，比 <c>NetResult</c> 的写法更直接</para>
        /// <code>
        /// StartCoroutine(ToolNet.Download(patchUrl,
        ///     bytes =>
        ///     {
        ///         ToolFile.WriteBytes(localPath, bytes);
        ///         DLogger.Log("Download", $"已保存 {bytes.Length} 字节");
        ///     },
        ///     error => DLogger.LogError("Download", error)));
        /// </code>
        /// </summary>
        /// <param name="url">下载地址</param>
        /// <param name="success">下载成功回调，参数为完整的响应字节</param>
        /// <param name="error">下载失败回调，参数为 <c>UnityWebRequest.error</c>；传 null 时静默忽略失败</param>
        /// <returns>供 <c>StartCoroutine</c> 使用的协程</returns>
        /// <remarks>
        /// <para>没有进度回调，也没有断点续传能力，整个文件会被一次性读入内存。</para>
        /// <para>需要大文件下载、进度显示或续传时，请改用 <c>HttpDownloader</c>。</para>
        /// </remarks>
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


        /// <summary>
        /// 发起 POST 请求并以分块方式接收响应
        /// <para>用于服务端流式输出（类似 SSE）的场景，例如 AI 接口逐段返回文本</para>
        /// <code>
        /// StartCoroutine(ToolNet.PostStream(chatUrl, json,
        ///     chunk => AppendText(Encoding.UTF8.GetString(chunk)), // 收到一块就处理一块
        ///     ()   => DLogger.Log("Stream", "接收完成"),
        ///     err  => DLogger.LogError("Stream", err)));
        /// </code>
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="json">请求体 JSON 字符串，按 UTF-8 编码并设置 <c>Content-Type: application/json</c></param>
        /// <param name="chunk">每收到一块数据时调用，参数为该块的字节内容</param>
        /// <param name="complete">整个响应接收完成时调用</param>
        /// <param name="error">请求失败时调用，参数为 <c>UnityWebRequest.error</c></param>
        /// <returns>供 <c>StartCoroutine</c> 使用的协程</returns>
        /// <remarks>
        /// <para>关于分块边界：分块由底层网络缓冲决定，<strong>不保证按行或按语义切分</strong>，
        /// 一块数据可能截断在半个 UTF-8 字符或多字节序列中间。</para>
        /// <para>需要按行解析时，请在回调里自行维护缓冲区并只处理完整的行，
        /// 把不完整的部分留到下一块拼上。</para>
        /// <para>失败时不会调用 <paramref name="complete"/>，也不会调用已经收到的块之外的任何回调。</para>
        /// </remarks>
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



        /// <summary>
        /// 给请求加上 Bearer 鉴权头
        /// </summary>
        /// <param name="request">目标请求</param>
        /// <param name="token">令牌，为空时不做任何处理</param>
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



        /// <summary>
        /// 把请求结果包装成统一的 <c>NetResult</c>
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="request">已完成的请求</param>
        /// <param name="data">要附带的数据，失败时同样会带上</param>
        /// <returns>填充好成功标志、错误信息与数据的统一结果对象</returns>
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
