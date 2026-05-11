using System;
using UnityEngine;
namespace ToolKits
{

    public class NtpClient : MonoBehaviour
    {
        public string NtpServerAddress = "127.0.0.1";
        public uint NtpServerPort = 8123;
        NtpClientPacket ntpClient;

        void Start()
        {
            ntpClient = new NtpClientPacket(NtpServerAddress, NtpServerPort);
        }
        public DateTime Now => ntpClient.Now;
        private void OnDestroy()
        {
            ntpClient.Dispose();
        }
    }
}
