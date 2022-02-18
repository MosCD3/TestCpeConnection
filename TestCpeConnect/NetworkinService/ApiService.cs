using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Flurl.Http;
using Flurl.Http.Configuration;
using Plugin.FileUploader;
using Plugin.FileUploader.Abstractions;

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

        public string CPE_BASEURL_LAB = "https://192.168.37.1:15012"; // Razi's in lab
        public string CPE_Endpoint_UploadCbrs = "/cgi-bin/cbrs_upload.cgi";

        public ApiService()
        {
            FlurlHttp.ConfigureClient("https://192.168.37.1:15012/cgi-bin/cbrs_upload.cgi", cli => cli.Settings.HttpClientFactory = new UntrustedCertClientFactory());
        }


        //Replace with UploadRegFile_HttpClient to test different client
        public Task<string> UploadRegFile() => UploadRegFile_HttpClient();



        /// <summary>
        /// Using HttpClient
        /// </summary>
        /// <returns></returns>

        public async Task<string> UploadRegFile_HttpClient()
        {
            Debug.WriteLine("Upload reg file");

            try
            {
                var uri = CPE_BASEURL_LAB + CPE_Endpoint_UploadCbrs;

                //Load reg file, this file is tested and works using curl and postman
                var body = LoadFakeSignedRegFile();

                HttpClientHandler handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
                var httpClient = new HttpClient(new HttpCustomHandler(handler));

                //Content
                var content = new MultipartFormDataContent(); //could add mimebound here
                var streamContent = new StringContent(body, Encoding.UTF8);
                content.Add(streamContent);

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

        /// <summary>
        /// Using Flurl, pritty famous but still didn't work
        /// </summary>
        /// <returns></returns>
        ///
        public async Task<string> UploadRegFile_Flurl()
        {
            Debug.WriteLine("Upload reg file ");

            try
            {
                var body = LoadFakeSignedRegFile();
                var content = new MultipartFormDataContent();
                var streamContent = new StringContent(body, Encoding.UTF8);
                content.Add(streamContent, "file", "pointlinq.txt");

                //Prep file
                var fileName = "file.txt";
                var assembly = Assembly.GetExecutingAssembly();
                var resName = assembly.GetManifestResourceNames()?.FirstOrDefault(r => r.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
                var stream = assembly.GetManifestResourceStream(resName);


                //Prep client
                var uri = CPE_BASEURL_LAB + CPE_Endpoint_UploadCbrs;

                //Load reg file, this file is tested and works using curl and postman
                var resp = await uri.PostMultipartAsync(mp => mp
                                                    //.AddString("name", "hello!")                // individual string
                                                    //.AddFile("file1", path1)                    // local file path
                                                    //.AddFile("file", stream, "pointlinq.txt")        // file stream
                                                    //.AddJson("json", new { foo = "x" })         // json
                                                    //.AddUrlEncoded("urlEnc", new { bar = "y" }) // URL-encoded                      
                                                    .Add(content)
                                                    );



                //Debug.WriteLine($"resssss:{await resp.Headers}");
                Debug.WriteLine($"resssss:{await resp.GetStringAsync()}");
                return "";
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Exception:{e.Message}");
                return e.Message;
            }
        }


        /// <summary>
        /// Still testing that
        /// </summary>
        /// <returns></returns>
        ///
        public async Task<string> UploadRegFile_Kosovo()
        {
            Debug.WriteLine("Upload reg file ");

            try
            {
                var body = LoadFakeSignedRegFile();

                //Prep file
                var fileName = "file.txt";
                var assembly = Assembly.GetExecutingAssembly();
                var resName = assembly.GetManifestResourceNames()?.FirstOrDefault(r => r.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
                var stream = assembly.GetManifestResourceStream(resName);

                using (var memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    var fileBytes = memoryStream.ToArray();


                    //Prep client
                    var uri = CPE_BASEURL_LAB + CPE_Endpoint_UploadCbrs;

                    CrossFileUploader.Current.FileUploadCompleted += (sender, response) =>
                    {
                        System.Diagnostics.Debug.WriteLine($"Done:{response.StatusCode} - {response.Message}");
                    };

                    CrossFileUploader.Current.FileUploadError += (sender, response) =>
                    {
                        System.Diagnostics.Debug.WriteLine($"Error:{response.StatusCode} - {response.Message}");
                    };

                    //UploadFileAsync(string url, FileBytesItem fileItem, IDictionary<string, string> headers = null, IDictionary<string, string> parameters = null, string boundary = null);
                    var uploadResult = await CrossFileUploader.Current.UploadFileAsync(uri, new FileBytesItem("pointlinq.txt", fileBytes, "file"));

                }


                return "";
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Exception:{e.Message}");
                return e.Message;
            }
        }

        



        public string LoadFakeSignedRegFile()
        {
            var fileName = "file.txt";

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



    }
}
