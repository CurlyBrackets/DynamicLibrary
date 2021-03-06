﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dynamic
{
    public class ObjectDeserializer
    {
        private Stream m_core;

        public class TypeProviderResult
        {
            public string TypeName { get; private set; }
            public Type Result { get; private set; }
            public bool TypeFound { get; private set; }

            public TypeProviderResult(string name)
            {
                TypeName = name;
                Result = null;
                TypeFound = false;
            }

            public void SetResult(Type t)
            {
                if (t != null)
                {
                    TypeFound = true;
                    Result = t;
                }
            }
        }

        public delegate void TypeSearch(object sender, TypeProviderResult e);

        public event TypeSearch TypeProviders = delegate { };

        public static void DefaultTypeSearch(object o, TypeProviderResult e)
        {
            if (!e.TypeFound)
            {
                foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var t = a.GetType(e.TypeName);
                    if (t != null) {
                        e.SetResult(t);
                        break;
                    }
                }
            }
        }

        public ObjectDeserializer(Stream core)
        {
            m_core = core;
            TypeProviders += DefaultTypeSearch;
        }

        private int m_readId = 0;
        private Dictionary<int, byte[]> m_buffers;

        public IAsyncResult BeginRead(AsyncCallback cb, object state)
        {
            if (m_buffers == null)
                m_buffers = new Dictionary<int, byte[]>();

            int length = m_core.BitRead();
            var buffer = new byte[length];
            var wrapper = new AsyncWrapper(Interlocked.Increment(ref m_readId));
            m_buffers.Add(wrapper.Id, buffer);
            var ret = m_core.BeginRead(buffer, 0, length, ar => {
                wrapper.Core = ar;
                cb(wrapper);
            }, state);
                        
            return ret;
        }

        public object EndRead(IAsyncResult ar)
        {
            var aw = (AsyncWrapper)ar;
            var id = aw.Id;
            var read = m_core.EndRead(aw.Core);
            var buf = m_buffers[id];
            m_buffers.Remove(id);

            using (var ms = new MemoryStream(buf, 0, read))
            using (var br = new BinaryReader(ms))
                return ReadCore(br);
        }

        private class AsyncWrapper : IAsyncResult
        {
            public object AsyncState
            {
                get
                {
                    return Core.AsyncState;
                }
            }

            public WaitHandle AsyncWaitHandle
            {
                get
                {
                    return Core.AsyncWaitHandle;
                }
            }

            public bool CompletedSynchronously
            {
                get
                {
                    return Core.CompletedSynchronously;
                }
            }

            public bool IsCompleted
            {
                get
                {
                    return Core.IsCompleted;
                }
            }

            public int Id { get; private set; }
            public IAsyncResult Core { get; set; }

            public AsyncWrapper(int id)
            {
                Id = id;
            }

        }

        public object Read()
        {
            int length = m_core.BitRead();
            var ms = new MemoryStream(length);
            m_core.CopyToN(ms, length);
            ms.Position = 0; // reset after writing
            var br = new BinaryReader(ms);
            return ReadCore(br);
        }

        private object ReadCore(BinaryReader br)
        {
            var tag = (ETypeTag)br.ReadByte();
            if (tag == ETypeTag.Null)
                return null;
            else if (tag == ETypeTag.Object)
            {
                var typename = br.ReadString();
                var type = GetType(typename);
                if (type == null)
                    return null;

                int num = br.ReadInt32();
                if (num > 0)
                {
                    var types = new Type[num];
                    for (int i = 0; i < num; i++)
                        types[i] = GetType(br.ReadString());
                    type = type.MakeGenericType(types);
                }

                //var ret = Activator.CreateInstance(type);
                var ret = FormatterServices.GetUninitializedObject(type);

                num = br.ReadInt32();
                for (; num > 0; num--)
                {
                    // get field
                    var name = br.ReadString();
                    var f = type.GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.FlattenHierarchy);
                    f.SetValue(ret, ReadCore(br));
                }

                num = br.ReadInt32();
                for (; num > 0; num--)
                {
                    // get prop
                    var p = type.GetProperty(br.ReadString());
                    p.SetValue(ret, ReadCore(br));
                }

                return ret;
            }
            else if (tag == ETypeTag.Array)
            {
                var typename = br.ReadString();
                var type = GetType(typename);
                if (type == null)
                    return null;

                var rank = br.ReadInt32();
                var lengths = new int[rank];
                for (int i = 0; i < rank; i++)
                    lengths[i] = br.ReadInt32();

                var arr = Array.CreateInstance(type, lengths);

                ReadArray(br, arr, 0);

                return arr;
            }
            else
                return ReadPrimitive(br, tag);
        }

        private void ReadArray(BinaryReader br, Array arr, int dimension, params int[] indices)
        {
            if (arr.Rank == dimension)
                arr.SetValue(ReadCore(br), indices);
            else
            {
                for (int i = 0; i < arr.GetLength(dimension); i++)
                    ReadArray(br, arr, dimension + 1, indices.Append(i));
            }
        }

        internal Type GetType(string name)
        {
            var res = new TypeProviderResult(name);
            TypeProviders(this, res);
            if (!res.TypeFound)
                return null;

            return res.Result;
        }

        private object ReadPrimitive(BinaryReader br, ETypeTag tag)
        {
            switch (tag)
            {
                case ETypeTag.Boolean:
                    return br.ReadBoolean();
                case ETypeTag.Byte:
                    return br.ReadByte();
                case ETypeTag.ByteArray:
                    return br.ReadBytes(br.ReadInt32());
                case ETypeTag.Char:
                    return br.ReadChar();
                case ETypeTag.CharArray:
                    return br.ReadChars(br.ReadInt32());
                case ETypeTag.Decimal:
                    return br.ReadDecimal();
                case ETypeTag.Double:
                    return br.ReadDouble();
                case ETypeTag.Float:
                    return br.ReadSingle();
                case ETypeTag.Int:
                    return br.ReadInt32();
                case ETypeTag.Long:
                    return br.ReadInt64();
                case ETypeTag.SByte:
                    return br.ReadSByte();
                case ETypeTag.Short:
                    return br.ReadInt16();
                case ETypeTag.String:
                    return br.ReadString();
                case ETypeTag.Uint:
                    return br.ReadUInt32();
                case ETypeTag.Ulong:
                    return br.ReadUInt64();
                case ETypeTag.Ushort:
                    return br.ReadUInt16();
                default:
                    return null;
            }
        }
    }
}
