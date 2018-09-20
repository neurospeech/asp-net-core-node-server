using System;
using System.Linq;
using System.Net.Http;

namespace GitNpmRegistry
{
    public interface IHttpClientProvider
    {
        HttpClient HttpClient { get; }
    }

    public class HttpClientProvider : IHttpClientProvider
    {
        private HttpClient client;
        public HttpClient HttpClient => client ?? (client = new HttpClient());
    }
}
