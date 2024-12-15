using ChatServer.Net.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer
{
    class Client
    {
        public string Username { get; set; }
        public Guid UID { get; set; }
        public TcpClient ClientSocket { get; set; }

        PacketReader _packetReader;
        public Client(TcpClient client)
        {
            ClientSocket = client;
            UID = Guid.NewGuid();
            _packetReader = new PacketReader(ClientSocket.GetStream());

            var opCode = _packetReader.Read();
            Username = _packetReader.ReadMessage();


            Console.WriteLine($"[{DateTime.Now}]: {Username} has connected! Packet has Opcode: {opCode}");

            Task.Run(() => Process());
        }

        void Process()
        {
            while (true)
            {
                try
                {
                    var opCode = _packetReader.ReadByte();
                    switch (opCode)
                    {
                        case 5:
                            var msg = _packetReader.ReadMessage();
                            Console.WriteLine($"[{DateTime.Now}]: Message recieved! {Username} said \"{msg}\"");
                            Program.BroadcastMessage(DateTime.Now, msg, 5, Username);
                            break;
                        default:
                            break;

                    }
                }
                catch (Exception)
                {
                    Console.WriteLine($"[{UID}] aka {Username} Disconnected!");
                    Program.BroadcastDisconnection(UID.ToString());
                    ClientSocket.Close();
                    break;
                }
            }
        }
    }
}
