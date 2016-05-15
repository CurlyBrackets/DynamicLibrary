using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Dynamic
{
    public class RemoteRunner
    {
        internal const int DiscoverPort = 30375;
        internal const int ConnectionPort = 30376;
        internal const int CodePort = 30377;

        internal static readonly byte[] DiscoverPackage = new byte[]
        {
             26, 144, 1, 23, 200, 57, 130, 213, 54, 85, 204, 89, 175, 57, 207, 66
        };

        private UdpClient m_discoverClient;
        private TcpListener m_listener;

        private TaskScheduler m_scheduler;
        private TaskFactory m_factory;

        public RemoteRunner(int taskCount)
        {
            m_scheduler = new LimitedConcurrencyLevelTaskScheduler(taskCount);
            m_factory = new TaskFactory(m_scheduler);
        }

        public void Listen()
        {
            m_discoverClient = new UdpClient(DiscoverPort);
            m_discoverClient.BeginReceive(DiscoverCallback, null);

            m_listener = new TcpListener(IPAddress.Any, ConnectionPort);
            m_listener.Start(10);
            m_listener.BeginAcceptTcpClient(AcceptCallback, null);
        }

        private void DiscoverCallback(IAsyncResult ar)
        {
            IPEndPoint remote = null;
            var package = m_discoverClient.EndReceive(ar, ref remote);

            bool good = package.Length == DiscoverPackage.Length;
            if (good)
            {
                for(int i = 0; i < package.Length && good; i++)
                {
                    if (package[i] != DiscoverPackage[i])
                        good = false;
                }

                m_discoverClient.Send(DiscoverPackage, DiscoverPackage.Length, remote);
            }

            m_discoverClient.BeginReceive(DiscoverCallback, null);
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            Debug.Write("+ RemoteRunner.AcceptCallback");

            var client = m_listener.EndAcceptTcpClient(ar);
            var connection = new RemoteRunnerConnection(this, client);
            connection.Start();

            m_listener.BeginAcceptTcpClient(AcceptCallback, null);
            Debug.Write("- RemoteRunner.AcceptCallback");
        }

        internal void Enqueue(RunnerContext ctx, object[] args)
        {
            int index = ctx.CallIndex;
            m_factory.StartNew(() =>
            {
                ctx.PutResult(ctx.MethodInfo.Invoke(ctx.Object, args), index);
                if (ctx.CallsLeft <= 0)
                    ctx.Connection.Respond(ctx);
            });
        }
    }
}
