using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicTest
{
    class ObjectDeserializer
    {
        private Stream m_core;

        public ObjectDeserializer(Stream core)
        {
            m_core = core;
        }

        public object Read()
        {
            return null;
        }
    }
}
