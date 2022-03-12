using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace TestCpeConnect
{
    public class CpiCert
    {
        public string ID { get; set; }
        public string Password { get; set; }
        public byte[] Data { get; set; }
        public Stream FileStream { get; set; }
        //public byte[] PrivateKeyData { get; set; } //only used for Android
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public X509Certificate2 Certificate { get; set; }
        public PublicKey PublicKey { get; set; }
        public RSACryptoServiceProvider PrivatKey { get; set; }
    }
}
