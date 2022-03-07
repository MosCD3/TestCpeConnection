using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Flurl.Http;
using Flurl.Http.Configuration;
using Newtonsoft.Json;
using Plugin.FileUploader;
using Plugin.FileUploader.Abstractions;
using TestCpeConnect.Models;
using Xamarin.Forms;

namespace TestCpeConnect
{

    public class UntrustedCertClientFactory : DefaultHttpClientFactory
    {
        public override HttpMessageHandler CreateMessageHandler()
        {
            return new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; }
            };
        }
    }

    public class ApiService
    {
        public string DemoCertFile = "gio_cert.p12";//Embeded
        public string DemoCertPass = "kHqnjs17VfCKjGBTBX1pPN";
        public string CPE_BASEURL_LAB = "https://192.168.37.1:15012"; // Razi's in lab
        public string CPE_Endpoint_UploadCbrs = "/cgi-bin/cbrs_upload.cgi";
        public string SampleRegFile = "{\"cbsdSerialNumber\":\"80029C397978\",\"fccId\":\"ROR1001\",\"installationParam\":{\"antennaAzimuth\":208,\"antennaBeamwidth\":23,\"antennaDowntilt\":0,\"antennaGain\":16,\"antennaModel\":\"Internal\",\"eirpCapability\":38,\"height\":4.571776897287412,\"heightType\":\"AGL\",\"horizontalAccuracy\":5,\"indoorDeployment\":false,\"latitude\":37.421998333333335,\"longitude\":-122.084,\"verticalAccuracy\":1},\"professionalInstallerData\":{\"cpiId\":\"e240c1b5-9588-40d6-89a6-ec30c0282919\",\"cpiName\":\"Gio\",\"installCertificationTime\":\"2022-02-28T22:24:06.809-05:00\"}}";
        public string SASUrl = "https://developer-sc-02.federatedwireless.com/v1.2";
        public string CpiID = "dwiaX5";

        public ApiService()
        {
            FlurlHttp.ConfigureClient("https://192.168.37.1:15012/cgi-bin/cbrs_upload.cgi", cli => cli.Settings.HttpClientFactory = new UntrustedCertClientFactory());
        }

        void err(string err) {
            Debug.WriteLine(err);
        }

        //Replace with UploadRegFile_HttpClient to test different client
        public async Task<string> UploadRegFile() {

           

            string signedFile = null;

            //Preparing stream
            if (Device.RuntimePlatform == Device.Android)
            {
                //Loading gio_cert.p12
                CpiCert certObject = LoadLocalCert(isReturnStream: true);
                if (certObject == null)
                {
                    var err = "Error[53] certObject undefined!";
                    Debug.WriteLine(err);
                    return err;

                }

                var certService = DependencyService.Get<ICertService>();
                if (certService == null)
                {
                    var err = "Error[48] Cannot init platform specific cert service";
                    Debug.WriteLine(err);
                    return err;
                }

                signedFile = certService.SignRegFile(
                    regFile: SampleRegFile,
                    certStream: certObject.FileStream,
                    password: certObject.Password,
                    userId: CpiID,
                    sasServerInfo: SASUrl);
            }
            else {

                //Loading gio_cert.p12
                CpiCert certObject = LoadLocalCert(isReturnStream: false);
                if (certObject == null || certObject?.Data == null)
                {
                    var err = "Error[90] certObject undefined!";
                    Debug.WriteLine(err);
                    return err;

                }

                signedFile = await SignRegFileShared2(
                    regFile: SampleRegFile,
                    certData: certObject.Data,
                    password: certObject.Password,
                    userId: CpiID,
                    sasServerInfo: SASUrl);
            }
           

            if (signedFile == null) {
                var err = "Error[70] Signed file undefined!";
                Debug.WriteLine(err);
                return err;
            }

            string mimebound = "----------------------------4ebf00fbcf09";
            string newBody = "--" + mimebound + "\n"
                + "Content-Disposition: form-data; name=\"file\"; filename=\"pointlinq.txt\"\n"
                + "Content-Type: text/plain\n\n"
                + signedFile
                + "\n"
                + "--" + mimebound + "--\n";

            return await UploadRegFile_HttpClient(newBody);
        }
        



        /// <summary>
        /// Using HttpClient
        /// </summary>
        /// <returns></returns>

        public async Task<string> UploadRegFile_HttpClient(string postBody)
        {
            Debug.WriteLine("Upload reg file");

            try
            {
                var uri = CPE_BASEURL_LAB + CPE_Endpoint_UploadCbrs;

                //Load reg file, this file is tested and works using curl and postman
                //string body = LoadFakeSignedRegFile();
                string body = postBody;

                HttpClientHandler handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
                var httpClient = new HttpClient(new HttpCustomHandler(handler));

                //Content
                //var content = new MultipartFormDataContent(); //could add mimebound here
                //var streamContent = new StringContent(body, Encoding.ASCII);
                //content.Add(streamContent, "file", "pointlinq.txt");

                // don't know why but this is needed...
                if (!body.Contains("4ebf00fbcf09\r\n"))
                    body = Regex.Replace(body, "\n", "\r\n");

                var content = new StringContent(body);
                content.Headers.Remove("Content-Type");
                content.Headers.Add("Content-Type", "multipart/form-data; boundary=----------------------------4ebf00fbcf09");
                content.Headers.Add("Content-Length", "" + body.Length);

                //Uploading the file
                var response = await httpClient.PostAsync(uri, content);
                var responseDataString = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"Response IS:{responseDataString}");
                return responseDataString;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Exception:{e.Message}");
                return e.Message;
            }


        }

        public async Task<ServerSignResponse> ParseCertificate_HttpClient(string base64Cert, string toSign)
        {
            Debug.WriteLine("Upload reg file");

            try
            {
                var uri = "http://localhost:8001";

                //Load reg file, this file is tested and works using curl and postman
                //string body = LoadFakeSignedRegFile();
                string serialized = JsonConvert.SerializeObject(new ServerSentCert {
                    certString = base64Cert,
                    dataToSign = toSign
                });

                HttpClientHandler handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
                var httpClient = new HttpClient(new HttpCustomHandler(handler));
                var response = await httpClient.PostAsync(uri, new StringContent(serialized, Encoding.UTF8, "application/json"));
                var responseDataString = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"Response IS:{responseDataString}");
                var responseObject = JsonConvert.DeserializeObject<ServerSignResponse>(responseDataString);
                return responseObject;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Exception[210]:{e.Message}");
                return null;
            }


        }

        public string LoadFakeSignedRegFile()
        {
            var fileName = "filem.txt";

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resName = assembly.GetManifestResourceNames()?.FirstOrDefault(r => r.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

                using (var stream = assembly.GetManifestResourceStream(resName))
                {
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        return reader.ReadToEnd();

                    }

                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"FakeSignedRegFile> Exception:{e.Message}");
                return null;
            }

        }



        public CpiCert LoadLocalCert(bool isReturnStream)
        {
            var fileName = DemoCertFile;
            var password = DemoCertPass;

            try
            {
                Debug.WriteLine($"~~ FakeCertService(LoadLocalCert) from local file: {fileName} ~~~");
                // Get the assembly this code is executing in
                var assembly = Assembly.GetExecutingAssembly();
                var resName = assembly.GetManifestResourceNames()?.FirstOrDefault(r => r.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
                Debug.WriteLine($">>resName:{resName}");


                if (isReturnStream) {
                    return new CpiCert
                    {
                        ID = "cpicert",
                        Password = password,
                        FileName = fileName,
                        FileStream = assembly.GetManifestResourceStream(resName)
                    };
                }

                byte[] buffer;
                //Never use stream twice
                using (Stream s = assembly.GetManifestResourceStream(resName))
                {
                    long length = s.Length;
                    buffer = new byte[length];
                    s.Read(buffer, 0, (int)length);
                }

                return new CpiCert
                {
                    ID = "cpicert",
                    Password = password,
                    Data = buffer,
                    FileName = fileName
                };

            }
            catch (Exception e)
            {
                Debug.WriteLine($"LoadLocalCert> Exception:{e.Message}");
                return null;
            }
        }


        /// <summary>
        /// Shared imp for sign
        /// </summary>
        /// <param name="regFile"></param>
        /// <param name="certStream"></param>
        /// <param name="password"></param>
        /// <param name="userId"></param>
        /// <param name="sasServerInfo"></param>
        /// <returns></returns>
        private string SignRegFileShared(string regFile, byte[] certData, string password, string userId, string sasServerInfo) {
            string returnedSignedFile = null;
            string protected_header = ApiService.protected_header;

            try
            {
                X509Certificate2 x509 = new X509Certificate2(rawData: certData, password: password);

                if (x509 == null)
                {
                    Debug.WriteLine("Certficate cannot be created with given password!");
                    return returnedSignedFile;
                }
                var publicKeyData = x509.GetPublicKey();
                var privateKey = x509.PrivateKey;
                var algorithm = privateKey.KeyExchangeAlgorithm;

                

                //Prepare header
                if (algorithm.StartsWith("EC"))
                {
                    protected_header = protected_header.Replace("RS256", "ES256");
                }

                string b64_protected_header = ApiService.base64Enc(protected_header);
                string b64_cpi_signed_data = ApiService.base64Enc(regFile);
                string data = b64_protected_header + '.' + b64_cpi_signed_data;

                byte[] buffer = Encoding.Default.GetBytes(data);

                //RSA rsa = x509.GetRSAPrivateKey();
                //byte[] signatureData = rsa.SignData(buffer, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                RSACryptoServiceProvider rSA = privateKey as RSACryptoServiceProvider;
                //byte[] privateKeyData = x509.PrivateKey.
                byte[] signatureData = (privateKey as RSACryptoServiceProvider).SignData(buffer, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);


                string digital_signature = Convert.ToBase64String(signatureData);
                string b64u_digital_signature = digital_signature.Replace("\n", "");

                //Public key
                string b64publicKeyString = Convert.ToBase64String(publicKeyData);

                Debug.WriteLine("Public key:");
                Debug.WriteLine(b64publicKeyString);

                Debug.WriteLine("Private key:");
                Debug.WriteLine(b64publicKeyString);

                string cpi_public_key = ApiService.FormatPublicKey(b64publicKeyString);
                string cpe_input_data = ApiService.FormatPostBody(
                                 userId: userId,
                                 sasServerInfo: sasServerInfo,
                                 b64_protected_header: b64_protected_header,
                                 b64_cpi_signed_data: b64_cpi_signed_data,
                                 b64u_digital_signature: b64u_digital_signature,
                                 cpi_public_key: cpi_public_key);

                return cpe_input_data;
            }
            catch (Exception e) {
                Debug.WriteLine($"Exception[278]:{e.Message}");
                return returnedSignedFile;
            }

        }

        private async Task<string> SignRegFileShared2(string regFile, byte[] certData, string password, string userId, string sasServerInfo)
        {
            string returnedSignedFile = null;
            string protected_header = ApiService.protected_header;

            try
            {
                var certString = Convert.ToBase64String(certData);
                string b64_protected_header = ApiService.base64Enc(protected_header);
                string b64_cpi_signed_data = ApiService.base64Enc(regFile);
                string data = b64_protected_header + '.' + b64_cpi_signed_data;

                var signedData = await ParseCertificate_HttpClient(base64Cert: certString, toSign: data);
                if (signedData == null) {
                    Debug.WriteLine("Aborting");
                    return null;
                }

                //Parsing public key
                string b64publicKeyString = signedData.publicKeyString;
                string b64u_digital_signature = signedData.digitalSignature;

                string cpi_public_key = ApiService.FormatPublicKey(b64publicKeyString);
                string cpe_input_data = ApiService.FormatPostBody(
                                 userId: userId,
                                 sasServerInfo: sasServerInfo,
                                 b64_protected_header: b64_protected_header,
                                 b64_cpi_signed_data: b64_cpi_signed_data,
                                 b64u_digital_signature: b64u_digital_signature,
                                 cpi_public_key: cpi_public_key);

                Debug.WriteLine("Signed File:", cpe_input_data);
                return cpe_input_data;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Exception[278]:{e.Message}");
                return returnedSignedFile;
            }

        }



        //Helpers
        public static string FormatPublicKey(string publicKeyB64) {

            string cpi_public_key = "";
            string publicKeyString = "";

            for (int i = 0; i <= publicKeyB64.Length / 64; i++)
            {
                var beginIndex = 64 * i;
                var endIndex = 64 * i + Math.Min(64, publicKeyB64.Length - 64 * i);
                var subs = publicKeyB64.Substring(beginIndex, endIndex - beginIndex);

                publicKeyString = publicKeyString + subs + "\\n";
            }

            if (!publicKeyString.Contains("PUBLIC KEY"))
            {
                cpi_public_key =
                    "-----BEGIN PUBLIC KEY-----\\n" +
                    publicKeyString +
                    "-----END PUBLIC KEY-----\\n";
            }

            return cpi_public_key;
        }

        public static string FormatPostBody(
            string userId,
            string sasServerInfo,
            string b64_protected_header,
            string b64_cpi_signed_data,
            string b64u_digital_signature,
            string cpi_public_key) => "{"
                    + "\"userId\": \"" + userId + "\", "
                    + "\"sasUrl\": \"" + sasServerInfo + "\", "
                    + "\"cpiSignatureData\": { "
                    + "\"protectedHeader\": \"" + b64_protected_header + "\", "
                    + "\"encodedCpiSignedData\": \"" + b64_cpi_signed_data + "\", "
                    + "\"digitalSignature\": \"" + b64u_digital_signature + "\""
                    + "}, "
                    + $"\"cpiPublicKey\": \"{cpi_public_key}\""
                    + "}";
        public static string protected_header = "{"
                    + "\"typ\":\"JWT\","
                    + "\"alg\":\"RS256\""
                    + "}";


        public static string base64Enc(string s)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            return Convert.ToBase64String(bytes)
                .Replace("\n$", "")
                .Replace("\n", "");
        }

    }
}
