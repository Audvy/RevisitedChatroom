using System.Net;
using System.Net.Sockets;

namespace XB
{
    class Program
    {
        static void Main(string[] args)
        {
            XA _server = new XA();
            _server.ConnectToServer();
            while (true)
            {
            }
        }
    }
}