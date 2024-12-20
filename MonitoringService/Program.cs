

using MonitoringService.Net.IO;
using System.Net;
using System.Net.Sockets;

namespace MonitoringService
{
    class Program
    {
        static List<Host> _hosts;
        static TcpListener _hostlistener;

        public PacketReader _packreader;

        static void Main(string[] args)
        {
            Console.WriteLine($"MonitoringService up and running");
            _hosts = new List<Host>();

            _hostlistener = new TcpListener(IPAddress.Any, 6789);
            _hostlistener.Start();

            while (true)
            {
                var server = new Host(_hostlistener.AcceptTcpClient());
                _hosts.Add(server);
            }

        }
    }
}