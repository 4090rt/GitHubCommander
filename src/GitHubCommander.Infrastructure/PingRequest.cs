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

namespace GithubComander.src.GitHubCommander.Infrastructure
{
    public class PingRequest
    {
        private readonly IMemoryCache _memorycache;
        private readonly ILogger<HttpRequestEthernet> _logger;
        private readonly ILogger<GitParser1> _loggerPars;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SaveLogInBd _saveLogInBd;
        private readonly GitParser1 _gitParser1;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public PingRequest(
            IHttpClientFactory httpClientFactory,
            IMemoryCache memoryCache,
            ILogger<HttpRequestEthernet> logger,
            ILogger<GitParser1> loggerPars,
            SaveLogInBd saveLogInBd,
            GitParser1 gitParser1)
        {
            _httpClientFactory = httpClientFactory;
            _memorycache = memoryCache;
            _logger = logger;
            _loggerPars = loggerPars;
            _saveLogInBd = saveLogInBd;
            _gitParser1 = gitParser1;
        }

        public async Task<List<PingResult>> CacheRequest(string host, CancellationToken cancellation = default)
        {
            string key_cache = $"cachekey{host}";
            string stalekey_cache = $"stale{key_cache}";
            List<PingResult> oldcache = null;

            if (_memorycache.TryGetValue(key_cache, out List<PingResult> cached))
            {
                oldcache = cached;
                return cached;
            }
            await _semaphore.WaitAsync(cancellation);
            try
            {
                if (_memorycache.TryGetValue(key_cache, out List<PingResult> cached2))
                {
                    return cached2;
                }

                var fallback = Policy<List<PingResult>>
                    .Handle<Exception>()
                    .OrResult(r => r == null)
                    .FallbackAsync(
                    fallbackAction: async (outcome, context, ctx) =>
                    {
                        var exception = outcome.Exception;
                        var IsEmpty = outcome.Result == null;

                        if (exception != null)
                        {
                            _logger.LogWarning($"⚠️ Fallback by exception: {exception.Message}");
                        }
                        if (IsEmpty)
                        {
                            _logger.LogWarning($"⚠️ Fallback by empty result");
                        }
                        if (oldcache != null)
                        {
                            _logger.LogInformation("✅ Fallback: возвращаю старые данные из кэша");
                            return oldcache;
                        }

                        if (_memorycache.TryGetValue(stalekey_cache, out List<PingResult> stalecached))
                        {
                            _logger.LogInformation($"✅ Returning stale copy for {stalecached}");
                            return stalecached;
                        }
                        else
                        {
                            _logger.LogWarning("⚠️ Fallback: кэш пуст, возвращаю default");
                            return default;
                        }
                    },
                    onFallbackAsync: async (outcome, ctx) =>
                    {
                        _logger.LogError($"🆘 Fallback сработал: {outcome.Exception?.Message}");
                        await Task.CompletedTask;
                    });

                var fallbackresult = await fallback.ExecuteAsync(async () =>
                {
                    try
                    {
                        var result = await Request(host, cancellation).ConfigureAwait(false);

                        if (result != null)
                        {
                            var options = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(TimeSpan.FromMinutes(15))
                            .SetSlidingExpiration(TimeSpan.FromMinutes(10));

                            _memorycache.Set(key_cache, result, options);

                            var staleoptions = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(TimeSpan.FromMinutes(15));

                            _logger.LogInformation("✅ Cached fresh data for {CacheCode}", key_cache);
                            _memorycache.Set(stalekey_cache, result, options);
                            return result;
                        }
                        else
                        {
                            return default;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation("✅ Using cached data for {CacheCode}", key_cache);
                        return default;
                    }
                });
                return fallbackresult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ВОзникло исключение");
                return new List<PingResult>();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<List<PingResult>> Request(string host, CancellationToken cancellation = default)
        {
            HttpResponseMessage response = null;
            try
            {
                var client = _httpClientFactory.CreateClient("EthernetApiClient");

                // Запрос к speedtest.librespeed.org для замера пинга
                // Используем пустой запрос для замера времени отклика
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
