using Dynamic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dynamic
{
    class RunnerContext
    {
        public MethodInfo MethodInfo { get; set; }
        public List<object> Results { get; set; }
        public object Object { get; set; }

        private int m_argsReceived;
        public int ArgumentsLeft { get { return TotalCalls - m_argsReceived; } }
        public int TotalCalls { get; private set; }
        public int CompletedCalls { get; private set; }
        public int CallsLeft
        {
            get
            {
                return TotalCalls - CompletedCalls;
            }
        }
        public int CallIndex
        {
            get
            {
                return m_argsReceived - 1;
            }
        }

        public RemoteRunnerConnection Connection { get; set; }

        public RunnerContext(RemoteRunnerConnection conn, int numCalls)
        {
            Connection = conn;
            Results = new List<object>(new object[numCalls]);
            
            TotalCalls = numCalls;
            CompletedCalls = 0;
        }

        public void PutResult(object o, int index)
        {
            Results[index] = o;
            CompletedCalls++;
        }

        public void ReceiveArguments()
        {
            m_argsReceived++;
        }
    }
}
