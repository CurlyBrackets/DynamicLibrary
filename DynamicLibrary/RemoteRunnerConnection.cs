using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dynamic
{
    class RemoteRunnerConnection
    {
        const int BufferSize = 1500;

        private RemoteRunner m_parent;
        private TcpClient m_client;
        private NetworkStream m_stream;
        private MemoryStream m_bufferStream;

        private byte[] m_buffer;
        private int m_todo;

        public RemoteRunnerConnection(RemoteRunner parent, TcpClient client)
        {
            m_parent = parent;
            m_client = client;
            m_stream = m_client.GetStream();

            m_buffer = new byte[BufferSize];
            m_bufferStream = new MemoryStream();
        }

        public void Start()
        {
            ReadLength();
        }

        private void LengthCallback(IAsyncResult ar)
        {
            int read = m_stream.EndRead(ar);
            if (read < sizeof(int))
                return; //shit

            int length = BitConverter.ToInt32(m_buffer, 0);
            if (read > sizeof(int))
            {
                m_bufferStream.Write(m_buffer, sizeof(int), read - sizeof(int));
                length -= (read - sizeof(int));
            }

            m_todo = length;
            ReadMessage();
        }

        private void ReadLength()
        {
            m_stream.BeginRead(m_buffer, 0, BufferSize, LengthCallback, null);
        }

        private void ReadMessage()
        {
            int amount = m_todo > BufferSize ? BufferSize : m_todo;
            m_stream.BeginRead(m_buffer, 0, amount, ReadCallback, null);
        }

        private void ReadCallback(IAsyncResult ar)
        {
            int read = m_stream.EndRead(ar);
            if(read > 0)
            {
                m_bufferStream.Write(m_buffer, 0, read);
                m_todo -= read;
                if (m_todo == 0)
                {
                    ProcessMessage();
                    ReadLength();
                }
                else
                    ReadMessage();
            }
        }

        private void ProcessMessage()
        {
            var od = new ObjectDeserializer(m_bufferStream);

            var ctx = new RunnerContext(this);
            var msg = (RunMessage)od.Read();
            string staticName = string.Empty;

            if (msg.IsStatic)
                staticName = (string)od.Read();
            else
                ctx.Object = od.Read();
            
            for(int i = 0; i < msg.NumCalls; i++)
            {
                var args = new object[msg.NumArguments];
                for (int j = 0; j < msg.NumArguments; j++)
                    args[j] = od.Read();

                ctx.Arguments.Add(args);
            }

            if (msg.IsStatic)
                ctx.MethodInfo = Type.GetType(staticName).GetMethod(
                    msg.MethodName, 
                    BindingFlags.Static | BindingFlags.FlattenHierarchy, 
                    null, 
                    ctx.Arguments[0].Select(o => o.GetType()).ToArray(), 
                    null);
            else
                ctx.MethodInfo = ctx.Object.GetType().GetMethod(
                    msg.MethodName,
                    BindingFlags.Instance | BindingFlags.FlattenHierarchy,
                    null,
                    ctx.Arguments[0].Select(o => o.GetType()).ToArray(),
                    null);

            // parent run context
            Extensions.Go(() => { m_parent.Execute(ctx); });
        }

        internal void Respond(RunnerContext ctx)
        {
            using(var ms = new MemoryStream())
            {
                var resp = new RunResultMessage { Success = true, NumResults = ctx.Results.Count };
                var os = new ObjectSerializer(ms);
                os.Write(resp);
                foreach (var res in ctx.Results)
                    os.Write(res);

                ms.CopyToN(m_stream, (int)ms.Length);
            }
        }
    }
}
