using ChatServer.Net.IO;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Net.Sockets;
using System.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Reflection.Emit;


namespace ChatServer
{
    class Program
    {
        static List<Client> _users;
        static TcpListener _listener;
  
        static void Main(string[] args)
        {
            Console.WriteLine($"{ChatHostInfo.MyIP} up and running");
            // _users is a subset of the total users where user is connected to this server
            _users = new List<Client>();
            int ClientSocketPort = 7890;
            _listener = new TcpListener(IPAddress.Any, ClientSocketPort);
            _listener.Start();
            Console.WriteLine($"Listening on {IPAddress.Any}:{ClientSocketPort}");

            while (true)
            {
                var client = new Client(_listener.AcceptTcpClient());

                if (IsUsernameAlreadyUsedInUsersTable(client))
                {
                    DuplicateUsernameFound(client);
                }
                else
                {
                    AcceptClient(client);
                }
            }
        }

        private static void AcceptClient(Client client)
        {
            client.HasValidConnection = true;
            Console.WriteLine($"[{DateTime.Now}]: {client.UID} aka {client.Username} connected");
            _users.Add(client);

            /* Add user's name and UID plus the server they are connected to DB */
            RecordUserToUsersTable(client);

            /*Broadcast connection to everyone on the server */
            BroadcastConnection(client.Username);

            /*Send all previously sent messages to the new user*/
            ForwardMessagesToUserFromMessagesDB(client);
        }

        private static void DuplicateUsernameFound(Client client)
        {
            string error = $"Username {client.Username} has already been taken";
            Console.WriteLine($"[{DateTime.Now}]: {client.UID.ToString()} has been denied; error: {error}");
            var broadcastPacket = new PacketBuilder();
            broadcastPacket.WriteOpCode(ChatHostInfo.INVALID_USERNAME_OPCODE);
            broadcastPacket.WriteMessage(error);
            client.ClientSocket.Client.Send(broadcastPacket.GetPacketBytes());
            client.Disconnect();
        }

        static void BroadcastConnection(string Username)
        {
            
            BroadcastMessage(DateTime.Now, $"{Username} joined!", 5);

            foreach (var establishedUser in _users)
            {
                foreach (var newUser in _users)
                {
                    var broadcastPacket = new PacketBuilder();
                    broadcastPacket.WriteOpCode(ChatHostInfo.USER_CONNECTION_OPCODE);
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
                RecordMessageToMessagesTable(DateTime.Now, msg, ChatHostInfo.MESSAGE_OPCODE, Username);
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
            BroadcastMessage(DateTime.Now, uid, ChatHostInfo.USER_DISCONNECTION_OPCODE);
            BroadcastMessage(DateTime.Now, $"{disconnectedUser.Username} Disconnected!", ChatHostInfo.MESSAGE_OPCODE);
        }

        static void RecordMessageToMessagesTable(DateTime TimeStamp, string msg, int opCode, string Author = null)
        {
            SqlConnection connection = new SqlConnection(ChatHostInfo.APPLICATION_DB_DATA_STRING);
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
            connection.Close();
        }
        static void ForwardMessagesToUserFromMessagesDB(Client user)
        {
            SqlConnection connection = new SqlConnection(ChatHostInfo.APPLICATION_DB_DATA_STRING);
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
            connection.Close();

        }

        static void RecordUserToUsersTable(Client user)
        {
            SqlConnection connection = new SqlConnection(ChatHostInfo.APPLICATION_DB_DATA_STRING);
            connection.Open();
            string? commandString = "insert into Users (username,serverip,uid) values"
                + "(@username,@serverip,@uid)";
            SqlCommand command = new SqlCommand(commandString, connection);
            command.Parameters.AddWithValue("@username", user.Username);
            command.Parameters.AddWithValue("@serverip", ChatHostInfo.MyIP);
            command.Parameters.AddWithValue("@uid", user.UID.ToString());
            command.ExecuteNonQuery();
            connection.Close();
        }

        static string RetrieveServerOfUserFromUsersTable(Client user)
        {
            SqlConnection connection = new SqlConnection(ChatHostInfo.APPLICATION_DB_DATA_STRING);
            connection.Open();
            SqlCommand command = new SqlCommand($"select * from users where uid={user.UID.ToString()}", connection);
            var reader = command.ExecuteReader();
            string serverIP = "";
            while (reader.Read())
            {
                serverIP = (string)reader[1];
            }
            reader.Close();
            connection.Close();

            return serverIP;
        }

        static bool IsUsernameAlreadyUsedInUsersTable(Client user)
        {
            SqlConnection connection = new SqlConnection(ChatHostInfo.APPLICATION_DB_DATA_STRING);
            connection.Open();
            SqlCommand command = new SqlCommand($"select * from users where username={user.Username}", connection);
            var reader = command.ExecuteReader();
            bool IsDuplicateUsername = false;
            while (reader.Read())
            {
                IsDuplicateUsername = true;
            }
            reader.Close();
            connection.Close();

            return IsDuplicateUsername;
        }

        // make new method that goes through each user in db and checks if user is in _users and if not enacts protocal to send data to other server

    }


}