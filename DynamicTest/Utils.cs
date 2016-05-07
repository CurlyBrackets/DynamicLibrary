using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace DynamicTest
{
    public static class Extensions
    {
        public static object Invoke(this object thiz, string name, params object[] p)
        {
            var t = thiz.GetType();
            var types = p.Select((o) => o.GetType()).ToArray();

            var mi = t.GetMethod(name, types);
            if (mi == null)
                throw new MissingMethodException();

            return mi.Invoke(thiz, p);
        }

        public static int CopyToN(this Stream src, Stream dest, int amount)
        {
            const int BufferSize = 32768;
            byte[] buffer = new byte[BufferSize];
            int left = amount, toRead, read;
            while (left > 0)
            {
                if (left < BufferSize)
                    toRead = left;
                else
                    toRead = BufferSize;
                read = src.Read(buffer, 0, toRead);
                if (read <= 0)
                    break;

                dest.Write(buffer, 0, read);
                left -= read;
            }

            return amount - left;
        }

        public static void SerializeTo(this object o, Stream s)
        {
            var formatter = new BinaryFormatter();
            formatter.Serialize(s, o);
        }

        public static T DeserializeFrom<T>(this Stream s)
        {
            var formatter = new BinaryFormatter();
            var buffer = new byte[]
            var o = formatter.Deserialize(s);
            
            return (T)o;
        }
    }
}
