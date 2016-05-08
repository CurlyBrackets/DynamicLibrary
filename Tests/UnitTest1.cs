using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DynamicTest;
using System.IO;

namespace Tests
{
    [TestClass]
    public class ObjectSerializationTests
    {
        private ObjectSerializer input;
        private ObjectDeserializer output;
        private Stream stream;

        [TestInitialize]
        public void Initialize()
        {
            stream = new MemoryStream(1024);
            input = new ObjectSerializer(stream);
            output = new ObjectDeserializer(stream);
        }

        [TestCleanup]
        public void Cleanup()
        {
            stream.Dispose();
            input = null;
            output = null;
        }

        private void TestCommon(Func<ITestObject> generator)
        {
            stream.Seek(0, SeekOrigin.Begin);
            var obj = generator();
            input.Write(obj);
            stream.Seek(0, SeekOrigin.Begin);
            var obj2 = output.Read();

            TestResult(obj, (ITestObject)obj2);
        }

        private void TestResult(ITestObject src, ITestObject dest)
        {
            if (src == null)
                Assert.IsNull(dest);
            else
            {
                Assert.IsNotNull(dest);
                Assert.IsInstanceOfType(dest, src.GetType());
                Assert.AreEqual(src.Test(), dest.Test());
            }
        }

        [TestMethod]
        public void TestSimple()
        {
            TestCommon(() =>
                {
                    return new Simple();
                }
            );
        }

        [TestMethod]
        public void TestNull()
        {
            TestCommon(() =>
            {
                return null;
            });
        }

        [TestMethod]
        public void TestProperty()
        {
            TestCommon(() =>
            {
                return new Property() { Prop = 77 };
            });
        }

        [TestMethod]
        public void TestField()
        {
            TestCommon(() =>
            {
                return new Field() { F = 88 };
            });
        }

        [TestMethod]
        public void TestProperties()
        {
            TestCommon(() =>
            {
                return new Properties() { Prop1 = 77, Prop2 = 99 };
            });
        }

        [TestMethod]
        public void TestFields()
        {
            TestCommon(() =>
            {
                return new Fields() { F1 = 88, F2 = 111 };
            });
        }

        [TestMethod]
        public void TestFunctionPointer()
        {
            TestCommon(() =>
            {
                return new Func(Something.Value);
            });
        }

        [TestMethod]
        public void TestLambda()
        {
            TestCommon(() =>
            {
                return new Action() { m_action = (v, cb) => { Something.Action(v, cb); } };
            });
        }

        [TestMethod]
        public void TestNestedObject()
        {
            TestCommon(() =>
            {
                return new Nested() { s = new Simple() };
            });

            TestCommon(() =>
            {
                return new Nested() { s = new Field() { F = 55 } };
            });
        }

        [TestMethod]
        public void TestAnonymousObject()
        {
            TestCommon(() =>
            {
                return new Anonymous() { o = new { Val = 666 } };
            });
        }

        [TestMethod]
        public void TestStruct()
        {
            TestCommon(() =>
            {
                return new Struct() { F = 777 };
            });
        }

        [TestMethod]
        public void TestNoDefaultCtor()
        {
            TestCommon(() =>
            {
                return new NoDefault(5);
            });
        }

        [TestMethod]
        public void TestInheritance()
        {
            TestCommon(() =>
            {
                return new Subclass() { F = 3 };
            });
        }
    }

    #region Test Classes
    static class Constants
    {
        public const int Prime = 6731;
    }

    interface ITestObject
    {
        int Test();
    }

    class Simple : ITestObject
    {
        // no fields or attributes

        public int Test()
        {
            return 5;
        }
    }

    class Property : ITestObject
    {
        public int Prop { get; set; }

        public int Test()
        {
            return Prop;
        }
    }

    class Field : ITestObject
    {
        public int F;

        public virtual int Test()
        {
            return F;
        }
    }

    class Properties : ITestObject
    {
        public int Prop1 { get; set; }
        public int Prop2 { get; set; }

        public int Test()
        {
            return Prop1 * Constants.Prime + Prop2;
        }
    }

    class Fields : ITestObject
    {
        public int F1, F2;

        public int Test()
        {
            return F1 * Constants.Prime + F2;
        }
    }

    static class Something
    {
        public static int Value()
        {
            return 5555;
        }

        public static void Action(int v, Action<int> cb)
        {
            cb(v * Value());
        }
    }

    class Func : ITestObject
    {
        private Func<int> m_func;

        public Func(Func<int> f)
        {
            m_func = f;
        }

        public int Test()
        {
            return m_func();
        }
    }

    class Action : ITestObject
    {
        public Action<int, Action<int>> m_action;
        private int f;

        private void Callback(int i)
        {
            f = i;
        }

        public int Test()
        {
            m_action(5, Callback);
            return f;
        }
    }

    class Nested : ITestObject
    {
        public ITestObject s;

        public int Test()
        {
            return s.Test() * Constants.Prime + s.Test();
        }
    }

    class Anonymous : ITestObject
    {
        public object o;

        public int Test()
        {
            var val = (int)o.GetType().GetProperty("Val").GetValue(o);
            return val;
        }
    }

    struct Struct : ITestObject
    {
        public int F;

        public int Test()
        {
            return F;
        }
    }

    class NoDefault : ITestObject
    {
        private int m_f;

        public NoDefault(int val)
        {
            m_f = val;
        }

        public int Test()
        {
            return m_f * m_f;
        }
    }

    class Subclass : Field
    {

        public override int Test()
        {
            return base.Test() * 69;
        }
    }

    #endregion
}
