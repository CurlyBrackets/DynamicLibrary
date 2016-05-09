using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dynamic
{
    class ClassSerializer : IDisposable
    {
        private Stream m_core;

        public ClassSerializer(Stream target)
        {
            m_core = target;
        }

        public void Dispose()
        {
            m_core.Dispose();
        }

        public void Serialize(Type t)
        {
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);

            SerializeCore(bw, t);

            m_core.BitWrite((int)ms.Length);
            ms.Position = 0;
            ms.CopyToN(m_core, (int)ms.Length);
        }

        public void SerializeCore(BinaryWriter bw, Type t)
        {
            bw.Write(t.FullName);
            bw.Write((uint)t.Attributes);
            bw.Write(t.BaseType.FullName);

            // TODO custom attr for t
            var cas = t.GetCustomAttributesData();
            bw.Write(cas.Count);
            if (cas.Count > 0)
            {
                var os = new ObjectSerializer(bw.BaseStream);
                foreach (var ca in cas)
                {
                    bw.Write(ca.AttributeType.FullName);
                    var ctorArgs = ca.ConstructorArguments;
                    bw.Write(ctorArgs.Count);
                    foreach (var arg in ctorArgs)
                        os.Write(arg.Value);

                    var namedArgs = ca.NamedArguments;
                    bw.Write(namedArgs.Count);
                    foreach (var arg in namedArgs)
                    {
                        bw.Write(arg.IsField);
                        bw.Write(arg.MemberName);
                        os.Write(arg.TypedValue.Value);
                    }
                }
            }

            var interfaces = t.GetInterfaces();
            bw.Write(interfaces.Length);
            foreach (var i in interfaces)
                bw.Write(i.FullName);

            // declarations

            var fields = t.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            bw.Write(fields.Length);
            foreach (var f in fields)
            {
                bw.Write(f.Name);
                bw.Write(f.FieldType.FullName);
                bw.Write((uint)f.Attributes);

                // TODO custom attr for f
                //f.GetCustomAttributesData();

                // TODO constant?
                bw.Write(Names.Field(f));
            }

            var methodsTodo = new List<MethodBase>();
            var methods = t.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Where((m) => !m.IsSpecialName);
            bw.Write(methods.Count());
            foreach (var m in methods)
            {
                Write(bw, m);
                methodsTodo.Add(m);
            }

            var props = t.GetProperties();
            bw.Write(props.Length);
            foreach (var pr in props)
            {
                Console.WriteLine(pr.Name);
                bw.Write(pr.Name);
                bw.Write((uint)pr.Attributes);
                bw.Write(pr.PropertyType.FullName);

                var ps = pr.GetIndexParameters();
                bw.Write(ps.Length);
                foreach (var p in ps)
                    bw.Write(p.ParameterType.FullName);

                // TODO custom attr for pr

                // default value
                bw.Write(false);

                bw.Write(pr.CanRead);
                if (pr.CanRead)
                {
                    Write(bw, pr.GetMethod);
                    methodsTodo.Add(pr.GetMethod);
                }
                bw.Write(pr.CanWrite);
                if (pr.CanWrite)
                {
                    Write(bw, pr.SetMethod);
                    methodsTodo.Add(pr.SetMethod);
                }
            }

            

            var ctors = t.GetConstructors();
            bw.Write(ctors.Length);
            foreach (var c in ctors)
            {
                Write(bw, c);
                methodsTodo.Add(c);
            }

            var ti = t.TypeInitializer;
            bw.Write(ti != null);
            if (ti != null)
            {
                Write(bw, ti, true);
                methodsTodo.Add(ti);
            }

            // definitions

            bw.Write(methodsTodo.Count);
            foreach (var m in methodsTodo)
            {
                bw.Write(Names.Method(m));
                if (!m.IsAbstract)
                    Write(bw, t.Module, m.GetMethodBody());
            }

            // others

            var nts = t.GetNestedTypes();
            bw.Write(nts.Length);
            foreach (var nt in nts)
                SerializeCore(bw, nt);            

            
            
        }

        private void Write(BinaryWriter bw, ConstructorInfo c, bool ignore = false)
        {
            if (!ignore)
            {
                bw.Write((uint)c.Attributes);
                bw.Write((uint)c.CallingConvention);

                var ps = c.GetParameters();
                bw.Write(ps.Length);
                foreach (var p in ps)
                    bw.Write(p.ParameterType.FullName);

                bw.Write(ps.Length);
                foreach (var p in ps)
                    Write(bw, p);
            }

            // TODO custom attr for c
            bw.Write(Names.Method(c));
        }

        private void Write(BinaryWriter bw, MethodInfo m)
        {
            bw.Write(m.Name);
            bw.Write((uint)m.Attributes);
            bw.Write((uint)m.CallingConvention);
            bw.Write(m.ReturnType.FullName);

            var ps = m.GetParameters();
            bw.Write(ps.Length);
            foreach (var p in ps)
                bw.Write(p.ParameterType.FullName);

            // TODO custom attr for m

            bw.Write(ps.Length);
            foreach (var p in ps)
                Write(bw, p);

            // todo override?
            bw.Write(Names.Method(m));
        }

        private void Write(BinaryWriter bw, ParameterInfo p)
        {
            bw.Write(p.Position);
            bw.Write((uint)p.Attributes);
            bw.Write(p.Name);

            // TODO custom attr for p

            bw.Write(false);
            //m_bw.Write(p.HasDefaultValue);
            //m_bw.Write(p.DefaultValue);
        }

        private void Write(BinaryWriter bw, Module module, MethodBody mb)
        {
            var locals = mb.LocalVariables;
            bw.Write(locals.Count);
            for (int i=0;i< locals.Count;i++)
            {
                bw.Write(locals[i].LocalType.FullName);
                bw.Write(locals[i].IsPinned);
            }

            var il = mb.GetILAsByteArray();
            for(int i = 0; i < il.Length;)
            {
                EArgumentType argType = EArgumentType.None;
                var opcode = (EOpCode)il[i];
                bw.Write(il[i]);
                i++;

                if(opcode == EOpCode.Extended)
                {
                    bw.Write(il[i]);
                    argType = ((EExtendedOpCode)il[i]).ArgFor();
                    i++;
                }
                else
                    argType = opcode.ArgFor();

                switch (argType)
                {
                    case EArgumentType.Field:
                        var fi = module.ResolveField(BitConverter.ToInt32(il, i));
                        i += 4;
                        bw.Write(Names.Field(fi));
                        break;
                    case EArgumentType.Float32:
                        bw.Write(BitConverter.ToSingle(il, i));
                        i += 4;
                        break;
                    case EArgumentType.Float64:
                        bw.Write(BitConverter.ToDouble(il, i));
                        i += 8;
                        break;
                    case EArgumentType.Token:
                    case EArgumentType.Int32:
                        bw.Write(BitConverter.ToInt32(il, i));
                        i += 4;
                        break;
                    case EArgumentType.Int64:
                        bw.Write(BitConverter.ToInt64(il, i));
                        i += 8;
                        break;
                    case EArgumentType.Int8:
                        bw.Write((sbyte)il[i]);
                        i++;
                        break;
                    case EArgumentType.ListOfInt:
                        uint count = BitConverter.ToUInt32(il, i);
                        bw.Write(count);
                        i += 4;
                        while(count > 0)
                        {
                            bw.Write(BitConverter.ToInt32(il, i));
                            i += 4;
                        }
                        break;
                    case EArgumentType.Method:
                        var mi = module.ResolveMethod(BitConverter.ToInt32(il, i));
                        i += 4;
                        bw.Write(Names.Method(mi));
                        break;
                    case EArgumentType.String:
                        var str = module.ResolveString(BitConverter.ToInt32(il, i));
                        i += 4;
                        bw.Write(str);
                        break;
                    case EArgumentType.Type:
                        var t = module.ResolveType(BitConverter.ToInt32(il, i));
                        i += 4;
                        bw.Write(t.FullName);
                        break;
                    case EArgumentType.Uint16:
                        bw.Write(BitConverter.ToUInt16(il, i));
                        i += 2;
                        break;
                    case EArgumentType.Uint32:
                        bw.Write(BitConverter.ToUInt32(il, i));
                        i += 4;
                        break;
                    case EArgumentType.Uint8:
                        bw.Write(il[i]);
                        i++;
                        break;
                }
            }

            bw.Write((byte)EOpCode.Terminator);
            /*

            var handlers = mb.ExceptionHandlingClauses;
            m_bw.Write(handlers.Count);
            foreach(var h in handlers)
            {
                m_bw.Write(h.TryOffset);
                m_bw.Write(h.TryLength);
                m_bw.Write(h.FilterOffset);
                m_bw.Write(h.HandlerOffset);
                m_bw.Write(h.HandlerLength);
                m_bw.Write((uint)h.Flags);
                
                //m_bw.Write(h.)
            }*/
        }
    }
}
