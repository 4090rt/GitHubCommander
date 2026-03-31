using GithubComander.src.GitHubCommander.BD;
using GithubComander.src.GitHubCommander.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace GithubComander.src.GitHubCommander.Infrastructure.EthernetStat
{
    public class PingRequestUS
    {
        private readonly ILogger<PingRequestUS> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public PingRequestUS(
            IHttpClientFactory httpClientFactory,
            ILogger<PingRequestUS> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<PingResult> Request(string host, CancellationToken cancellation = default)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("EthernetApiClient");

                var options = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/zen")
                {
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
                };

                var timer = System.Diagnostics.Stopwatch.StartNew();

                using HttpResponseMessage response = await client.SendAsync(options, cancellation).ConfigureAwait(false);
                timer.Stop();
                if (response.IsSuccessStatusCode)
                {
                    var ping = timer.ElapsedMilliseconds / 2;

                    return new PingResult
                    {
                        Host = host,
                        PingMs = ping,
                        Error = null,
                        Status = "success"
                    };
                }
                else
                {
                    _logger.LogInformation("Ошибка запроса: {StatusCode}", response.StatusCode);
                    return new PingResult
                    {
                        Host = host,
                        PingMs = 0,
                        Error = response.StatusCode.ToString(),
                        Status = "error"
                    };
                }
            }
            catch (TaskCanceledException ex) when (!cancellation.IsCancellationRequested)
            {
                _logger.LogError(ex, "Операция отменена ");

                return new PingResult
                {
                    Host = host,
                    PingMs = 0,
                    Error = ex.Message + ex.StackTrace,
                    Status = "error"
                };
            }
            catch (TaskCanceledException ex) when (cancellation.IsCancellationRequested)
            {
                _logger.LogError(ex, "Операция отменена пользователем");

                return new PingResult
                {
                    Host = host,
                    PingMs = 0,
                    Error = ex.Message + ex.StackTrace,
                    Status = "error"
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Возникло исключение  при запросе");

                return new PingResult
                {
                    Host = host,
                    PingMs = 0,
                    Error = ex.Message + ex.StackTrace,
                    Status = "error"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Возникло исключение");

                return new PingResult
                {
                    Host = host,
                    PingMs = 0,
                    Error = ex.Message + ex.StackTrace,
                    Status = "error"
                };
            }
        }
    }
}
