using ChatClient.Net.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatClient.Net
{
    class Server
    {
        TcpClient _client;
        public PacketReader PacketReader;

        public event Action connectedEvent;
        public event Action msgReceivedEvent;
        public event Action userDisconnectEvent;
        public event Action invalidUsernameEvent;
        public event Action usernameAlreadyTakenEvent;


        public Server()
        {
            _client = new TcpClient();
        }

        public void ConnectToServer(string username)
        {
            if (!string.IsNullOrWhiteSpace(username))
            {
                if (!_client.Connected)
                {
                    //_client.Connect("3.133.113.67", 7890);
                    //_client.Connect("18.217.171.195", 7890);
                    //_client.Connect("127.0.0.1", 7890);
                    _client.Connect("ChatServerLB-9f7dfd96952273cc.elb.us-east-2.amazonaws.com", 7890);
                    //_client.Connect("18.218.59.112", 7890);

                    PacketReader = new PacketReader(_client.GetStream());


                    var connectPacket = new PacketBuilder();
                    connectPacket.WriteOpCode(0);
                    connectPacket.WriteMessage(username);
                    _client.Client.Send(connectPacket.GetPacketBytes());
                    ReadPackets();
                } 
            }
            else
            {
                invalidUsernameEvent?.Invoke();
                Console.WriteLine("Invalid Username");
            }
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
                        case 1:
                            connectedEvent?.Invoke();
                            break;
                        case 5:
                            msgReceivedEvent?.Invoke(); 
                            break;
                        case 10:
                            userDisconnectEvent?.Invoke();
                            break;
                        case 25:
                            usernameAlreadyTakenEvent?.Invoke();
                            break;
                        default:
                            Console.WriteLine("OpCode not found");
                            break;
                    }
                }
            });
        }

        public void SendMessageToServer(string message)
        {
            var messagePacket = new PacketBuilder();
            messagePacket.WriteOpCode(5);
            // Add a bunch of other write messages to be able to send more information about the messaage like images author files etc
            messagePacket.WriteMessage(message);
            _client.Client.Send(messagePacket.GetPacketBytes());
        }

    }
}
