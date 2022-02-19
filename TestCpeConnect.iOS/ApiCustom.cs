﻿using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Foundation;
using ObjCRuntime;
using Security;
using TestCpeConnect.iOS;

[assembly: Xamarin.Forms.Dependency(typeof(ApiCustom_iOS))]
namespace TestCpeConnect.iOS
{
    public class MySessionDelegate : NSObject, INSUrlSessionDelegate
    {
        [Export("URLSession:didReceiveChallenge:completionHandler:")]
        public void DidReceiveChallenge(NSUrlSession session, NSUrlAuthenticationChallenge challenge, Action<NSUrlSessionAuthChallengeDisposition, NSUrlCredential> completionHandler)
        {
            SecTrust trust = challenge.ProtectionSpace.ServerSecTrust;
            completionHandler(NSUrlSessionAuthChallengeDisposition.UseCredential, NSUrlCredential.FromTrust(trust));
        }
    }

    public class ApiCustom_iOS: IApi
    {
        NSUrlSession session;
        public string fileToUpload;
        string url = "https://192.168.37.1:15012/cgi-bin/cbrs_upload.cgi";
        string mimebound = "----------------------------4ebf00fbcf09";
        string fieldName = "file";
        string fileName = "pointlinq.txt";
        string contentType = "text/plain";
        string bodyString = "{\"userId\": \"dwiaX5\", \"sasUrl\": \"https://developer-sc-02.federatedwireless.com/v1.2\", \"cpiSignatureData\": { \"protectedHeader\": \"eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiJ9\", \"encodedCpiSignedData\": \"eyJjYnNkU2VyaWFsTnVtYmVyIjoiODAwMjlDMzk3OTc4IiwiZmNjSWQiOiJST1IxMDAxIiwiaW5zdGFsbGF0aW9uUGFyYW0iOnsiYW50ZW5uYUF6aW11dGgiOjAsImFudGVubmFCZWFtd2lkdGgiOjIzLCJhbnRlbm5hRG93bnRpbHQiOi0xLCJhbnRlbm5hR2FpbiI6MTYsImFudGVubmFNb2RlbCI6IkludGVybmFsIiwiZWlycENhcGFiaWxpdHkiOjM4LCJoZWlnaHQiOjQuNTcxNzc2ODk3Mjg3NDEyLCJoZWlnaHRUeXBlIjoiQUdMIiwiaG9yaXpvbnRhbEFjY3VyYWN5IjoyMCwiaW5kb29yRGVwbG95bWVudCI6ZmFsc2UsImxhdGl0dWRlIjo0MS4wMDg1MTIsImxvbmdpdHVkZSI6LTkxLjE2MzQ5MiwidmVydGljYWxBY2N1cmFjeSI6MX0sInByb2Zlc3Npb25hbEluc3RhbGxlckRhdGEiOnsiY3BpSWQiOiJlMjQwYzFiNS05NTg4LTQwZDYtODlhNi1lYzMwYzAyODI5MTkiLCJjcGlOYW1lIjoicmF6IiwiaW5zdGFsbENlcnRpZmljYXRpb25UaW1lIjoiMjAyMi0wMi0xNlQxNzo1ODoxMS42OC0wNTowMCJ9fQ==\", \"digitalSignature\": \"jKBAM8b7kyBdStmKWQ03XJz5SCAsYpzrMFMMY0_hphedfCdRqpf3i80Iw-GM3e4Hnq1ACoIcM_k5wS_sdGn8fZdmxV3YbN-MAAGO76OiPCQiQ9cvBDfHvAlVhqaqpPFLXTLBL_HejD4k4y5LFnO_RK_Lx0Gzudg2v3dCD7mGu7QjU7CXJBM2KQcRYwhFEWbz9OclJhfKY4AK8YiGZWTs2QiPLPwPIroGl_fZ90LCQHdNAAY1uUg9GyvND8c87QKjAFIuXA6IG88yJtmCr5k8czMWCdtbpyULyjmWbPGRhmGS6uC5ylc7NjyxmZn2T-86nigbh_zCeg3XTq1Is_awGA==\"}, \"cpiPublicKey\": \"-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAvLPdUGCJVxF0Zt8jLGPU\nPeikrs0qjOzP2K1/8HEGw+LIyauRaoRT/6aFML6aJtN5elL1oMLsSB82MGZs1xLl\n2mzIS4+lvE2NBNl7FS+bpXGAMZZmTHdW/4DxXGT0MVUE9Jod8ajwzUNByMy5kVLc\nobuYPvFMhYJzmRD8sBGGQ8p7xSB4k4Aq28JTsCmScCxZbwm07iiX2oYDZeJNGCqZ\nYyG0ildov6Vz6apm4FgTA2HH/+CO3pcsSj9ytwVprYd3nkNIsW6u/mqyY+j56GG1\nzT+p8kiIQ9cDlQRXSMRWN4102CpFHPPXrEXRnp1TO2BzBHUSxNhO5j+SO/7ZGrIX\nsQIDAQAB\n-----END PUBLIC KEY-----\n\"}";


        public ApiCustom_iOS()
        {
            ServicePointManager.ServerCertificateValidationCallback += (s, c, k, e) => true;
        }


        NSData FromString(string value) => NSData.FromString(value, NSStringEncoding.UTF8);

        NSMutableData PrepBody() {
            var data = new NSMutableData();
            //data.AppendData(NSData.FromString(jsonSerialized, NSStringEncoding.UTF8));
            data.AppendData(FromString($"--{mimebound}\r\n"));
            data.AppendData(FromString($"Content-Disposition: form-data; name=\"{fieldName}\"; filename=\"{fileName}\"\r\n"));
            data.AppendData(FromString($"Content-Type: {contentType}\r\n\r\n"));
            data.AppendData(FromString(bodyString));
            data.AppendData(FromString("\r\n\r\n"));
            data.AppendData(FromString($"--{mimebound}--\r\n"));
            return data;
        }

        void UploadFile() {

            Debug.WriteLine("Uploading reg file");
            try {
                NSUrl downloadURL = NSUrl.FromString(url);
                NSMutableUrlRequest request = new NSMutableUrlRequest(downloadURL);
                request.HttpMethod = "POST";
                request["Content-Type"] = "multipart/form-data; boundary=" + mimebound;
                request.Body = PrepBody();

                NSUrlSessionConfiguration myConfig = NSUrlSessionConfiguration.DefaultSessionConfiguration;
                session = NSUrlSession.FromConfiguration(myConfig, new MySessionDelegate(), new NSOperationQueue());
                NSUrlSessionTask task = session.CreateDataTask(request, (prep, response, error) => {
                    Debug.WriteLine("Prep", prep);
                    Debug.WriteLine("Prep", response);
                    Debug.WriteLine("Prep", error);
                    


                });
                task.Resume();
                Debug.WriteLine("Task resumed");
                
            }
            catch(Exception e)
            {
                Debug.WriteLine($"Exception:{e.Message}");


            }
            
        }

        public void UploadRegFile()
        {
            UploadFile();
        }
    }
}
