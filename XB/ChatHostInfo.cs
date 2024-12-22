using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace XB
{
    class ChatHostInfo
    {
        // 1 - 14: Intraserver OpCodes
        public const int USER_CONNECTION_OPCODE = 1;
        public const int MESSAGE_OPCODE = 5;
        public const int USER_DISCONNECTION_OPCODE = 10;

        // 15 - 24: Interserver OpCodes
        public const int INTERSERVER_CONNECTION_OPCODE = 11;
        public const int INTERSERVER_MESSAGE_OPCODE = 15;
        public const int INTERSERVER_USER_DISCONNECTION_OPCODE = 20;


        // 25 - 34: Notification OpCodes
        public const int INVALID_USERNAME_OPCODE = 25;

        // 50-55: MonitoringService OpCodes
        public const int MONITORINGSERVICE_CONNECTION_OPCODE = 50;
        public const int MONITORINGSERVICE_MESSAGE_OPCODE = 55;

        // Database Connection strings
        public const string APPLICATION_DB_DATA_STRING = "Data Source=chatdb.cf64cwwg2pao.us-east-2.rds.amazonaws.com,1433;Initial Catalog=application;Persist Security Info=True;User ID=admin;Password=password;Trust Server Certificate=True";

        // Monitoring Service Connection
        public const string MONITORINGSERVICE_IP = "18.118.255.136";

        // Ports
        public const int INTERSERVER_COMMS_PORT = 8765;
        public const int CLIENT_SOCKET_PORT = 7890;
        public const int AWS_HEALTHCHECKS_PORT = 9876;
        public const int MONITORINGSERVICE_PORT = 6789;


        public static string MyIP 
        {
            get { return GetMyIP(); }
            set { return; } 
        }

        private static async Task<IPAddress?> GetExternalIpAddress()
        {
            var externalIpString = (await new HttpClient().GetStringAsync("http://icanhazip.com"))
                .Replace("\\r\\n", "").Replace("\\n", "").Trim();
            if (!IPAddress.TryParse(externalIpString, out var ipAddress)) return null;
            return ipAddress;
        }

        private static string GetMyIP()
        {
            var externalIpTask = GetExternalIpAddress();
            GetExternalIpAddress().Wait();
            var externalIpString = externalIpTask.Result ?? IPAddress.Loopback;

            return externalIpString.ToString();
        }
    }
}
