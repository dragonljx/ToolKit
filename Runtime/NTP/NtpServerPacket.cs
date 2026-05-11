using System;
using System.Buffers.Binary;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ToolKits
{

    public class NtpServerPacket : IDisposable
    {
        public static DateTime BaseDate = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private byte[] _bytes;
        ///// <summary>
        ///// Adds the Transmit timestamp when getter is used for most accurate result
        ///// </summary>
        //public byte[] Bytes { get => InsertTransmitTime(); protected set => _bytes = value; }
        public DateTime UTCTime => CalculateUTCTime();


        CancellationTokenSource cancelToken = new CancellationTokenSource();
        Task task;
        UdpClient udpServer;

        public NtpServerPacket(int serverPort = 8123)
        {
            _bytes = new byte[48];

            udpServer = new UdpClient((int)serverPort);
            task = new Task(RunNtpServer, cancelToken.Token, TaskCreationOptions.LongRunning);
            task.Start();

        }

        async void RunNtpServer()
        {
            try
            {
                while (!cancelToken.IsCancellationRequested)
                {
                    var received = await udpServer.ReceiveAsync();
                    PrepBytes(received.Buffer);
                    await udpServer.SendAsync(InsertTransmitTime(), 48, received.RemoteEndPoint);
                    Debug.Log($"Responded to {received.RemoteEndPoint.Address} with {UTCTime.ToLocalTime()}");
                }
            }
            catch (System.Exception)
            {
                Debug.Log("End NTP Server" );
            }

        }
        private byte[] InsertTransmitTime()
        {
            var transmitTime = GetTimeStamp();
            ChangeUIntBE(transmitTime.sec, 40);
            ChangeUIntBE(transmitTime.frac, 44);
            return _bytes;
        }

        private void PrepBytes(byte[] data)
        {
            Array.Clear(_bytes, 0, data.Length);

            var arrival = GetTimeStamp();
            var reference = arrival.sec - 60; //Fake reference
            byte firstByte = (byte)(data[0] & 0b00111000); //Copy version from client
            firstByte = (byte)(firstByte | 0b00000100); //Set mode 4 = server
            _bytes[0] = firstByte;
            _bytes[1] = 1; //Stratum 
            _bytes[2] = data[2]; //copy poll
            _bytes[3] = unchecked((byte)(-6)); //Precision
                                               //Skip 8 Bytes Root Delay Root Dispersion
                                               //LOCL as reference
            _bytes[12] = 0x4C;
            _bytes[13] = 0x4F;
            _bytes[14] = 0x43;
            _bytes[15] = 0x4C;
            ChangeUIntBE(reference, 16);
            //Copy client transmit
            data.AsSpan(40, 8).CopyTo(_bytes.AsSpan(24, 8));
            ChangeUIntBE(arrival.sec, 32);
            ChangeUIntBE(arrival.frac, 36);
        }

        private (uint sec, uint frac) GetTimeStamp()
        {
            var nowUtc = DateTime.Now.ToUniversalTime();
            var seconds = (nowUtc.Subtract(BaseDate).TotalSeconds);
            var secs = (uint)Math.Floor(seconds);
            var fractions = (uint)((seconds - secs) * (1UL << 32));
            return (secs, fractions);
        }
        private DateTime CalculateUTCTime()
        {
            ulong seconds = BinaryPrimitives.ReadUInt32BigEndian(_bytes.AsSpan(40, 4));
            ulong fraction = BinaryPrimitives.ReadUInt32BigEndian(_bytes.AsSpan(44, 4));
            var dt = BaseDate.AddMilliseconds(seconds * 1000);
            double ms = (fraction * 1000.0) / (1UL << 32);
            dt.AddMilliseconds(ms);
            return dt;
        }
        protected void ChangeUIntBE(uint num, int offset)
        {
            BinaryPrimitives.WriteUInt32BigEndian(_bytes.AsSpan(offset), num);
        }
        ~NtpServerPacket()
        {
            Dispose();
        }
        public void Dispose()
        {
            cancelToken.Cancel();
            udpServer.Close();
        }
    }
}
