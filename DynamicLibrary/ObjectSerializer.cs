﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Dynamic
{
    public class ObjectSerializer
    {
        private Stream m_core;

        public ObjectSerializer(Stream core)
        {
            m_core = core;
        }

        public IAsyncResult BeginWrite(object o, AsyncCallback cb, object state)
        {
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);

            WriteCore(bw, o);

            m_core.BitWrite((int)ms.Length);
            var buf = ms.GetBuffer();
            return m_core.BeginWrite(buf, 0, (int)ms.Length, cb, state);
        }

        public void EndWrite(IAsyncResult ar)
        {
            m_core.EndWrite(ar);
        }

        public void Write(object o)
        {
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);

            WriteCore(bw, o);

            m_core.BitWrite((int)ms.Length);
            ms.Position = 0;
            ms.CopyToN(m_core, (int)ms.Length);
        }

        private void WriteCore(BinaryWriter bw, object o)
        {
            if (o == null)
            {
                bw.Write((byte)ETypeTag.Null);
                return;
            }

            var type = o.GetType();
            var tag = type.Tag();
            bw.Write((byte)tag);

            if(tag == ETypeTag.Object)
            {
                // write object
                bw.Write(type.FullName);
                if (type.IsGenericType)
                {
                    bw.Write(type.GenericTypeArguments.Length);
                    foreach (var t in type.GenericTypeArguments)
                        bw.Write(t.FullName);
                }
                else
                    bw.Write(0);

                var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                    .Where((f) => !f.IsDefined(typeof(CompilerGeneratedAttribute), false));
                bw.Write(fields.Count());
                foreach(var f in fields)
                {
                    bw.Write(f.Name);
                    WriteCore(bw, f.GetValue(o));
                }

                var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                bw.Write(props.Length);
                foreach(var p in props)
                {
                    bw.Write(p.Name);
                    WriteCore(bw, p.GetValue(o));
                }
            }
            else if(tag == ETypeTag.Array)
            {
                bw.Write(type.GetElementType().FullName);
                var arr = (Array)o;
                bw.Write(arr.Rank);
                for (int i = 0; i < arr.Rank; i++)
                    bw.Write(arr.GetLength(i));
                WriteArray(bw, arr, 0);
            }
            else
                WritePrimitive(bw, o, tag);
        }

        private void WriteArray(BinaryWriter bw, Array a, int dimension, params int[] indices)
        {
            if (a.Rank == dimension)
                WriteCore(bw, a.GetValue(indices));
            else
            {
                for (int i = 0; i < a.GetLength(dimension); i++)
                    WriteArray(bw, a, dimension + 1, indices.Append(i));
            }
        }

        private void WritePrimitive(BinaryWriter bw, object o, ETypeTag tag)
        {
            switch (tag)
            {
                case ETypeTag.Boolean:
                    bw.Write((bool)o);
                    break;
                case ETypeTag.Byte:
                    bw.Write((byte)o);
                    break;
                case ETypeTag.ByteArray:
                    var b = (byte[])o;
                    bw.Write(b.Length);
                    bw.Write(b);
                    break;
                case ETypeTag.Char:
                    bw.Write((char)o);
                    break;
                case ETypeTag.CharArray:
                    var c = (char[])o;
                    bw.Write(c.Length);
                    bw.Write(c);
                    break;
                case ETypeTag.Decimal:
                    bw.Write((decimal)o);
                    break;
                case ETypeTag.Double:
                    bw.Write((double)o);
                    break;
                case ETypeTag.Float:
                    bw.Write((float)o);
                    break;
                case ETypeTag.Int:
                    bw.Write((int)o);
                    break;
                case ETypeTag.Long:
                    bw.Write((long)o);
                    break;
                case ETypeTag.SByte:
                    bw.Write((sbyte)o);
                    break;
                case ETypeTag.Short:
                    bw.Write((short)o);
                    break;
                case ETypeTag.String:
                    bw.Write((string)o);
                    break;
                case ETypeTag.Uint:
                    bw.Write((uint)o);
                    break;
                case ETypeTag.Ulong:
                    bw.Write((ulong)o);
                    break;
                case ETypeTag.Ushort:
                    bw.Write((ushort)o);
                    break;
            }
        }
    }
}
