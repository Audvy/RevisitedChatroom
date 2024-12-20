using ChatServer.Net.IO;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer
{
    class MonitoringService
    {
        public string ip;
        static TcpClient _server;
        public PacketReader _packetReader;

        public MonitoringService(string server)
        {
            this.ip = server;
            _server = new TcpClient();
        }

        public void ConnectToServer()
        {
            if (!_server.Connected)
            {
                _server.Connect(ip, ChatHostInfo.MONITORINGSERVICE_PORT);
                _packetReader = new PacketReader(_server.GetStream());
                Console.WriteLine($"[{DateTime.Now}]: Checked into MonitoringService {ip}");


                var connectPacket = new PacketBuilder();
                connectPacket.WriteOpCode(ChatHostInfo.MONITORINGSERVICE_CONNECTION_OPCODE);
                connectPacket.WriteMessage(ChatHostInfo.MyIP);
                _server.Client.Send(connectPacket.GetPacketBytes());
                ReadPackets();
            }
        }

        private void ReadPackets()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        var opCode = _packetReader.ReadByte();
                        switch (opCode)
                        {
                            case ChatHostInfo.MONITORINGSERVICE_MESSAGE_OPCODE:
                                var username = _packetReader.ReadMessage();
                                var connection_uid = _packetReader.ReadMessage();
                                Program.BroadcastConnection(username, connection_uid);
                                break;
                            default:
                                Console.WriteLine("OpCode not found");
                                break;
                        }
                    }
                    catch (System.IO.IOException)
                    {
                        Console.WriteLine($"MonitoringService went down!");
                        _server.Close();
                        break;
                    }
                }
            });
        }

        public void SendMessageToServer(string Message, byte opCode)
        {
            var messagePacket = new PacketBuilder();
            messagePacket.WriteOpCode(opCode);
            messagePacket.WriteMessage(Message);
            _server.Client.Send(messagePacket.GetPacketBytes());
        }

    }
}
