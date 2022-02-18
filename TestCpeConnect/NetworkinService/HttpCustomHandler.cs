using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TestCpeConnect
{
    public class HttpCustomHandler : DelegatingHandler
    {
        public HttpCustomHandler(HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Debug.WriteLine($"[HttpClient]Request:{request}");
            if (request.Content != null)
            {
                Debug.WriteLine(await request.Content.ReadAsStringAsync());
            }
            Debug.WriteLine("");

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            Debug.WriteLine($"[HttpClient]Response:{response}");
            if (response.Content != null)
            {
                Debug.WriteLine(await response.Content.ReadAsStringAsync());

            }
            Debug.WriteLine("");

            return response;
        }


    }
}
