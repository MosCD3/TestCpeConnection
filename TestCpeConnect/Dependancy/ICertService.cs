using System;
using System.IO;
using System.Threading.Tasks;

namespace TestCpeConnect
{
    public interface ICertService
    {
        string SignRegFile(string regFile, Stream certStream, string password);
    }
}
