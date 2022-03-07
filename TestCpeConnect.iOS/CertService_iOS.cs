using System;
using System.IO;
using Foundation;
using Security;

namespace TestCpeConnect.iOS
{
    public class CertService_iOS: ICertService
    {
        public CertService_iOS()
        {
        }

        public string SignRegFile(string regFile, Stream certStream, string password, string userId, string sasServerInfo)
        {

            

            var options = NSDictionary.FromObjectAndKey(NSObject.FromObject("kHqnjs17VfCKjGBTBX1pPN"), SecImportExport.Passphrase);
            NSDictionary[] result;
            SecStatusCode statusCode = SecImportExport.ImportPkcs12(StreamToBytes(certStream), options, out result);
            var identity = result[0][SecImportExport.Identity];
            var trust = result[0][SecImportExport.Trust];

            var st = new SecTrust(result[0].LowlevelObjectForKey(SecImportExport.Trust.Handle));
            var pubKey = st.GetPublicKey();
            var pubKeyBytes = pubKey.GetExternalRepresentation().ToArray();
            string decoded = Convert.ToBase64String(pubKeyBytes);

            return null;
        }

        byte[] StreamToBytes(Stream stream) {
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}
