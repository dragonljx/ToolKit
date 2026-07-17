using System;
using UnityEngine.Networking;
namespace ToolKit
{
    public class StreamDownloadHandler : DownloadHandlerScript
    {
        private readonly Action<byte[]> onChunk;

        public StreamDownloadHandler(Action<byte[]> onChunk)
            : base(new byte[1024 * 8]) // 接收缓冲区
        {
            this.onChunk = onChunk;
        }

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