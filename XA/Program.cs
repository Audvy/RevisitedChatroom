using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace XA
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("0");
            int consecutiveNonSuccessfulPings = 0;
            while (true)
            {
                Console.WriteLine("0");
                Ping pingSender = new Ping();
                Console.WriteLine("1");
                PingOptions options = new PingOptions();
                Console.WriteLine("2");

                // Use the default Ttl value which is 128,
                // but change the fragmentation behavior.
                options.DontFragment = true;
                Console.WriteLine("3");

                // Create a buffer of 32 bytes of data to be transmitted. 
                string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                Console.WriteLine("4");
                byte[] buffer = Encoding.ASCII.GetBytes(data);
                Console.WriteLine("5");
                int timeout = 120;
                Console.WriteLine("6");
                string ip = "3.144.117.10";
                Console.WriteLine("7");
                PingReply reply = pingSender.Send(ip, timeout, buffer, options);
                Console.WriteLine("8");
                if (reply.Status != IPStatus.Success)
                {
                    Console.WriteLine($"----------------No connection to {ip}!");
                    consecutiveNonSuccessfulPings++;
                }
                else
                {
                    Console.WriteLine("Pinged!");
                    consecutiveNonSuccessfulPings = 0;
                }
                if (consecutiveNonSuccessfulPings > 30)
                {
                    Console.WriteLine($"----------------{ip} is offline----------------");
                    break;
                }
            }

        }
    }
}