using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Dynamic
{
    struct ClassFrame
    {
        public Dictionary<string, FieldBuilder> Fields;
        public Dictionary<string, MethodBuilder> Methods;
        public Dictionary<string, ConstructorBuilder> Constructors;
        public TypeBuilder Builder;
        public BinaryReader Br;
    }

    class ClassDeserializer
    {
        private const string Namespace = "Dynamic.";

        private ClassManager m_parent;

        private Stream m_core;
        private BinaryReader m_br;

        private AppDomain m_ad;
        private AssemblyBuilder m_ab;
        private ModuleBuilder m_mb;

        private Stack<ClassFrame> m_stack;
        private Dictionary<string, FieldBuilder> m_fields;
        private Dictionary<string, MethodBuilder> m_methods;
        private Dictionary<string, ConstructorBuilder> m_ctors;
        private TypeBuilder m_currentTb;

        public ClassDeserializer(ClassManager parent)
        {
            m_parent = parent;

            var an = new AssemblyName()
            {
                Name = "HelloReflectionEmit"
            };

            m_ad = AppDomain.CurrentDomain;
            m_ab = m_ad.DefineDynamicAssembly(an, System.Reflection.Emit.AssemblyBuilderAccess.RunAndSave);
            m_mb = m_ab.DefineDynamicModule(an.Name, "Hello.exe");

            m_stack = new Stack<ClassFrame>();
        }

        public void SetStream(Stream s)
        {
            m_core = s;
            //m_br = new BinaryReader(m_core);
        }

        private void ResetState()
        {
            m_fields = new Dictionary<string, FieldBuilder>();
            m_methods = new Dictionary<string, MethodBuilder>();
            m_ctors = new Dictionary<string, ConstructorBuilder>();
            m_currentTb = null;
            m_br = null;
        }

        private void PushState()
        {
            m_stack.Push(
                new ClassFrame()
                {
                    Fields = m_fields,
                    Methods = m_methods,
                    Constructors = m_ctors,
                    Builder = m_currentTb,
                    Br = m_br,
                });
            ResetState();
        }

        private void PopState()
        {
            if (m_stack.Count > 0)
            {
                var frame = m_stack.Pop();
                m_fields = frame.Fields;
                m_methods = frame.Methods;
                m_ctors = frame.Constructors;
                m_currentTb = frame.Builder;
                m_br = frame.Br;
            }
            else
                ResetState();
        }

        public void Dispose()
        {
            if(m_br != null)
                m_br.Dispose();
            m_core.Dispose();
        }

        public Type Deserialize()
        {
            ResetState();

            int length = m_core.BitRead();

            var ms = new MemoryStream(length);
            m_core.CopyToN(ms, length);
            ms.Position = 0; // reset after writing
            m_br = new BinaryReader(ms);

            return DeserializeCore(
                m_mb.DefineType(
                    Name(m_br.ReadString()),
                    (TypeAttributes)m_br.ReadUInt32(),
                    ReadType()));
        }

        private void DeserializeNested(TypeBuilder tb)
        {
            PushState();

            DeserializeCore(
                tb.DefineNestedType(
                    Name(m_br.ReadString()),
                        (TypeAttributes)m_br.ReadUInt32(),
                        ReadType()));

            PopState();
        }

        private Type DeserializeCore(TypeBuilder tb)
        {
            m_currentTb = tb;
            // TODO custom attr for tb
            ReadCustomAttributes(tb);

            foreach (var t in ReadTypeArray())
                tb.AddInterfaceImplementation(t);

            int num = m_br.ReadInt32();
            for (int i = 0; i < num; i++)
            {
                var fb = tb.DefineField(
                    m_br.ReadString(),
                    ReadType(),
                    (FieldAttributes)m_br.ReadUInt32());

                // TODO custom attr for fb
                //Console.WriteLine($"Read => {fb.Name} = {fb.MetadataToken:X8}");

                m_fields.Add(m_br.ReadString(), fb);
            }

            num = m_br.ReadInt32();
            for (int i = 0; i < num; i++)
                ReadMethod();

            // read props
            num = m_br.ReadInt32();
            for (int i = 0; i < num; i++)
            {
                var pb = tb.DefineProperty(
                    m_br.ReadString(),
                    (PropertyAttributes)m_br.ReadUInt32(),
                    ReadType(),
                    ReadTypeArray());

                // TODO custom attr for pb

                if (m_br.ReadBoolean())
                {
                    // default value
                }

                if (m_br.ReadBoolean()) // canread
                    pb.SetGetMethod(ReadMethod());
                if (m_br.ReadBoolean()) // canwrite
                    pb.SetSetMethod(ReadMethod());

            }


            num = m_br.ReadInt32();
            for (int i = 0; i < num; i++)
            {
                var cb = tb.DefineConstructor(
                    (MethodAttributes)m_br.ReadUInt32(),
                    (CallingConventions)m_br.ReadUInt32(),
                    ReadTypeArray());

                ReadConstructor(cb);
            }

            if (m_br.ReadBoolean())
            {
                var ti = tb.DefineTypeInitializer();
                ReadConstructor(ti, true);
            }

            num = m_br.ReadInt32();
            for (int i = 0; i < num; i++)
            {
                var mb = ResolveMethod(m_br.ReadString());
                if (!mb.IsAbstract)
                    ReadMethodBody(mb);
            }

            num = m_br.ReadInt32();
            for (int i = 0; i < num; i++)
                DeserializeNested(tb);

            

            return tb.CreateType();
        }

        private Type ReadType()
        {
            var name = m_br.ReadString();
            return ResolveType(name);
            
        }

        private Type[] ReadTypeArray()
        {
            int num = m_br.ReadInt32();
            var ret = new Type[num];
            for (int i = 0; i < num; i++)
                ret[i] = ReadType();
            return ret;
        }

        private byte[] ReadArray()
        {
            return m_br.ReadBytes(m_br.ReadInt32());
        }

        private ExceptionHandler[] ReadExceptionHandlerArray()
        {
            int num = m_br.ReadInt32();
            var ret = new ExceptionHandler[num];
            for (int i = 0; i < num; i++) {
                ret[i] = new ExceptionHandler(
                    tryOffset: m_br.ReadInt32(),
                    tryLength: m_br.ReadInt32(),
                    filterOffset: m_br.ReadInt32(),
                    handlerOffset: m_br.ReadInt32(),
                    handlerLength: m_br.ReadInt32(),
                    kind: (ExceptionHandlingClauseOptions)m_br.ReadUInt32(),
                    exceptionTypeToken: /*m_br.ReadInt32()*/ 0
                    );
            }
            return ret;
        }

        private void ReadCustomAttributes(dynamic target)
        {
            int num = m_br.ReadInt32();
            if (num > 0) {
                var od = new ObjectDeserializer(m_br.BaseStream);
                od.TypeProviders += (o, e) =>
                {
                    if (!e.TypeFound)
                    {
                        var t = ResolveType(e.TypeName);
                        if (t != null)
                            e.SetResult(t);
                    }
                };

                for (int i = 0; i < num; i++)
                {
                    List<object> ctorArgs = new List<object>(), fieldValues = new List<object>(), propValues = new List<object>();
                    var fis = new List<FieldInfo>();
                    var pis = new List<PropertyInfo>();

                    var type = ResolveType(m_br.ReadString());
                    int num2 = m_br.ReadInt32();
                    for (; num2 > 0; num2--)
                        ctorArgs.Add(od.Read());

                    num2 = m_br.ReadInt32();
                    for (;num2 > 0;num2--)
                    {
                        bool isField = m_br.ReadBoolean();
                        var name = m_br.ReadString();
                        var val = od.Read();

                        if (isField)
                        {
                            var fi = type.GetField(name);
                            if(fi != null)
                            {
                                fis.Add(fi);
                                fieldValues.Add(val);
                            }
                        }
                        else
                        {
                            var pi = type.GetProperty(name);
                            if(pi != null)
                            {
                                pis.Add(pi);
                                propValues.Add(val);
                            }
                        }
                    }

                    var ci = type.GetConstructor(ctorArgs.Select(o => o.GetType()).ToArray());

                    var attrBuilder = new CustomAttributeBuilder(
                        ci,
                        ctorArgs.ToArray(),
                        pis.ToArray(),
                        propValues.ToArray(),
                        fis.ToArray(),
                        fieldValues.ToArray());
                    target.SetCustomAttribute(attrBuilder);
                }
            }
        }

        private void ReadParameter(dynamic item)
        {
            ReadParameterCore(
                item.DefineParameter(
                    m_br.ReadInt32(),
                    (ParameterAttributes)m_br.ReadUInt32(),
                    m_br.ReadString()));
        }

        private void ReadParameterCore(ParameterBuilder pb)
        {
            // TODO custom attr for pb
            if (m_br.ReadBoolean())
            {
                // default value
            }
        }

        private ConstructorBuilder ReadConstructor(ConstructorBuilder cb, bool ignore = false)
        {
            if (!ignore)
            {
                int num2 = m_br.ReadInt32();
                for (int j = 0; j < num2; j++)
                    ReadParameter(cb);
            }

            // TODO custom attr for cb

            var id = m_br.ReadString();
            m_ctors.Add(id, cb);
            return cb;
        }

        private MethodBuilder ReadMethod()
        {
            var mb = m_currentTb.DefineMethod(
                    m_br.ReadString(),
                    (MethodAttributes)m_br.ReadUInt32(),
                    (CallingConventions)m_br.ReadUInt32(),
                    ReadType(),
                    ReadTypeArray());

            // TODO custom attr for mb
            int num2 = m_br.ReadInt32();
            for (int j = 0; j < num2; j++)
                ReadParameter(mb);

            var id = m_br.ReadString();
            m_methods.Add(id, mb);
            return mb;
        }

        private void ReadMethodBody(dynamic builder)
        {
            ILGenerator gen = builder.GetILGenerator();
            var count = m_br.ReadInt32();
            for(int i = 0; i < count; i++)
            {
                gen.DeclareLocal(
                    ReadType(),
                    m_br.ReadBoolean());
            }

            while (true)
            {
                var opcode = (EOpCode)m_br.ReadByte();
                if (opcode == EOpCode.Terminator)
                    break;

                EArgumentType argType = EArgumentType.None;
                OpCode o = default(OpCode);

                if(opcode == EOpCode.Extended)
                {
                    var eopcode = (EExtendedOpCode)m_br.ReadByte();
                    argType = eopcode.ArgFor();
                    o = Translate(eopcode);
                }
                else
                {
                    argType = opcode.ArgFor();
                    o = Translate(opcode);
                }

                if (opcode == EOpCode.Call || opcode == EOpCode.Callvirt)
                {
                    var m = ResolveMethod(m_br.ReadString());
                    if (m is MethodInfo)
                        gen.EmitCall(o, m, null);
                    else
                    {
                        if (opcode == EOpCode.Call)
                            gen.Emit(OpCodes.Call, m);
                        else
                            gen.Emit(OpCodes.Calli, m);
                    }
                }
                else if(opcode == EOpCode.Calli)
                {
                    gen.Emit(OpCodes.Nop);
                }
                else {
                    switch (argType)
                    {
                        case EArgumentType.Field:
                            gen.Emit(o, ResolveField(m_br.ReadString()));
                            break;
                        case EArgumentType.Float32:
                            gen.Emit(o, m_br.ReadSingle());
                            break;
                        case EArgumentType.Float64:
                            gen.Emit(o, m_br.ReadDouble());
                            break;
                        case EArgumentType.Int32:
                            gen.Emit(o, m_br.ReadInt32());
                            break;
                        case EArgumentType.Int64:
                            gen.Emit(o, m_br.ReadInt64());
                            break;
                        case EArgumentType.Int8:
                            gen.Emit(o, m_br.ReadSByte());
                            break;
                        case EArgumentType.ListOfInt:
                            //just read the items and don't emit a code
                            var num = m_br.ReadUInt32();
                            for (; num > 0; num--)
                                m_br.ReadInt32();
                            break;
                        case EArgumentType.Method:
                            gen.Emit(o, ResolveMethod(m_br.ReadString()));
                            break;
                        case EArgumentType.None:
                            gen.Emit(o);
                            break;
                        case EArgumentType.String:
                            gen.Emit(o, m_br.ReadString());
                            break;
                        case EArgumentType.Token:
                            gen.Emit(o, m_br.ReadInt32());
                            break;
                        case EArgumentType.Type:
                            gen.Emit(o, ResolveType(m_br.ReadString()));
                            break;
                        case EArgumentType.Uint16:
                            gen.Emit(o, m_br.ReadUInt16());
                            break;
                        case EArgumentType.Uint32:
                            gen.Emit(o, m_br.ReadUInt32());
                            break;
                        case EArgumentType.Uint8:
                            gen.Emit(o, m_br.ReadByte());
                            break;
                    }
                }
            }
        }

        private OpCode Translate(EOpCode opcode)
        {
            var t = typeof(OpCodes);
            var fi = t.GetField(opcode.ToString(), BindingFlags.Static | BindingFlags.Public);
            
            if (fi == null)
                return default(OpCode);
            else
                return (OpCode)fi.GetValue(null);
        }

        private OpCode Translate(EExtendedOpCode opcode)
        {
            var t = typeof(OpCodes);
            var fi = t.GetField(opcode.ToString(), BindingFlags.Static | BindingFlags.Public);

            if (fi == null)
                return default(OpCode);
            else
                return (OpCode)fi.GetValue(null);
        }

        private FieldInfo ResolveField(string name)
        {
            if (m_fields.ContainsKey(name))
                return m_fields[name];

            if (m_currentTb != null)
            {
                Type current = m_currentTb.BaseType;
                while (current != null)
                {
                    var fi = current.GetField(Names.ReverseField(name));
                    if (fi != null)
                        return fi;

                    current = current.BaseType;
                }
            }

            return null;
        }

        private dynamic ResolveMethod(string name)
        {
            if (m_methods.ContainsKey(name))
                return m_methods[name];
            if (m_ctors.ContainsKey(name))
                return m_ctors[name];

            if (m_currentTb != null)
            {
                Type current = m_currentTb.BaseType;
                while (current != null)
                {
                    var temp = Names.ReverseMethod(name);
                    var types = new Type[temp.Count - 1];
                    for (int i = 0; i < types.Length; i++)
                        types[i] = ResolveType(temp[i + 1]);

                    if (temp[0] == ".ctor")
                    {
                        var ci = current.GetConstructor(types);
                        if (ci != null)
                            return ci;
                    }
                    else
                    {
                        var mi = current.GetMethod(temp[0], types);
                        if (mi != null)
                            return mi;
                    }

                    current = current.BaseType;
                }
            }

            return null;
        }

        public Type ResolveType(string name)
        {
            Type ret = null;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                ret = a.GetType(name);
                if (ret != null)
                    return ret;
            }
            
            ret = m_ab.GetType(Name(name));
            if (ret != null)
                return ret;
            
            PushState();
            try
            {
                return m_parent.RequestType(name);
            }
            finally
            {
                PopState();
            }
        }

        private static string Name(string n)
        {
            return Namespace + n;
        }

        public object Instance(Type t, params object[] args)
        {
            return Activator.CreateInstance(t, args);
        }
    }
}
