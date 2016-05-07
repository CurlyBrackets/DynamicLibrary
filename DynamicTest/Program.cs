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

            var stream = new MemoryStream();
            var input = new ClassSerializer(stream);
            var output = new ClassDeserializer(null);
            output.SetStream(stream);

            input.Serialize(typeof(BaseClass));
            input.Serialize(typeof(SomeClass));

            stream.Position = 0;

            var tb = output.Deserialize();
            var t = output.Deserialize();
            var obj = output.Instance(t, 5);

            obj.Invoke("Init", 10, 7);
            Console.WriteLine(obj.Invoke("BaseWork"));
        }
    }
}
