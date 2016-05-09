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
        public List<object[]> Arguments { get; set; }
        public List<object> Results { get; set; }
        public object Object { get; set; }

        public RemoteRunnerConnection Connection { get; set; }

        public RunnerContext(RemoteRunnerConnection conn)
        {
            Connection = conn;
            Arguments = new List<object[]>();
            Results = new List<object>();
        }
    }
}
