using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using XB.Net.IO;

namespace XB
{
    class XA
    {
        TcpClient _client;
        public PacketReader PacketReader;
        string hostname = "127.0.0.1";

        public XA()
        {
            _client = new TcpClient();
        }

        public void ConnectToServer()
        {

            _client.Connect(hostname, 9999);
            Console.WriteLine($"Connected to {hostname}:9999");
            PacketReader = new PacketReader(_client.GetStream());

            var connectPacket = new PacketBuilder();
            connectPacket.WriteOpCode(50);
            connectPacket.WriteMessage(ChatHostInfo.MyIP);
            _client.Client.Send(connectPacket.GetPacketBytes());
            ReadPackets();
        }

        private void ReadPackets()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    var opCode = PacketReader.ReadByte();
                    switch (opCode)
                    {
                        case 55:
                            Console.WriteLine($"Message from {hostname}");
                            break;
                        default:
                            Console.WriteLine("OpCode not found");
                            break;
                    }
                }
            });
        }
    }
}
