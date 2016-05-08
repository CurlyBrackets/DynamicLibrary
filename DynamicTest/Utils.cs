using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DynamicTest
{
    public static class Extensions
    {
        const int BufferSize = 1048576;

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

        public static void BitWrite(this Stream s, int item)
        {
            var b = BitConverter.GetBytes(item);
            s.Write(b, 0, b.Length);
        }

        public static int BitRead(this Stream s)
        {
            var b = new byte[sizeof(int)];
            s.Read(b, 0, b.Length);
            return BitConverter.ToInt32(b, 0);
        }

        public static void SerializeTo(this object o, Stream s)
        {
            var formatter = new BinaryFormatter();
            var ms = new MemoryStream();
            formatter.Serialize(ms, o);

            s.BitWrite((int)ms.Length);
            ms.Position = 0;
            ms.CopyToN(s, (int)ms.Length);
        }

        public static async Task<T> DeserializeFromAsync<T>(this Stream s) where T : class
        {
            return await s.DeserializeFromAsync<T>(CancellationToken.None);
        }

        public static async Task<T> DeserializeFromAsync<T>(this Stream s, CancellationToken ct) where T : class
        {
            var formatter = new BinaryFormatter();
            var buffer = new byte[BufferSize];
            int length = s.BitRead();
            using (var ms = new MemoryStream())
            {
                while(length > 0)
                {
                    int toRead = length > BufferSize ? BufferSize : length;
                    int read = await s.ReadAsync(buffer, 0, toRead, ct);
                    if (read == 0 || ct.IsCancellationRequested)
                        return null;

                    ms.Write(buffer, 0, read);
                    length -= read;
                }

                ms.Position = 0;
                var o = formatter.Deserialize(ms);
                return (T)o;
            }           
        }

        public static void Go(Action action)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(RunAction));
            thread.IsBackground = true;
            thread.Start(action);
            //ThreadPool.QueueUserWorkItem(new WaitCallback(RunAction), action);
        }

        private static void RunAction(object a)
        {
            ((Action)a)();
        }
    }
}
