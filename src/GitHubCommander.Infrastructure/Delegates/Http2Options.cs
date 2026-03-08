using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
namespace GithubComander.src.GitHubCommander.Infrastructure.Delegates
{
    public class Http2Options
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<Http2Options> _logger;

        public delegate HttpRequestMessage Http2Delegate();
        public delegate System.Net.Http.HttpClient HttpClientDelegate(string cleintname);

        public Http2Options(IHttpClientFactory httpClientFactory, ILogger<Http2Options> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public HttpRequestMessage Delegateoptions(Func<HttpRequestMessage> action)
        { 
          var dele = action?.Invoke();
            return dele;
        }

        public System.Net.Http.HttpClient Delegateclient(Func<string, System.Net.Http.HttpClient> client, string cleintname)
        {
            var clienthttp = client?.Invoke(cleintname);
            return clienthttp;
        }

        public HttpRequestMessage OptionshTTP2()
        { 
            var options = new HttpRequestMessage(HttpMethod.Get, "user/repos")
            {
                Version = HttpVersion.Version20,
                VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
            };

            return options;
        }

        public System.Net.Http.HttpClient OptionsClient(string clientname)
        {
            try
            {
                var client = _httpClientFactory.CreateClient(clientname);

                var token1 = Environment.GetEnvironmentVariable("GITHUB_TOKEN") ??
                    throw new InvalidOperationException("GITHUB_TOKEN not set");

                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("token", token1);

                return client;
            }
            catch (Exception ex)
            {
                _logger.LogError("Возникло исключение при создании клиента" + ex.Message + ex.StackTrace);
                return null;
            }
        }
    }
}
