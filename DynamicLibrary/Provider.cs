using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dynamic
{
    public class Provider : IDisposable
    {
        private CancellationTokenSource m_source;

        public Provider()
        {
            m_source = new CancellationTokenSource();
        }

        public void Dispose()
        {
            if (!m_source.IsCancellationRequested)
            {
                m_source.Cancel();
            }
        }

        public async Task Listen(Stream s)
        {
            var serializer = new ClassSerializer(s);
            RequestMessage rm = null;
            while ((rm = await s.DeserializeFromAsync<RequestMessage>(m_source.Token)) != null)
            {
                Type t = null;
                foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    t = a.GetType(rm.FullName);
                    if (t != null)
                        break;
                }
                
                var resp = new ResponseMessage() { TypeFollowing = t != null };
                resp.SerializeTo(s);

                if (t != null)
                    serializer.Serialize(t);
            }
        }

        public void Listen(int port)
        {
            var ln = new TcpListener(IPAddress.Any, port);
            ln.Start(10);

            while (!m_source.IsCancellationRequested)
            {
                var client = ln.AcceptTcpClient();
                Extensions.Go(() => { ServeClient(client); });
            }

            ln.Stop();         
        }

        private void ServeClient(TcpClient client)
        {
            if (m_source.IsCancellationRequested)
                return;

            var stream = client.GetStream();
            Listen(stream).Wait(m_source.Token);
        }
    }
}
