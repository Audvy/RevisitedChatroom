using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer.Models
{
    public class Message
    {
        public int opCode {  get; set; }
        public string msg { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Author { get; set; }
    }
}
