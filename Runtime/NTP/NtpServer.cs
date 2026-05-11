using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
namespace ToolKits
{

    public class NtpServer : MonoBehaviour
    {
        NtpServerPacket server;
        public uint NtpServerPort = 8123;
        void Start()
        {
            server = new NtpServerPacket((int)NtpServerPort);
        }
         
        private void OnDestroy()
        {
            server.Dispose();
            server = null;
        }
    }
}
