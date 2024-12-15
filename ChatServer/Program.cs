using ChatServer.Models;
using ChatServer.Net.IO;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Threading;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.Data;

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
                Console.WriteLine($"{client.UID} connected on port 7890!");
                _users.Add(client);
                
                /*Broadcast connection to everyone on the server */
                BroadcastConnection(client.Username);
                /*Send all previously sent messages to the new user*/
                ReadMessagesFromDatabase(client);
            }
        }
        

        static void BroadcastConnection(string Username)
        {
            RecordMessagesToDatabase(DateTime.Now, $"[{DateTime.Now}] {Username} joined!", 5);

            foreach (var establishedUser in _users)
            {
                foreach (var newUser in _users)
                {
                    var broadcastPacket = new PacketBuilder();
                    broadcastPacket.WriteOpCode(1);
                    broadcastPacket.WriteMessage(newUser.Username);
                    broadcastPacket.WriteMessage(newUser.UID.ToString());
                    establishedUser.ClientSocket.Client.Send(broadcastPacket.GetPacketBytes());
                }
            }
        }

        public static void BroadcastMessage(DateTime TimeStamp, string Message, byte opCode, string Username = null)
        {
            string? msg = Username.IsNullOrEmpty() ? $"[{TimeStamp}] {Message}" : $"[{TimeStamp}] {Username}: {Message}";
            RecordMessagesToDatabase(DateTime.Now, msg, 5, Username);


            foreach (var user in _users)
            {
                var msgPacket = new PacketBuilder();
                msgPacket.WriteOpCode(opCode);
                msgPacket.WriteMessage(msg);
                user.ClientSocket.Client.Send(msgPacket.GetPacketBytes());
            }
        }

        public static void BroadcastDisconnection(string uid)
        {
            var disconnectedUser = _users.Where(x => x.UID.ToString() == uid).FirstOrDefault();
            if (disconnectedUser == null) { return; }

            _users.Remove(disconnectedUser);
            foreach (var user in _users)
            {
                var broadcastPacket = new PacketBuilder();
                broadcastPacket.WriteOpCode(10);
                broadcastPacket.WriteMessage(uid);
                user.ClientSocket.Client.Send(broadcastPacket.GetPacketBytes());
                
            }
            BroadcastMessage(DateTime.Now, $"{disconnectedUser.Username} Disconnected!", 5);
        }

        static void RecordMessagesToDatabase(DateTime TimeStamp, string msg, int opCode, string Author = null)
        {
            SqlConnection connection = new SqlConnection("Data Source = (localdb)\\Local; Initial Catalog = master; Integrated Security = True");
            connection.Open();
            string? commandString = Author.IsNullOrEmpty()
                    ? "insert into Messages (TimeStamp,msg,opCode) values" + "(@TimeStamp,@msg,@opCode)"
                    : "insert into Messages (TimeStamp,Author,msg,opCode) values" + "(@TimeStamp,@Author,@msg,@opCode)";
            SqlCommand command = new SqlCommand(commandString, connection);
            if (!Author.IsNullOrEmpty())
            {
                command.Parameters.AddWithValue("@Author", Author);
            }
            command.Parameters.AddWithValue("@TimeStamp", TimeStamp);
            command.Parameters.AddWithValue("@msg", msg);
            command.Parameters.AddWithValue("@opCode", opCode);
            command.ExecuteNonQuery();
        }
        static void ReadMessagesFromDatabase(Client user)
        {
            SqlConnection connection = new SqlConnection("Data Source = (localdb)\\Local; Initial Catalog = master; Integrated Security = True");
            connection.Open();
            SqlCommand command = new SqlCommand("select * from Messages order by TimeStamp asc", connection);
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var msgPacket = new PacketBuilder();
                msgPacket.WriteOpCode(5);
                msgPacket.WriteMessage((string)reader[2]);
                user.ClientSocket.Client.Send(msgPacket.GetPacketBytes());
            }
            reader.Close();

        }

    }


}