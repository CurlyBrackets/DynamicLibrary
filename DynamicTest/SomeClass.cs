using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicTest
{
    abstract class BaseClass
    {
        private int m_a, m_b;

        protected BaseClass()
        {

        }

        public void Init(int a, int b)
        {
            m_a = a;
            m_b = b;
        }

        public int BaseWork()
        {
            int val = m_a * m_b;
            for (int i = 0; i < m_a; i++)
                val += Work();

            return val;
        }

        public abstract int Work();
    }

    class SomeClass : BaseClass
    {
        private int m_value, m_iter;

        public BaseClass Comp { get; set; }
        public BaseClass GetOnly {
            get
            {
                return Comp;
            }
        }
        public BaseClass SetOnly
        {
            set
            {
                Comp = value;
            }
        }

        public SomeClass(int val)
        {
            m_value = val;
            m_iter = 1;
        }

        public override int Work()
        {
            return m_value * m_iter++;
        }
    }
}
