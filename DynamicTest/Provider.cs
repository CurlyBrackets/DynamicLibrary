using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DynamicTest
{
    class Provider
    {
        public Provider()
        {

        }

        public void Listen(Stream s)
        {
            
        }

        public void Listen(int port)
        {
            var ln = new TcpListener(IPAddress.Any, port);
            var cl = ln.AcceptTcpClientAsync();
            cl.Wait();
            cl.Result.GetStream()
        }
    }
}
