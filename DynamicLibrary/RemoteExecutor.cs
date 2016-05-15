using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private Provider m_provider;

        private static Random rand = new Random();

        public RemoteExecutor()
        {
            m_provider = new Provider();
            Extensions.Go(() => m_provider.Listen(RemoteRunner.CodePort) );
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
            m_clients = new List<TcpClient>();

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

        public IAsyncResult BeginCall(object o, string method, List<object[]> param, AsyncCallback callback, object state)
        {
            //rand.Next(m_clients.Count-1)
            var cl = m_clients[0];
            return new AsyncCall(o, method, param, callback, state, cl);
        }

        public List<object> EndCall(IAsyncResult ar)
        {
            return ((AsyncCall)ar).Results;
        }

        private class AsyncCall : IAsyncResult
        {
            private object m_state;
            public object AsyncState
            {
                get
                {
                    return m_state;
                }
            }

            public WaitHandle AsyncWaitHandle
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public bool CompletedSynchronously
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            private bool m_completed;
            public bool IsCompleted
            {
                get
                {
                    return m_completed;
                }
            }

            private AsyncCallback m_cb;
            private NetworkStream m_ns;
            private ObjectSerializer m_input;
            private ObjectDeserializer m_output;

            private object m_obj;
            private List<object[]> m_param;
            private int m_left;

            public List<object> Results { get; private set; }

            public AsyncCall(object o, string method, List<object[]> param, AsyncCallback callback, object state, TcpClient client)
            {
                Debug.Print("+ AsyncCall..ctor");

                m_state = state;
                m_completed = false;
                m_cb = callback;
                m_param = param;
                m_left = param.Count;
                m_obj = o;

                m_ns = client.GetStream();

                var tosend = new RunMessage()
                {
                    IsStatic = false,
                    MethodName = method,
                    NumArguments = param[0].Length,
                    NumCalls = param.Count,
                    TypeNames = param[0].Select(z => z.GetType().FullName).ToArray()
                };

                m_output = new ObjectDeserializer(m_ns);
                m_input = new ObjectSerializer(m_ns);
                m_input.BeginWrite(tosend, SendObject, null);
            }

            private void SendObject(IAsyncResult ar)
            {
                Debug.Print("+ AsyncCall.SendObject");

                m_input.EndWrite(ar);
                m_input.BeginWrite(m_obj, SendParam, null);
            }

            private void SendParam(IAsyncResult ar)
            {
                Debug.Print("+ AsyncCall.SendParam - " + m_left);

                m_input.EndWrite(ar);

                if (m_left > 0)
                    m_input.BeginWrite(m_param[m_param.Count - m_left--], SendParam, null);
                else
                    m_output.BeginRead(StartReceive, null);
            }

            private void StartReceive(IAsyncResult ar)
            {
                Debug.Print("+ AsyncCall.StartReceive");

                var msg = (RunResultMessage)m_output.EndRead(ar);
                if (!msg.Success)
                {
                    Results = null;
                    Complete();
                }
                else
                {
                    Results = new List<object>();
                    m_left = msg.NumResults;
                    m_output.BeginRead(ReceiveResult, null);
                }
            }

            private void ReceiveResult(IAsyncResult ar)
            {
                Debug.Print("+ AsyncCall.ReceiveResult - " + m_left);

                Results.Add(m_output.EndRead(ar));
                if (--m_left > 0)
                    m_output.BeginRead(ReceiveResult, null);
                else
                    Complete();
            }

            private void Complete()
            {
                Debug.Print("+ AsyncCall.Complete");

                m_completed = true;
                m_cb(this);
            }
        }
    }
}
