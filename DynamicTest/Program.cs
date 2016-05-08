using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace DynamicTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var a = new SomeClass(5);
            a.Init(6, 7);
            Console.WriteLine(a.BaseWork());

            //var stream = new MemoryStream();
            var provider = new Provider();
            Extensions.Go(() => provider.Listen(5335));
            //Extensions.Go(() => provider.Listen(stream));

            var runner = new Runner();
            runner.AddProvider("localhost", 5335);
            //runner.AddProvider(stream);

            var obj = runner.Instance("DynamicTest.SomeClass", 5);
            obj.Invoke("Init", 10, 7);
            Console.WriteLine(obj.Invoke("BaseWork"));
        }
    }
}
