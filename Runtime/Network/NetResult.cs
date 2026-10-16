namespace ToolKit
{
    /// <summary>
    /// 网络请求的统一返回结构，把「是否成功」「错误信息」「数据」打包成一个对象
    /// <para>用法：作为 <c>ToolNet</c> 各接口回调的参数，先判 <see cref="Success"/> 再取 <see cref="Data"/></para>
    /// <code>
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
    /// </code>
    /// <para>三个字段都是 public 字段而非属性，属于简单的数据传输对象，构造后直接赋值即可</para>
    /// </summary>
    /// <remarks>
    /// <para>关于失败时的字段状态：</para>
    /// <list type="bullet">
    /// <item><see cref="Success"/> 为 false 时，<see cref="Data"/> 通常是 <c>null</c> 或类型默认值，
    /// <strong>不要在没有判成功的情况下直接使用 Data</strong></item>
    /// <item><see cref="Error"/> 只在失败时才有意义，成功时可能仍是 null</item>
    /// <item>引用类型的 <c>Data</c> 判空用 <c>== null</c> 即可，本类不含 Unity 对象，不涉及「假 null」问题</item>
    /// </list>
    /// <para>本类不包含请求的状态码字段，失败原因全部压缩在 <see cref="Error"/> 字符串里，</para>
    /// <para>需要按状态码做分支处理时，请直接用 <c>UnityWebRequest</c>。</para>
    /// </remarks>
    /// <typeparam name="T">请求返回的数据类型，如 <c>string</c>（文本）或 <c>byte[]</c>（二进制）</typeparam>
    public class NetResult<T>
    {
        /// <summary>
        /// 请求是否成功完成
        /// <para>为 false 时不要使用 <see cref="Data"/>，具体原因见 <see cref="Error"/></para>
        /// </summary>
        public bool Success;

        /// <summary>
        /// 失败原因，来自 <c>UnityWebRequest.error</c> 或本地异常信息
        /// <para>成功时为 null，仅在 <see cref="Success"/> 为 false 时有意义</para>
        /// </summary>
        public string Error;

        /// <summary>
        /// 请求返回的数据
        /// <para>成功时是解析后的结果；失败时通常为 null 或类型默认值</para>
        /// </summary>
        public T Data;
    }
}
