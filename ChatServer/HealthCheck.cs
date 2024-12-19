using ChatServer.Net.IO;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer
{
     class HealthCheck
    {
        static TcpClient _healthCheck;
        public PacketReader _packetReader;

        public HealthCheck(TcpClient HealthCheck)
        {
            _healthCheck = HealthCheck;
            _packetReader = new PacketReader(_healthCheck.GetStream());
            var opCode = _packetReader.Read();
        }
    }
}
