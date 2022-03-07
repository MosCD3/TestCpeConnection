using System;
namespace TestCpeConnect.Models
{
    public class ServerSentCert
    {
        public string certString { get; set; }
        public string dataToSign { get; set; }
    }

    public class ServerSignResponse
    {
        public string publicKeyString { get; set; }
        public string digitalSignature { get; set; }
    }
}
