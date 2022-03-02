using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Flurl.Http;
using Flurl.Http.Configuration;
using Plugin.FileUploader;
using Plugin.FileUploader.Abstractions;
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


        public ApiService()
        {
            FlurlHttp.ConfigureClient("https://192.168.37.1:15012/cgi-bin/cbrs_upload.cgi", cli => cli.Settings.HttpClientFactory = new UntrustedCertClientFactory());
        }

        void err(string err) {
            Debug.WriteLine(err);
        }

        //Replace with UploadRegFile_HttpClient to test different client
        public Task<string> UploadRegFile() {

            //Loading gio_cert.p12
            CpiCert certObject = LoadLocalCert(isReturnStream: true);
            if (certObject == null)
            {
                var err = "Error[53] certObject undefined!";
                Debug.WriteLine(err);
                return Task.FromResult(err);

            }

            //Preparing stream
            var certService = DependencyService.Get<ICertService>();
            if (certService == null) {
                var err = "Error[48] Cannot init platform specific cert service";
                Debug.WriteLine(err);
                return Task.FromResult(err);
            }

            var signedFile = certService.SignRegFile(regFile: SampleRegFile, certStream: certObject.FileStream, password: certObject.Password);
            if (signedFile == null) {
                var err = "Error[70] Signed file undefined!";
                Debug.WriteLine(err);
                return Task.FromResult(err);
            }

            string mimebound = "----------------------------4ebf00fbcf09";
            string newBody = "--" + mimebound + "\n"
                + "Content-Disposition: form-data; name=\"file\"; filename=\"pointlinq.txt\"\n"
                + "Content-Type: text/plain\n\n"
                + signedFile
                + "\n"
                + "--" + mimebound + "--\n";

            return UploadRegFile_HttpClient(newBody);
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



    }
}
