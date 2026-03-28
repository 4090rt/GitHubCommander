using GithubComander.src.GitHubCommander.BD;
using GithubComander.src.GitHubCommander.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace GithubComander.src.GitHubCommander.Infrastructure.EthernetStat
{
    public class HttpRequestEthernet
    {
        private readonly IMemoryCache _memorycache;
        private readonly ILogger<HttpRequestEthernet> _logger;
        private readonly ILogger<GitParser1> _loggerPars;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SaveLogInBd _saveLogInBd;
        private readonly GitParser1 _gitParser1;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1,1);

        public HttpRequestEthernet(
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

        public async Task<List<ParserEthernet>> RequestCache(CancellationToken cancellation = default)
        {
            string cachekey = "cache_key_Ethernet";
            string cachestale = $"stale{cachekey}";
            List<ParserEthernet> oldcache = null;
            if (_memorycache.TryGetValue(cachekey, out List<ParserEthernet> cached))
            {

                oldcache = cached;
                return cached;
            }
            await _semaphore.WaitAsync(cancellation);
            try
            {
                if (_memorycache.TryGetValue(cachekey, out List<ParserEthernet> cached2))
                {
                    return cached2;
                }

                var fallbackPolitics = Policy<List<ParserEthernet>>
                    .Handle<Exception>()
                    .OrResult(r => r == null)
                    .FallbackAsync(
                    fallbackAction: async (outcome, context, ctx) =>
                    {
                        var exception = outcome.Exception;
                        var isEmpty = outcome.Result == null;

                        if (exception != null)
                        {
                            _logger.LogWarning($"⚠️ Fallback by exception: {exception.Message}");
                        }
                        else if (isEmpty)
                        {
                            _logger.LogWarning($"⚠️ Fallback by empty result");
                        }
                        else if (oldcache != null)
                        {
                            _logger.LogInformation("✅ Fallback: возвращаю старые данные из кэша");
                            return oldcache;
                        }

                        if (_memorycache.TryGetValue(cachestale, out List<ParserEthernet> stalecached))
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

                var fallbackresult = await fallbackPolitics.ExecuteAsync(async () =>
                {
                    var result = await Request(cancellation).ConfigureAwait(false);

                    if (result != null)
                    {
                        var options = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromMinutes(10))
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(15));

                        _memorycache.Set(cachekey, result, options);


                        var staleoptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(15));

                        _logger.LogInformation("✅ Cached fresh data for {CacheCode}", cachekey);
                        _memorycache.Set(cachestale, result, options);
                        return result;
                    }
                    else
                    {
                        _logger.LogInformation("✅ Using cached data for {CacheCode}", cachekey);
                        return default;
                    }
                });
                return fallbackresult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ВОзникло исключение");
                return new List<ParserEthernet>();
            }
            finally
            { 
                _semaphore.Release();
            }
        }

        public async Task<List<ParserEthernet>> Request(CancellationToken cancellation = default)
        {
            try
            {
                var cliewnt = _httpClientFactory.CreateClient("EthernetApiClient");

                var clientoptions = new HttpRequestMessage(HttpMethod.Get, "https://ipinfo.io/json")
                {
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
                };

                string log = "Начинаю выполнение запроса";
                _logger.LogInformation(log);
                await _saveLogInBd.Saved(log, DateTime.Now);

                var timer = Stopwatch.StartNew();
                using var request = await cliewnt.SendAsync(clientoptions, cancellation).ConfigureAwait(false);
                timer.Stop();
                string log2 = $"Запрос выполнен за {timer}";
                await _saveLogInBd.Saved(log2, DateTime.Now);
                if (request.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Начинаю чтение ответа");
                    var timer2 = Stopwatch.StartNew();
                    var readresult = await request.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    timer2.Stop();
                    string log3 = $"Ответ прочитан за  {timer2}";
                    await _saveLogInBd.Saved(log3, DateTime.Now);

                    _logger.LogInformation("Начинаю парсинг ответа");
                    var timer3 = Stopwatch.StartNew();
                    var result = await _gitParser1.ParsedEthernet(readresult);
                    timer3.Stop();
                    string log4 = $"Ответ распаршен за  {timer3}";
                    await _saveLogInBd.Saved(log4, DateTime.Now);

                    if (result != null)
                    {
                        return new List<ParserEthernet> { result };
                    }
                    else
                    {
                        return new List<ParserEthernet>();
                    }
                }
                else
                {
                    string log5 = $"Возникла ошибка запрос. Статус код" + request.StatusCode;
                    await _saveLogInBd.Saved(log5, DateTime.Now);
                    return new List<ParserEthernet>();
                }
            }
            catch (TaskCanceledException ex) when (!cancellation.IsCancellationRequested)
            {
                _logger.LogError(ex, "Операция отменена");
                return new List<ParserEthernet>();
            }
            catch (TaskCanceledException ex) when (cancellation.IsCancellationRequested)
            {
                _logger.LogError(ex, "Операция отменена пользователем");
                return new List<ParserEthernet>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Возникло исключение во время запроса");
                return new List<ParserEthernet>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ВОзникло исключение");
                return new List<ParserEthernet>();
            }
        }
    }
}
