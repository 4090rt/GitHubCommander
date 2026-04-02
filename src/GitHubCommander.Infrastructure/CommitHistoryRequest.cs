using GithubComander.src.GitHubCommander.Data;
using GithubComander.src.GitHubCommander.Infrastructure.Delegates;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GithubComander.src.GitHubCommander.Infrastructure
{
    public class CommitHistoryRequest
    {
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _memorycache;
        private readonly Microsoft.Extensions.Logging.ILogger<CommitHistoryRequest> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GitParser1 _parser;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        public CommitHistoryRequest(Microsoft.Extensions.Caching.Memory.IMemoryCache memorycache, Microsoft.Extensions.Logging.ILogger<CommitHistoryRequest> logger, IHttpClientFactory httpClientFactory, GitParser1 parser)
        {
            _httpClientFactory = httpClientFactory;
            _memorycache = memorycache;
            _logger = logger;
            _parser = parser;
        }

        public async Task<List<GitHubCommit>> RequestCache(string owner, string repo, string path = "", CancellationToken cancellation = default)
        {
            string memorycache = $"memorycachefrom{owner}{repo}commtis";
            string stalecache = $"stale{memorycache}";
            List<GitHubCommit> oldcache = null;
            if (_memorycache.TryGetValue(memorycache, out object cached) && cached is List<GitHubCommit> list)
            {
                oldcache = list;
                return list;
            }
            await _semaphore.WaitAsync(cancellation);
            try
            {
                if (_memorycache.TryGetValue(memorycache, out object cached2) && cached2 is List<GitHubCommit> list2)
                {
                    return list2;
                }

                var fallback = Policy<List<GitHubCommit>>
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
                        if (isEmpty)
                        {
                            _logger.LogWarning($"⚠️ Fallback by empty result");
                        }
                        if (oldcache != null)
                        {
                            _logger.LogInformation("✅ Fallback: возвращаю старые данные из кэша");
                            return oldcache;
                        }
                        if (_memorycache.TryGetValue(stalecache, out object stalecached) && stalecached is List<GitHubCommit> list3)
                        {
                            _logger.LogInformation($"✅ Returning stale copy for {stalecached}");
                            return list3;
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
                    var result = await Request(owner, repo, path, cancellation);

                    if (result != null && result is List<GitHubCommit>)
                    {
                        var options = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(15))
                        .SetSlidingExpiration(TimeSpan.FromMinutes(10));

                        _memorycache.Set(memorycache, result, options);

                        var staleoptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(15))
                        .SetSlidingExpiration(TimeSpan.FromMinutes(10));

                        _memorycache.Set(stalecache, result, staleoptions);
                        _logger.LogInformation("✅ Cached fresh data for {CacheCode}", memorycache);
                        return result;
                    }
                    else
                    {
                        _logger.LogInformation("✅ Using cached data for {CacheCode}", memorycache);
                        return default;
                    }
                });
                return fallbackresult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ВОзникло исключение");
                return new List<GitHubCommit>();
            }
            finally
            { 
                _semaphore.Release();
            }
        }

        public async Task<List<GitHubCommit>> Request(string owner, string repo, string path = "", CancellationToken cancellation = default)
        {
            try
            { 
                var client = _httpClientFactory.CreateClient("GithubApiClient1");

                var options = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{owner}/{repo}/ urcommits")
                {
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
                };

                _logger.LogInformation("Начинаю запрос коммитов");
                var timer = System.Diagnostics.Stopwatch.StartNew();
                using HttpResponseMessage recpon = await client.SendAsync(options, cancellation).ConfigureAwait(false);
                timer.Stop();
                _logger.LogInformation($"Ответ получен за {timer.Elapsed.Seconds}сек.");
                if (recpon.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Начинаю чтение ответа");
                    var timer2 = System.Diagnostics.Stopwatch.StartNew();
                    var readresult = await recpon.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    timer2.Stop();
                    _logger.LogInformation($"Ответ прочитан за {timer2}сек.");

                    _logger.LogInformation("Начинаю парсинг ответа");
                    var timer3 = System.Diagnostics.Stopwatch.StartNew();
                    var parsed = await _parser.ParsedCommit(readresult).ConfigureAwait(false);
                    timer3.Stop();
                    _logger.LogInformation($"Ответ распаршен за {timer3}сек.");

                    if (parsed != null)
                    {
                        return parsed;
                    }
                    else
                    {
                        _logger.LogError("Данные о коммитах не найдены или имеют невалидный формат!");
                        return new List<GitHubCommit>();
                    }
                }
                else
                {
                    _logger.LogError("Возникла ошибка запрос" + recpon.StatusCode);
                    return new List<GitHubCommit>();
                }
            }
            catch (TaskCanceledException ex) when (cancellation.IsCancellationRequested)
            {
                _logger.LogError(ex, "Операция отменена ");
                return new List<GitHubCommit>();
            }
            catch (TaskCanceledException ex) when (!cancellation.IsCancellationRequested)
            {
                _logger.LogError(ex, "Операция отменена ");
                return new List<GitHubCommit>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Возникло исключение  при запросе");
                return new List<GitHubCommit>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Возникло исключение");
                return new List<GitHubCommit>();
            }

        }
    }
}
