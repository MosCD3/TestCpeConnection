using System;
using System.Threading;

namespace TestCpeConnect
{
    public struct GenericOpResult
    {
        public bool Error;
        public string Message;
        public object Data;
        public CancellationTokenSource operationToken;
    }
}
