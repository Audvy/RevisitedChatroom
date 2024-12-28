using ChatServer.Net.IO;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Net.Sockets;
using System.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Reflection.Emit;
using System.Collections;
using StackExchange.Redis;
using System.Reflection.Metadata;
using ChatServer.Models;


namespace ChatServer
{
    class Program
    {
        static List<Client> _users;
        static List<ServerSender> _servers;
        //static MonitoringService _monitoringService;

        static TcpListener _clientportlistener;
        static TcpListener _serverportlistener;

        static TcpListener _awshealthcheckportlistener;

        public PacketReader ServerSidePacketReader;

        static void Main(string[] args)
        {

            //_monitoringService = new MonitoringService(ChatHostInfo.MONITORINGSERVICE_IP);
            //_monitoringService.ConnectToServer();
            Console.WriteLine($"{ChatHostInfo.MyIP} up and running");
            // _users is a subset of the total users where ∀u, u ∈ _users is connected to this server instance
            _users = new List<Client>();
            _servers = new List<ServerSender>();

            _clientportlistener = new TcpListener(IPAddress.Any, ChatHostInfo.CLIENT_SOCKET_PORT);
            _clientportlistener.Start();


            _serverportlistener = new TcpListener(IPAddress.Any, ChatHostInfo.INTERSERVER_COMMS_PORT);
            _serverportlistener.Start();

            _awshealthcheckportlistener = new TcpListener(IPAddress.Any, ChatHostInfo.AWS_HEALTHCHECKS_PORT);
            _awshealthcheckportlistener.Start();

            Console.WriteLine($"[Client comms]\t\tListening on {IPAddress.Any}:{ChatHostInfo.CLIENT_SOCKET_PORT}");
            Console.WriteLine($"[Interserver comms]\tListening on {IPAddress.Any}:{ChatHostInfo.INTERSERVER_COMMS_PORT}");
            Console.WriteLine($"[AWS Healthchecks]\tListening on {IPAddress.Any}:{ChatHostInfo.AWS_HEALTHCHECKS_PORT}");

            Task.Run(() => ListenForHealthChecks());
            Task.Run(() => ListenForClients());
            while (true)
            {
                var server = new ServerSender(_serverportlistener.AcceptTcpClient());
                _servers.Add(server);
            }

        }

        private static void ListenForHealthChecks()
        {
            while (true)
            {
                var healthCheck = new HealthCheck(_awshealthcheckportlistener.AcceptTcpClient());
            }
        }

        private static void ListenForClients()
        {
            while (true)
            {
                var client = new Client(_clientportlistener.AcceptTcpClient());

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

            /* Add user's name and UID plus the server they are connected to DB */
            RecordUserToUsersTable(client);

            /*Broadcast connection to everyone on the server */
            BroadcastConnection(client);

            /*Send all previously sent messages to the new user*/
            ForwardMessagesToUserFromMessagesDB(client);

            /*Send all current connected users to the new user*/
            ForwardUsersToNewUser(client);

            _users.Add(client);
        }

        private static void DuplicateUsernameFound(Client client)
        {
            string error = $"Username {client.Username} has already been taken";
            Console.WriteLine($"[{DateTime.Now}]: {client.UID.ToString()} has been denied; error: {error}");
            var broadcastPacket = new PacketBuilder();
            broadcastPacket.WriteOpCode(ChatHostInfo.INVALID_USERNAME_OPCODE);
            broadcastPacket.WriteMessage(error);
            client.ClientSocket.Client.Send(broadcastPacket.GetPacketBytes());
            client.ClientSocket.Close();
        }

        static void BroadcastConnection(Client user)
        {
            BroadcastInterserverMessage(DateTime.Now, $"{user.Username} joined!", ChatHostInfo.MESSAGE_OPCODE);

            ArrayList ServerIPList = RetrieveAllServersFromServersDB();

            foreach (string IP in ServerIPList)
            {
                if (IP.Equals(ChatHostInfo.MyIP))
                {
                    BroadcastConnection(user.Username, user.UID.ToString());
                }
                else
                {
                    var ServerConnection = _servers.Where(x => x.ip == IP).FirstOrDefault();
                    if (ServerConnection == null)
                    {
                        ServerConnection = new ServerSender(IP);
                        ServerConnection.ConnectToServer();

                    }
                    ServerConnection.SendMessageToServer(user.Username, user.UID.ToString(), ChatHostInfo.INTERSERVER_CONNECTION_OPCODE);
                }
            }
        }

        public static void BroadcastConnection(string Username, string uid)
        {
            foreach (var establishedUser in _users)
            {
                var broadcastPacket = new PacketBuilder();
                broadcastPacket.WriteOpCode(ChatHostInfo.USER_CONNECTION_OPCODE);
                broadcastPacket.WriteMessage(Username);
                broadcastPacket.WriteMessage(uid);
                establishedUser.ClientSocket.Client.Send(broadcastPacket.GetPacketBytes());
            }
        }

        public static void BroadcastIntraserverMessage(string Message, byte opCode)
        {
            foreach (var user in _users)
            {
                var msgPacket = new PacketBuilder();
                msgPacket.WriteOpCode(opCode);
                msgPacket.WriteMessage(Message);
                user.ClientSocket.Client.Send(msgPacket.GetPacketBytes());
            } 
        }

        public static void BroadcastInterserverMessage(DateTime TimeStamp, string Message, byte opCode, string Username = null)
        {
            string msg = Message;

            if (opCode == 5)
            {
                msg = Username.IsNullOrEmpty() ? $"[{TimeStamp}] {Message}" : $"[{TimeStamp}] {Username}: {Message}";
                RecordMessageToMessagesTable(DateTime.Now, msg, ChatHostInfo.MESSAGE_OPCODE, Username);
            }

            ArrayList ServerIPList = RetrieveAllServersFromServersDB();

            foreach (string IP in ServerIPList)
            {
                if (IP.Equals(ChatHostInfo.MyIP))
                {
                    BroadcastIntraserverMessage(msg, opCode);
                }
                else
                {
                    var ServerConnection = _servers.Where(x => x.ip == IP).FirstOrDefault();
                    if (ServerConnection == null)
                    {
                        ServerConnection = new ServerSender(IP);
                        ServerConnection.ConnectToServer();
                        
                    }
                    ServerConnection.SendMessageToServer(msg, ChatHostInfo.GetInterserverOpCodeAlternative(opCode));
                }
            }

        }

        public static void BroadcastDisconnection(string uid)
        {
            var disconnectedUser = _users.Where(x => x.UID.ToString() == uid).FirstOrDefault();
            if (disconnectedUser == null) { return; }

            _users.Remove(disconnectedUser);
            RemoveDisconnectedUserFromUsersTable(disconnectedUser);
            BroadcastInterserverMessage(DateTime.Now, uid, ChatHostInfo.USER_DISCONNECTION_OPCODE);
            BroadcastInterserverMessage(DateTime.Now, $"{disconnectedUser.Username} Disconnected!", ChatHostInfo.MESSAGE_OPCODE);
        }

        public static void RecieveInterserverDisconnection(string uid)
        {
            BroadcastIntraserverMessage(uid, ChatHostInfo.USER_DISCONNECTION_OPCODE);

        }


        // Messages Tables
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

        // Users Table
        static void RecordUserToUsersTable(Client user)
        {
            SqlConnection connection = new SqlConnection(ChatHostInfo.APPLICATION_DB_DATA_STRING);
            connection.Open();
            string? commandString = "insert into Users (username,uid) values"
                + "(@username,@uid)";
            SqlCommand command = new SqlCommand(commandString, connection);
            command.Parameters.AddWithValue("@username", user.Username);
            command.Parameters.AddWithValue("@uid", user.UID.ToString());
            command.ExecuteNonQuery();
            connection.Close();
        }

        static bool IsUsernameAlreadyUsedInUsersTable(Client user)
        {
            SqlConnection connection = new SqlConnection(ChatHostInfo.APPLICATION_DB_DATA_STRING);
            connection.Open();
            SqlCommand command = new SqlCommand($"SELECT * from Users WHERE username='{user.Username}'", connection);
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

        static void ForwardUsersToNewUser(Client user)
        {
            SqlConnection connection = new SqlConnection(ChatHostInfo.APPLICATION_DB_DATA_STRING);
            connection.Open();
            SqlCommand command = new SqlCommand("select * from Users", connection);
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var msgPacket = new PacketBuilder();
                msgPacket.WriteOpCode(ChatHostInfo.USER_CONNECTION_OPCODE);
                msgPacket.WriteMessage((string)reader[0]);
                msgPacket.WriteMessage((string)reader[1]);
                user.ClientSocket.Client.Send(msgPacket.GetPacketBytes());
            }
            reader.Close();
            connection.Close();
        }

        static void RemoveDisconnectedUserFromUsersTable(Client user)
        {
            SqlConnection connection = new SqlConnection(ChatHostInfo.APPLICATION_DB_DATA_STRING);
            connection.Open();
            string? commandString = $"delete from Users where username='{user.Username}'";
            SqlCommand command = new SqlCommand(commandString, connection);
            command.ExecuteNonQuery();
            connection.Close();
        }

        // Servers Table
        static ArrayList RetrieveAllServersFromServersDB()
        {
            ArrayList servers = new ArrayList();
            SqlConnection connection = new SqlConnection(ChatHostInfo.APPLICATION_DB_DATA_STRING);
            connection.Open();
            SqlCommand command = new SqlCommand($"select * from Servers", connection);
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                servers.Add((string)reader[0]);
            }
            reader.Close();
            connection.Close();

            return servers;
        }

        static void RecordIPToServersDB()
        {
            ArrayList ServerIPList = RetrieveAllServersFromServersDB();

            foreach (string IP in ServerIPList)
            {
                if (IP.Equals(ChatHostInfo.MyIP))
                {
                    return;
                }
            }

            SqlConnection connection = new SqlConnection(ChatHostInfo.APPLICATION_DB_DATA_STRING);
            connection.Open();
            string? commandString = "insert into Servers (server) values"
                + "(@server)";
            SqlCommand command = new SqlCommand(commandString, connection);
            command.Parameters.AddWithValue("@server", ChatHostInfo.MyIP);
            command.ExecuteNonQuery();
            connection.Close();
        }


    }


}