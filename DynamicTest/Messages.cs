using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicTest
{
    [Serializable]
    class RequestMessage
    {
        public string FullName { get; set; }
    }

    [Serializable]
    class ResponseMessage
    {
        public bool TypeFollowing { get; set; }
    }
}
