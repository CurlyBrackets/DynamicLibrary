using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dynamic
{
    class RemoteRunnerConnection
    {
        struct State
        {
            public RunMessage Msg;
            public RunnerContext Ctx;
        }

        const int BufferSize = 1500;

        private RemoteRunner m_parent;
        private TcpClient m_client;
        private NetworkStream m_stream;
        private ObjectDeserializer m_output;
        private ClassManager m_manager;

        public RemoteRunnerConnection(RemoteRunner parent, TcpClient client)
        {
            m_parent = parent;
            m_client = client;
            m_stream = m_client.GetStream();
            m_output = new ObjectDeserializer(m_stream);
            m_manager = new ClassManager();
            m_manager.AddProvider(((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString(), RemoteRunner.CodePort);
            m_output.TypeProviders += (o, e) =>
            {
                if (!e.TypeFound)
                {
                    var t = m_manager.RequestType(e.TypeName);
                    if (t != null)
                        e.SetResult(t);
                }
            };
        }

        public void Start()
        {
            try
            {
                Debug.Print("+ RemoteRunnerConnection.Start");

                m_output.BeginRead(ProcessMessage, null);
            }
            catch (IOException)
            {
                m_client.Close();
            }
        }

        private void ProcessMessage(IAsyncResult ar)
        {
            Debug.Print("+ RemoteRunnerConnection.ProcessMessage");

            var msg = (RunMessage)m_output.EndRead(ar);
            var ctx = new RunnerContext(this, msg.NumCalls);

            m_output.BeginRead(ProcessObject, new State() { Msg = msg, Ctx = ctx });
        }

        private void ProcessObject(IAsyncResult ar)
        {
            Debug.Print("+ RemoteRunnerConnection.ProcessObject");

            var state = (State)ar.AsyncState;
            var obj = m_output.EndRead(ar);

            var types = state.Msg.TypeNames.Select(tn => m_output.GetType(tn)).ToArray();

            if (!state.Msg.IsStatic)
            {
                state.Ctx.Object = obj;
                var type = obj.GetType();
                state.Ctx.MethodInfo = obj.GetType().GetMethod(
                    state.Msg.MethodName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy,
                    null,
                    types,
                    null);
            }
            else
            {
                state.Ctx.MethodInfo = m_output.GetType((string)obj).GetMethod(
                    state.Msg.MethodName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy,
                    null,
                    types,
                    null);
            }

            m_output.BeginRead(ProcessArgs, state.Ctx);
        }

        private void ProcessArgs(IAsyncResult ar)
        {
            var ctx = (RunnerContext)ar.AsyncState;
            Debug.Print("+ RemoteRunnerConnection.ProcessArgs - " + ctx.ArgumentsLeft);

            var args = (object[])m_output.EndRead(ar);

            ctx.ReceiveArguments();
            m_parent.Enqueue(ctx, args);

            if (ctx.ArgumentsLeft <= 0)
                Start();
            else
                m_output.BeginRead(ProcessArgs, ctx);
        }

        /*
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
                var args = (object[])od.Read();
                ctx.Arguments.Add(args);
            }

            //if (msg.IsStatic)
                
            else
                

            // parent run context
            Extensions.Go(() => { m_parent.Execute(ctx); });
        }*/

        internal void Respond(RunnerContext ctx)
        {
            Debug.Print("+ RemoteRunnerConnection.Respond");

            var resp = new RunResultMessage { Success = true, NumResults = ctx.Results.Count };
            var os = new ObjectSerializer(m_stream);
            os.Write(resp);
            foreach (var res in ctx.Results)
                os.Write(res);

            Debug.Print("- RemoteRunnerConnection.Respond");
        }
    }
}
