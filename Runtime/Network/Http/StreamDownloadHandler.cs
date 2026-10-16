using System;
using UnityEngine.Networking;

namespace ToolKit
{
    /// <summary>
    /// 流式响应处理器：每收到一块数据就立刻回调，不在内存里攒完整响应
    /// <para>用法：作为 <c>UnityWebRequest.downloadHandler</c> 传入，配合服务端流式输出使用</para>
    /// <code>
    /// UnityWebRequest request = new UnityWebRequest(url, "POST");
    /// request.downloadHandler = new StreamDownloadHandler(chunk =>
    /// {
    ///     // 收到一块就处理一块，不必等整个响应结束
    ///     DLogger.Log("Stream", System.Text.Encoding.UTF8.GetString(chunk));
    /// });
    ///
    /// yield return request.SendWebRequest();
    /// </code>
    /// <para>与 <c>DownloadHandlerBuffer</c> 的区别是「边收边给」：后者要把全部数据收完才拿得到内容，</para>
    /// <para>长连接场景下会让用户一直看不到反馈；本类则可以让界面持续输出</para>
    /// </summary>
    /// <remarks>
    /// <para>关于分块边界（接入流式接口时最容易出问题的地方）：</para>
    /// <list type="bullet">
    /// <item>每块的大小由底层网络缓冲决定，<strong>与业务上的行、消息边界无关</strong></item>
    /// <item>一块数据可能刚好切在一个 UTF-8 多字节字符中间，直接 <c>Encoding.UTF8.GetString</c> 会得到乱码</item>
    /// <item>按行解析时（如 SSE 的 <c>data: ...\n\n</c>）需要自己维护缓冲区，只处理完整的行，
    /// 把尾部不完整的部分留到下一块拼接</item>
    /// </list>
    /// <para>关于内存：每收到一块都会 <c>new</c> 一个等长数组再回调（避免复用底层缓冲导致数据被覆盖），</para>
    /// <para>因此高频小块数据会产生较多 GC 分配；总量大的流式响应建议评估一下分配压力。</para>
    /// <para>关于中断：<c>ReceiveData</c> 返回 false 会中止下载，本类在数据为 null 或长度为 0 时就返回 false，
    /// 这意味着服务端发送一个空块会导致请求被提前终止。</para>
    /// </remarks>
    public class StreamDownloadHandler : DownloadHandlerScript
    {
        private readonly Action<byte[]> onChunk;

        /// <summary>
        /// 创建一个流式响应处理器
        /// </summary>
        /// <param name="onChunk">每收到一块数据时调用，参数是该块的字节副本，允许为 null</param>
        public StreamDownloadHandler(Action<byte[]> onChunk)
            : base(new byte[1024 * 8]) // 接收缓冲区
        {
            this.onChunk = onChunk;
        }

        /// <summary>
        /// 接收一块数据并转发给回调
        /// </summary>
        /// <param name="data">底层接收缓冲区</param>
        /// <param name="dataLength">本块的有效字节数</param>
        /// <returns>继续接收返回 true；数据为空或长度非正时返回 false 并中止下载</returns>
        /// <remarks>
        /// <para>返回的 <paramref name="data"/> 是会被复用的底层缓冲区，因此这里先拷贝一份再回调，
        /// 否则回调里保存的引用会在下一块到达时被覆盖。</para>
        /// </remarks>
        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            if (data == null || dataLength <= 0)
                return false;

            byte[] chunk = new byte[dataLength];
            Buffer.BlockCopy(data, 0, chunk, 0, dataLength);

            onChunk?.Invoke(chunk);

            return true;
        }
    }
}
