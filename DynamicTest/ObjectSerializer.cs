using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicTest
{
    class ObjectSerializer
    {
        private Stream m_core;

        public ObjectSerializer(Stream core)
        {
            m_core = core;
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
            
        }
    }
}
