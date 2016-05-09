using System;
using System.Collections.Generic;
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

        internal static readonly byte[] DiscoverPackage = new byte[]
        {
             26, 144, 1, 23, 200, 57, 130, 213, 54, 85, 204, 89, 175, 57, 207, 66
        };

        private UdpClient m_discoverClient;
        private TcpListener m_listener;

        public RemoteRunner(int taskCount)
        {

        }

        public void Listen()
        {
            m_discoverClient = new UdpClient(DiscoverPort);
            m_discoverClient.BeginReceive(DiscoverCallback, null);

            m_listener = new TcpListener(IPAddress.Any, ConnectionPort);
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
            var client = m_listener.EndAcceptTcpClient(ar);
            var connection = new RemoteRunnerConnection(this, client);
            connection.Start();
        }

        internal void Execute(RunnerContext ctx)
        {
            // generate actions to invoke
            var actions = new Action[ctx.Arguments.Count];
            for (int i = 0; i < actions.Length; i++)
                actions[i] = () => { ctx.Results.Add(ctx.MethodInfo.Invoke(ctx.Object, ctx.Arguments[i])); };
            Parallel.Invoke(actions);
            ctx.Connection.Respond(ctx);
        }
    }
}
