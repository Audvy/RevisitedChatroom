using ChatServer.Net.IO;
using Microsoft.Data.SqlClient;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer
{
    class ServerSender
    {
        public string ip;
        static TcpClient _server;
        public PacketReader _packetReader;

        public ServerSender(string server)
        {
            this.ip = server;
            _server = new TcpClient();
        }

        public ServerSender(TcpClient server)
        {
            _server = server;
            _packetReader = new PacketReader(_server.GetStream());
            var opCode = _packetReader.Read();
            if (opCode == ChatHostInfo.INTERSERVER_CONNECTION_OPCODE)
            {
                ip = _packetReader.ReadMessage();
                Console.WriteLine($"[{DateTime.Now}]: Connection established by server {ip}");
                ReadPackets();
            }
        }

        public void ConnectToServer()
        {
            if (!_server.Connected)
            {
                _server.Connect(ip, ChatHostInfo.INTERSERVER_COMMS_PORT);
                _packetReader = new PacketReader(_server.GetStream());
                Console.WriteLine($"[{DateTime.Now}]: Connection to server {ip} was established");


                var connectPacket = new PacketBuilder();
                connectPacket.WriteOpCode(ChatHostInfo.INTERSERVER_CONNECTION_OPCODE);
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
                            case ChatHostInfo.INTERSERVER_CONNECTION_OPCODE:
                                var username = _packetReader.ReadMessage();
                                var connection_uid = _packetReader.ReadMessage();
                                Program.BroadcastConnection(username, connection_uid);
                                break;
                            case ChatHostInfo.INTERSERVER_MESSAGE_OPCODE:
                                var msg = _packetReader.ReadMessage();
                                Program.BroadcastIntraserverMessage(msg, ChatHostInfo.MESSAGE_OPCODE);
                                break;
                            case ChatHostInfo.INTERSERVER_USER_DISCONNECTION_OPCODE:
                                var disconnection_uid = _packetReader.ReadMessage();
                                Program.RecieveInterserverDisconnection(disconnection_uid);
                                break;
                            default:
                                Console.WriteLine("OpCode not found");
                                break;
                        }
                    }
                    catch (System.IO.IOException)
                    {
                        Console.WriteLine($"Server {ip} is not online");
                        RemoveUnresponsiveServerFromServersDB();
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

        public void SendMessageToServer(string Message, string Message2, byte opCode)
        {
            var messagePacket = new PacketBuilder();
            messagePacket.WriteOpCode(opCode);
            messagePacket.WriteMessage(Message);
            messagePacket.WriteMessage(Message2);
            _server.Client.Send(messagePacket.GetPacketBytes());
        }

        public void RemoveUnresponsiveServerFromServersDB()
        {
            SqlConnection connection = new SqlConnection(ChatHostInfo.APPLICATION_DB_DATA_STRING);
            connection.Open();
            string? commandString = $"delete from Servers where server='{ip}'";
            SqlCommand command = new SqlCommand(commandString, connection);
            command.ExecuteNonQuery();
            connection.Close();
        }
    }
}
