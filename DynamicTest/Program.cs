using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dynamic
{
    class Adder
    {
        public int Add(int a, int b)
        {
            return a + b;
        }
    }

    class Program
    {
        static ManualResetEvent s_wait;

        static void Main(string[] args)
        {
            Debug.Listeners.Add(new ConsoleTraceListener());

            var executor = new Dynamic.RemoteExecutor();
            executor.Discover();

            int numCalls = 100000;
            var obj = new Adder();
            var param = new List<object[]>();
            for (int i = 0; i < numCalls; i++)
                param.Add(new object[] { i, i * i });

            s_wait = new ManualResetEvent(false);
            executor.BeginCall(obj, "Add", param, Done, executor);
            s_wait.WaitOne();
        }

        static void Done(IAsyncResult ar)
        {
            var executor = (RemoteExecutor)ar.AsyncState;
            var ret = executor.EndCall(ar);

            foreach (var r in ret)
                Console.Write("{0}\t", r);
            s_wait.Set();
        }
    }
}
