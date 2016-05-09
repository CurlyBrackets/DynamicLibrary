using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Dynamic
{
    public class RemoteExecutor
    {
        private const int BroadcastDelay = 1000;
        private const int NumBroadcast = 3;

        private UdpClient m_discover;
        private HashSet<IPEndPoint> m_endpoints;
        private List<TcpClient> m_clients;

        private static Random rand = new Random();

        public RemoteExecutor()
        {

        }

        public void Discover()
        {
            m_endpoints = new HashSet<IPEndPoint>();
            //m_discover = new UdpClient(new IPEndPoint(IPAddress.Broadcast, RemoteRunner.DiscoverPort));
            m_discover = new UdpClient();
            m_discover.EnableBroadcast = true;
            var endpoint = new IPEndPoint(IPAddress.Broadcast, RemoteRunner.DiscoverPort);

            for (int i = 0; i < NumBroadcast; i++)
            {
                m_discover.Send(RemoteRunner.DiscoverPackage, RemoteRunner.DiscoverPackage.Length, endpoint);
                m_discover.BeginReceive(DiscoverCallback, null);
                Thread.Sleep(BroadcastDelay);
            }

            m_discover.Close();

            foreach (var addr in m_endpoints)
            {
                Console.WriteLine(addr);
                var cl = new TcpClient();
                cl.BeginConnect(addr.Address, RemoteRunner.ConnectionPort, ConnectCallback, cl);
            }
        }

        private void DiscoverCallback(IAsyncResult ar)
        {
            IPEndPoint remote = null;
            try
            {
                var package = m_discover.EndReceive(ar, ref remote);

                bool good = package.Length == RemoteRunner.DiscoverPackage.Length;
                if (good)
                {
                    for (int i = 0; i < package.Length && good; i++)
                    {
                        if (package[i] != RemoteRunner.DiscoverPackage[i])
                            good = false;
                    }

                    m_endpoints.Add(remote);
                }

                m_discover.BeginReceive(DiscoverCallback, null);
            }
            catch (ObjectDisposedException)
            {
                // socket closed
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            var cl = (TcpClient)ar.AsyncState;
            cl.EndConnect(ar);

            if (cl.Connected)
                m_clients.Add(cl);
        }

        public void BeginCall(object o, string method, List<object[]> param, AsyncCallback callback, object state)
        {
            

            var cl = m_clients[rand.Next(m_clients.Count)];
            var ms = new MemoryStream();

            
        }
    }
}
