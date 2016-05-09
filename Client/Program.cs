using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            var runner = new Dynamic.RemoteRunner(8);
            runner.Listen();

            Console.WriteLine("Press any key to stop");
            Console.ReadKey();
        }
    }
}
