using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Java.Security;
using Java.Security.Cert;
using Java.Security.Spec;
using Java.Util;
using TestCpeConnect.Droid;

[assembly: Xamarin.Forms.Dependency(typeof(CertService_Android))]
namespace TestCpeConnect.Droid
{
    public class CertService_Android : ICertService
    {

        public string base64Enc(string s)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            return Convert.ToBase64String(bytes)
                .Replace("\n$", "")
                .Replace("\n", "");
        }

        public static byte[] sign(string s, IPrivateKey key)
        {
            try
            {
                byte[] bytes = Encoding.Default.GetBytes(s);
                string alg = key.Algorithm.StartsWith("EC") ? "SHA256withECDSA" : "SHA256withRSA";
                Signature sig = Signature.GetInstance(alg);
                sig.InitSign(key);
                sig.Update(bytes);
                byte[] signatureBytes = sig.Sign();
                return signatureBytes;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Exception[40]: Sign data exception:{e.Message}");
                return null;
            }
        }

        public CertService_Android()
        {
        }

        public string SignRegFile(string regFile, Stream certStream, string password)
        {
            //Use for debugging
            string fake_digital_signature = "PuSCxeMCbIHurTBqkZ6D7B1ITvuFIlsRUNKjASRkoi6-fUfi4FKbvlaqM8Ddlr7j7saLY6GwN0SfplUEwuawcF-UBoD8dsdHDwrkwicheGK4ub7XwWyiIfi-KrSQ8fftxsa7HYKNXvvOFVEijfkHYm_JnmCIPO_dVJLqVH2-4R3QSSCG25HO4-BHdhoZN_SLPbsj3jxHZsaznBf6IXBbBK7YWhbIP_vb1AOpjIwHp9x4hJZw_xGDuHBp-m0i8sWjTWCCiYn5FxGPbAz_2ZXRznv9d07QYwjRkfUXAOcC6vJjKOMmBPs_8yxf4astql2POTvhHAMNWGlk287lMfbWpg==";

            string userId = "dwiaX5";
            string sasServerInfo = "https://developer-sc-02.federatedwireless.com/v1.2";
            //Load keypair from cert
            KeyPair keyPair = LoadCert(certStream, password);
            if (keyPair == null)
            {
                Debug.WriteLine("Error[20] Cannot load cert");
                return null;
            }

            string protected_header = "{"
                    + "\"typ\":\"JWT\","
                    + "\"alg\":\"RS256\""
                    + "}";
            try
            {
                //TODO: Impliment "EC" in rest of the code
                if (keyPair.Private.Algorithm.StartsWith("EC"))
                {
                    protected_header = protected_header.Replace("RS256", "ES256");
                }

                string b64_protected_header = base64Enc(protected_header);
                string b64_cpi_signed_data = base64Enc(regFile);


                //#digitalSignature
                string data = b64_protected_header + '.' + b64_cpi_signed_data;
                byte[] bdigital_signature = sign(data, keyPair.Private);
                if (bdigital_signature == null) {
                    Debug.WriteLine("Error[79] bdigital_signature null");
                    return null;
                }

                string digital_signature = Base64.GetEncoder().EncodeToString(bdigital_signature);
                string b64u_digital_signature = digital_signature.Replace("\n", "");

                //Parsing public key
                string b64publicKeyString = Base64.GetEncoder().EncodeToString(keyPair.Public.GetEncoded());


                string publicKeyString = "";
                for (int i = 0; i <= b64publicKeyString.Length / 64; i++)
                {
                    var beginIndex = 64 * i;
                    var endIndex = 64 * i + Math.Min(64, b64publicKeyString.Length - 64 * i);
                    var subs = b64publicKeyString.Substring(beginIndex, endIndex - beginIndex);

                    publicKeyString = publicKeyString + subs + "\\n";
                }

                string cpi_public_key = publicKeyString;
                if (!publicKeyString.Contains("PUBLIC KEY"))
                {
                    cpi_public_key =
                        "-----BEGIN PUBLIC KEY-----\\n" +
                        publicKeyString +
                        "-----END PUBLIC KEY-----\\n";
                    //      cpi_public_key = cpi_public_key.replaceAll("\n", "\\" + "n");
                }


                string cpe_input_data = "{"
                    + "\"userId\": \"" + userId + "\", "
                    + "\"sasUrl\": \"" + sasServerInfo + "\", "
                    + "\"cpiSignatureData\": { "
                    + "\"protectedHeader\": \"" + b64_protected_header + "\", "
                    + "\"encodedCpiSignedData\": \"" + b64_cpi_signed_data + "\", "
                    + "\"digitalSignature\": \"" + b64u_digital_signature + "\""
                    + "}, "
                    + $"\"cpiPublicKey\": \"{cpi_public_key}\""
                    + "}";

                Debug.WriteLine("Signed File:", cpe_input_data);
                return cpe_input_data;
            }
            catch (Exception e) {
                Debug.WriteLine($"Exception[37] Droid signing reg file:{e.Message}");
            }


            return null;
        }


        public KeyPair LoadCert(Stream certStream, string password)
        {
            Debug.WriteLine("Android> Init cert from data ..");
            try
            {
                KeyStore _keyStore = KeyStore.GetInstance("PKCS12");
                _keyStore.Load(certStream, password.ToCharArray());

                string name = _keyStore.Aliases().NextElement().ToString();

                X509Certificate c = (X509Certificate)_keyStore.GetCertificate(name);


                var pubKey = c.PublicKey;
                var encoded = pubKey.GetEncoded();

                var pubKeyString = Convert.ToBase64String(encoded);
                //var st2 = pubKey.Format;
                var privKey = _keyStore.GetKey(name, password.ToCharArray());
                var privKeyBytes = privKey.GetEncoded();
                var privKeyString = Convert.ToBase64String(privKeyBytes);

                //var f = IPrivateKey()
                //Signature sig = Signature.GetInstance("SHA256withRSA");
                //sig.InitSign(privKey);

                Debug.WriteLine("===============================>");
                Debug.WriteLine("Cert Public Key:");
                Debug.WriteLine(pubKeyString);

                //Had to do this trick because stuped .NET doesn't allow a cast or init from IKey to IPrivateKey
                //TODO: Impliment "EC"
                KeyFactory kf = KeyFactory.GetInstance("RSA"); 
                IPrivateKey privateKey = kf.GeneratePrivate(new PKCS8EncodedKeySpec(privKey.GetEncoded()));

                var privateKeyBytes = privateKey.GetEncoded();
                var privateKeyString = Convert.ToBase64String(privateKeyBytes);

                Debug.WriteLine("===============================>");
                Debug.WriteLine("Cert Private Key Method2:");
                Debug.WriteLine(privateKeyString);


                return new KeyPair(pubKey, privateKey);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Exception[60] Droid load cert error:{e}");
                return null;

            }



        }
    }
}
