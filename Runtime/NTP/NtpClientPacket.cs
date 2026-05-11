using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ToolKits
{

    public class NtpClientPacket : IDisposable
    {
        private DateTime ntpDate;
        private long responseReceivedTime;
        private byte[] receivedNtpData;

        private UdpClient udpClient;
        private CancellationTokenSource cancelToken = new CancellationTokenSource();
        private Task taskSend;
        private Task taskReceive;
        public bool DateSynchronized { get; private set; }

        public DateTime Now
        {
            get
            {
                if (DateSynchronized)
                {
                    return ntpDate.AddSeconds(TimeSpan.FromTicks(DateTime.Now.Ticks - responseReceivedTime).TotalSeconds);
                }

                return DateTime.Now;
            }
        }

        public NtpClientPacket(string addresses, uint port)
        {
            udpClient = new UdpClient(addresses, (int)port);
            
            uint IOC_IN = 0x80000000;
            uint IOC_VENDOR = 0x18000000;
            uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
            udpClient.Client.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);

            taskReceive = new Task(RunNtpClientReceive, cancelToken.Token, TaskCreationOptions.LongRunning);
            taskReceive.Start();

            taskSend = new Task(RunNtpClientSend, cancelToken.Token, TaskCreationOptions.LongRunning);
            taskSend.Start();

        }

        async void RunNtpClientSend()
        {
            var ntpData = new byte[48];
            ntpData[0] = 0x1B;

            while (!cancelToken.IsCancellationRequested)
            {
                await udpClient.SendAsync(ntpData, ntpData.Length);
                Thread.Sleep(3000);
            }
        }

        async void RunNtpClientReceive()
        {

            while (!cancelToken.IsCancellationRequested)
            {
                try
                {
                    var receive = await udpClient.ReceiveAsync();
                    receivedNtpData = receive.Buffer;
                    break;
                }
                catch (SocketException e)
                {

                    Debug.LogError(e);
                }

            }

            cancelToken.Cancel();
            udpClient?.Close();

            responseReceivedTime = DateTime.Now.Ticks;

            var intPart = ((ulong)receivedNtpData[40] << 24) | ((ulong)receivedNtpData[41] << 16) | ((ulong)receivedNtpData[42] << 8) | receivedNtpData[43];
            var fractPart = ((ulong)receivedNtpData[44] << 24) | ((ulong)receivedNtpData[45] << 16) | ((ulong)receivedNtpData[46] << 8) | receivedNtpData[47];

            var milliseconds = intPart * 1000 + fractPart * 1000 / 0x100000000L;
            ntpDate = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds((long)milliseconds).ToLocalTime();

            DateSynchronized = true;

            Debug.Log("Date is synchronized : " + Now);
        }

        public void Dispose()
        {
            cancelToken.Cancel();
            udpClient?.Close();
        }
    }

}
