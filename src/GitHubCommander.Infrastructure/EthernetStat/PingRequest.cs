using GithubComander.src.GitHubCommander.BD;
using GithubComander.src.GitHubCommander.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace GithubComander.src.GitHubCommander.Infrastructure.EthernetStat
{
    public class PingRequest
    {
        private readonly ILogger<HttpRequestEthernet> _logger;
        private readonly ILogger<GitParser1> _loggerPars;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SaveLogInBd _saveLogInBd;
        private readonly GitParser1 _gitParser1;

        public PingRequest(
            IHttpClientFactory httpClientFactory,
            ILogger<HttpRequestEthernet> logger,
            ILogger<GitParser1> loggerPars,
            SaveLogInBd saveLogInBd,
            GitParser1 gitParser1)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _loggerPars = loggerPars;
            _saveLogInBd = saveLogInBd;
            _gitParser1 = gitParser1;
        }



        public async Task<List<PingResult>> Request(string host, CancellationToken cancellation = default)
        {
            HttpResponseMessage response = null;
            try
            {
                var client = _httpClientFactory.CreateClient("EthernetApiClient");

                var options = new HttpRequestMessage(HttpMethod.Get, "https://www.google.com/generate_204")
                {
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
                };

                _logger.LogInformation("Начинаю замер пинга до Google (generate_204)");
                var timer = System.Diagnostics.Stopwatch.StartNew();
                response = await client.SendAsync(options, cancellation).ConfigureAwait(false);
                timer.Stop();

                if (response.IsSuccessStatusCode)
                {
                    var pingms = timer.ElapsedMilliseconds / 2;

                    return new List<PingResult>
                    {
                        new PingResult
                        {
                            Host = host,
                            PingMs = pingms,
                            Status = "success",
                            Error = null
                        }
                    };
                }
                else
                {
                    return new List<PingResult>
                    {
                        new PingResult
                        {
                            Host = host,
                            PingMs = 0,
                            Status = "error",
                            Error = $"HTTP {response.StatusCode}"
                        }
                    };
                }
            }
            catch (TaskCanceledException ex) when (!cancellation.IsCancellationRequested)
            {
                _logger.LogError(ex, "Операция отменена по таймауту");
                return new List<PingResult>
                {
                    new PingResult
                    {
                        Host = host,
                        PingMs = 0,
                        Status = "timeout",
                        Error = "Превышено время ожидания (10 сек)"
                    }
                };
            }
            catch (TaskCanceledException ex) when (cancellation.IsCancellationRequested)
            {
                _logger.LogError(ex, "Операция отменена пользователем");
                return new List<PingResult>
                {
                    new PingResult
                    {
                        Host = host,
                        PingMs = 0,
                        Status = "cancelled",
                        Error = "Операция отменена"
                    }
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Возникло исключение во время запроса");
                return new List<PingResult>
                {
                    new PingResult
                    {
                        Host = host,
                        PingMs = 0,
                        Status = "error",
                        Error = ex.Message
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ВОзникло исключение");
                return new List<PingResult>
                {
                    new PingResult
                    {
                        Host = host,
                        PingMs = 0,
                        Status = "error",
                        Error = ex.Message
                    }
                };
            }
            finally
            {
                response?.Dispose();
            }
        }

    }
}
