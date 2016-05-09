using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamic
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

    [Serializable]
    class RunMessage
    {
        public bool IsStatic { get; set; }
        public int NumArguments { get; set; }
        public int NumCalls { get; set; }
        public string MethodName { get; set; }
    }

    [Serializable]
    class RunResultMessage
    {
        public bool Success { get; set; }
        public int NumResults { get; set; }
    }
}
