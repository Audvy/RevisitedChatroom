using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer
{
    class ChatHostInfo
    {
        public const int USER_CONNECTION_OPCODE = 1;
        public const int MESSAGE_OPCODE = 5;
        public const int USER_DISCONNECTION_OPCODE = 10;
        public const int INVALID_USERNAME_OPCODE = 25;

        public const string APPLICATION_DB_DATA_STRING = "Data Source=chatdb.cf64cwwg2pao.us-east-2.rds.amazonaws.com,1433;Initial Catalog=application;Persist Security Info=True;User ID=admin;Password=password;Trust Server Certificate=True";

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
