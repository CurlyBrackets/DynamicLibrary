using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Debug.Listeners.Add(new ConsoleTraceListener());

            var runner = new Dynamic.RemoteRunner(8);
            runner.Listen();

            Console.WriteLine("Press any key to stop");
            Console.ReadKey();
        }
    }
}
