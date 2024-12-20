using Microsoft.Data.SqlClient;
using MonitoringService.Net.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringService
{
    class Host
    {
        public string ip { get; set; }
        public TcpClient HostSocket { get; set; }

        PacketReader _packetReader;

        public bool HasValidConnection;

        public Host(TcpClient client)
        {
            HasValidConnection = false;
            HostSocket = client;
            _packetReader = new PacketReader(HostSocket.GetStream());

            var opCode = _packetReader.Read();
            if (opCode == 50)
            {
                ip = _packetReader.ReadMessage();
                Console.WriteLine($"[{DateTime.Now}]: Server {ip} is online");
                RecordIPToServersDB();
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
                        case 55:
                            var msg = _packetReader.ReadMessage();
                            Console.WriteLine($"[{DateTime.Now}]: Message recieved! {ip} said \"{msg}\"");
                            break;
                        default:
                            break;

                    }
                }
                catch (System.IO.IOException)
                {
                    Console.WriteLine($"[{DateTime.Now}]: Server {ip} went down");
                    Disconnect();
                    break;
                }
            }
        }

        public void Disconnect()
        {
            Console.WriteLine($"[{DateTime.Now}]: Server {ip} went down");
            
            HostSocket.Close();
        }

        public void RemoveUnresponsiveServerFromServersDB()
        {
            SqlConnection connection = new SqlConnection("Data Source=chatdb.cf64cwwg2pao.us-east-2.rds.amazonaws.com,1433;Initial Catalog=application;Persist Security Info=True;User ID=admin;Password=password;Trust Server Certificate=True");
            connection.Open();
            string? commandString = $"delete from Servers where server='{ip}'";
            SqlCommand command = new SqlCommand(commandString, connection);
            command.ExecuteNonQuery();
            connection.Close();
        }

        public void RecordIPToServersDB()
        {
            SqlConnection connection = new SqlConnection("Data Source=chatdb.cf64cwwg2pao.us-east-2.rds.amazonaws.com,1433;Initial Catalog=application;Persist Security Info=True;User ID=admin;Password=password;Trust Server Certificate=True");
            connection.Open();
            string? commandString = "insert into Servers (server) values"
                + "(@server)";
            SqlCommand command = new SqlCommand(commandString, connection);
            command.Parameters.AddWithValue("@server", ip);
            command.ExecuteNonQuery();
            connection.Close();
        }
    }
}
