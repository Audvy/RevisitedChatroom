using System;
using System.Net;
using System.Net.Sockets;

namespace ChatServer
{
    class Program
    {
        static List<Client> _users;
        static TcpListener _listener;
        static void Main(string[] args)
        {
            _users = new List<Client>();
            _listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 7890);
            _listener.Start();

            while (true)
            {
                var client = new Client(_listener.AcceptTcpClient());
                Console.WriteLine($"{client.UID} has connected on port 7890!");
                _users.Add(client);

                /*Broadcast connection to everyone on the server */
            }
        }
    }
}