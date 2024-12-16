using ChatServer.Net.IO;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Net.Sockets;
using System.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace ChatServer
{
    class Program
    {
        static List<Client> _users;
        static TcpListener _listener;
        
        static void Main(string[] args)
        {
            Console.WriteLine("Server up and running");
            _users = new List<Client>();
            int port = 7890;
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            Console.WriteLine($"Listening on port {port}");

            while (true)
            {
                var client = new Client(_listener.AcceptTcpClient());

                var username = _users.Where(x => x.Username == client.Username).FirstOrDefault();
                if (username != null) 
                {
                    string error = $"Username {client.Username} has already been taken";
                    Console.WriteLine($"[{DateTime.Now}]: {client.UID.ToString()} has been denied; error: {error}");
                    var broadcastPacket = new PacketBuilder();
                    broadcastPacket.WriteOpCode(25);
                    broadcastPacket.WriteMessage(error);
                    client.ClientSocket.Client.Send(broadcastPacket.GetPacketBytes());
                    client.Disconnect();
                }
                else
                {
                    client.accepted = true;
                    Console.WriteLine($"[{DateTime.Now}]: {client.UID} aka {client.Username} connected");
                    _users.Add(client);

                    /*Broadcast connection to everyone on the server */
                    BroadcastConnection(client.Username);
                    /*Send all previously sent messages to the new user*/
                    ReadMessagesFromDatabase(client);
                }
            }
        }
        

        static void BroadcastConnection(string Username)
        {
            BroadcastMessage(DateTime.Now, $"{Username} joined!", 5);

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
            string msg = Message;
            
            if (opCode== 5)
            {
                msg = Username.IsNullOrEmpty() ? $"[{TimeStamp}] {Message}" : $"[{TimeStamp}] {Username}: {Message}";
                RecordMessagesToDatabase(DateTime.Now, msg, 5, Username);
            }

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
            BroadcastMessage(DateTime.Now, uid, 10);
            BroadcastMessage(DateTime.Now, $"{disconnectedUser.Username} Disconnected!", 5);
        }

        static void RecordMessagesToDatabase(DateTime TimeStamp, string msg, int opCode, string Author = null)
        {
            // mssqlserver.cf64cwwg2pao.us-east-2.rds.amazonaws.com
            SqlConnection connection = new SqlConnection("Data Source=chatdb.cf64cwwg2pao.us-east-2.rds.amazonaws.com,1433;Initial Catalog=application;Persist Security Info=True;User ID=admin;Password=password;Trust Server Certificate=True");
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
            SqlConnection connection = new SqlConnection("Data Source=chatdb.cf64cwwg2pao.us-east-2.rds.amazonaws.com,1433;Initial Catalog=application;Persist Security Info=True;User ID=admin;Password=password;Trust Server Certificate=True");
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