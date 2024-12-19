using ChatServer.Net.IO;
using System.Net.Sockets;


namespace ChatServer
{
    class Client
    {
        public string Username { get; set; }
        public Guid UID { get; set; }
        public TcpClient ClientSocket { get; set; }

        PacketReader _packetReader;

        public bool HasValidConnection;

        public Client(TcpClient client)
        {
            HasValidConnection = false;
            ClientSocket = client;
            UID = Guid.NewGuid();
            _packetReader = new PacketReader(ClientSocket.GetStream());

            var opCode = _packetReader.Read();
            if (opCode == 0)
            {
                Username = _packetReader.ReadMessage();
                Console.WriteLine($"[{DateTime.Now}]: {UID.ToString()} has made connection request");
                Task.Run(() => Process());
            }
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
                            Program.BroadcastInterserverMessage(DateTime.Now, msg, 5, Username);
                            break;
                        case 25:
                            Disconnect();
                            break;
                        default:
                            break;

                    }
                }
                catch (System.IO.IOException)
                {
                    Console.WriteLine($"[{DateTime.Now}]: Connection Lost");
                    if (HasValidConnection)
                    {
                        Disconnect();
                    }
                    break;
                }
            }
        }

        public void Disconnect()
        {
            Console.WriteLine($"[{DateTime.Now}]: [{UID}] aka {Username} disconnected!");
            Program.BroadcastDisconnection(UID.ToString());
            ClientSocket.Close();
        }
    }
}
