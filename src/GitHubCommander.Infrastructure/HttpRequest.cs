using GithubComander.src.GitHubCommander.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GithubComander.src.GitHubCommander.Infrastructure
{
    public class HttpRequest
    {
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _memorycache;
        private readonly Microsoft.Extensions.Logging.ILogger<HttpRequest> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GitParser1 _parser;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1,1);
        private readonly FallbackPolitic _fallbackPolitic;
        public HttpRequest(Microsoft.Extensions.Caching.Memory.IMemoryCache memorycache, Microsoft.Extensions.Logging.ILogger<HttpRequest> logger, IHttpClientFactory httpClientFactory, GitParser1 parser,
            FallbackPolitic fallbackPolitic)
        {
            _httpClientFactory = httpClientFactory;
            _memorycache = memorycache;
            _logger = logger;
            _parser = parser;
            _fallbackPolitic = fallbackPolitic;
        }

        public async Task<List<DataModelRepositoryInfo>> CachingRequest(CancellationToken cancellation = default)
        {
            string cache_code = $"cachde_code_from_baseadress";

            List<DataModelRepositoryInfo>? oldCached = null;
            if (_memorycache.TryGetValue(cache_code, out object? cacheobject) && 
                cacheobject is List<DataModelRepositoryInfo> cached)
            {
                oldCached = cached;
                _logger.LogInformation($"📦 Данные из кэша для {cache_code}");
                return cached;
            }

            await _semaphore.WaitAsync(cancellation);
            try
            {
                if (_memorycache.TryGetValue(cache_code, out object? cacheobject2))
                {
                    if (cacheobject2 is List<DataModelRepositoryInfo> cached2)
                    {
                        return cached2;
                    }
                }

                var fallback = _fallbackPolitic.FallbackPolicy(_fallbackPolitic.FallbackProverka, oldCached, cache_code, cancellation);

                _logger.LogInformation("начинаю процесс получения данных");

                var result = await fallback.ExecuteAsync(async () =>
                {
                    var reuslt = await Request(cancellation).ConfigureAwait(false);

                    if (reuslt != null && reuslt.Count > 0)
                    {
                        var options = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(15))
                        .SetSlidingExpiration(TimeSpan.FromMinutes(10));

                        _memorycache.Set(cache_code, reuslt, options);

                        var StaleOptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(25));

                        _memorycache.Set($"stale: {cache_code}", reuslt, StaleOptions);
                        _logger.LogInformation($"✅ Cached fresh data for {cache_code}");
                    }
                    return reuslt ?? new List<DataModelRepositoryInfo>();
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
                return new List<DataModelRepositoryInfo>();
            }
            finally
            { 
                _semaphore.Release();
            }
        }

        public async Task<List<DataModelRepositoryInfo>> Request(CancellationToken cancellation = default)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("GithubApiClient1");

                var token1 = Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? throw new InvalidOperationException("GITHUB_TOKEN not set");
                client.DefaultRequestHeaders.Authorization =
                              new System.Net.Http.Headers.AuthenticationHeaderValue("token", token1);

                var options = new HttpRequestMessage(HttpMethod.Get, "user/repos")
                {
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
                };

                _logger.LogInformation("Начинаю запрос данных");
                var timer = System.Diagnostics.Stopwatch.StartNew();
                using HttpResponseMessage recpon = await client.SendAsync(options).ConfigureAwait(false);
                timer.Stop();
                _logger.LogInformation($"Запрос завершился за {timer}");
                if (recpon.IsSuccessStatusCode)
                {
                    if (recpon != null)
                    {
                        try
                        {
                            _logger.LogInformation("Читаю ответ");
                            var timer2 = System.Diagnostics.Stopwatch.StartNew();
                            var content = await recpon.Content.ReadAsStreamAsync().ConfigureAwait(false);
                            timer2.Stop();
                            _logger.LogInformation($"Прочитано за {timer2}");

                            _logger.LogInformation("Начинаю парсинг");
                            var timer3 = System.Diagnostics.Stopwatch.StartNew();
                            var result = await _parser.Parsed(content);
                            timer3.Stop();
                            _logger.LogInformation($"Распаршено за {timer3}");

                            return result;

                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
                            return new List<DataModelRepositoryInfo>();
                        }
                    }
                    else
                    {
                        _logger.LogError("Тело запроса не найдено");
                        return new List<DataModelRepositoryInfo>();
                    }
                }
                else
                {
                    _logger.LogError("Запрос завершился ошибкой. Статус код:" + recpon.StatusCode);
                    return new List<DataModelRepositoryInfo>();
                }

            }
            catch (TaskCanceledException ex) when (!cancellation.IsCancellationRequested)
            {
                _logger.LogError("Операция отменена" + ex.Message + ex.StackTrace);
                return new List<DataModelRepositoryInfo>();
            }
            catch (TaskCanceledException ex) when (cancellation.IsCancellationRequested)
            {
                _logger.LogError("Операция отменена пользователем" + ex.Message + ex.StackTrace);
                return new List<DataModelRepositoryInfo>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError("Возникло исключение при выполнении запроса" + ex.Message + ex.StackTrace);
                return new List<DataModelRepositoryInfo>();
            }
            catch (Exception ex)
            {
                _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
                return new List<DataModelRepositoryInfo>();
            }
        }
    }

    public class HttpRequest3
    {
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _memorycache;
        private readonly Microsoft.Extensions.Logging.ILogger<HttpRequest3> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GitParser1 _parser;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); 
        public HttpRequest3(Microsoft.Extensions.Caching.Memory.IMemoryCache memorycache, Microsoft.Extensions.Logging.ILogger<HttpRequest3> logger, IHttpClientFactory httpClientFactory, GitParser1 parser)
        {
            _httpClientFactory = httpClientFactory;
            _memorycache = memorycache;
            _logger = logger;
            _parser = parser;
        }

        public async Task<List<FileContent>> CacheRequest(string owner, string repo, string path = "", CancellationToken cancellation = default)
        {
            string key_cache = $"cached_key{owner}{repo}{path}";

            List<FileContent>? oldCached = null;
            if (_memorycache.TryGetValue(key_cache, out List<FileContent>? cached) && cached != null)
            {
                oldCached = cached;
                _logger.LogInformation($"📦 Данные из кэша для {key_cache}");
                return cached;
            }

            await _semaphore.WaitAsync(cancellation);

            try
            {
                if (_memorycache.TryGetValue(key_cache, out object? cachedobject))
                {
                    if (cachedobject is List<FileContent> cached2)
                    {
                        return cached2;
                    }
                }

                var fallbackPolicy = Policy<List<FileContent>>
                    .Handle<Exception>()
                    .OrResult(r => r == null || r.Count == 0)
                    .FallbackAsync(
                        fallbackAction: async (outcome, context, ct) =>
                        {
                            _logger.LogWarning("⚠️ Fallback: запрос не удался");

                            var exception = outcome.Exception;
                            var isEmpty = outcome.Result?.Count == 0;

                            if (exception != null)
                            {
                                _logger.LogError($"Ошибка: {exception.Message}");
                            }
                            else if (isEmpty)
                            {
                                _logger.LogWarning("Получен пустой результат");
                            }

                            if (oldCached != null)
                            {
                                _logger.LogInformation("✅ Fallback: возвращаю старые данные из кэша");
                                return oldCached;
                            }

                            string stalekey = $"stalekey:{key_cache}";

                            if (_memorycache.TryGetValue(stalekey, out object? staleonject))
                            {
                                if (staleonject is List<FileContent> cachedstale)
                                {
                                    _logger.LogInformation("✅ Fallback: возвращаю stale-копию");
                                    return cachedstale;
                                }
                            }

                            _logger.LogWarning("⚠️ Fallback: кэш пуст, возвращаю пустой список");
                            return new List<FileContent>();
                        },
                        onFallbackAsync: async (outcome, ctx) =>
                        {
                            _logger.LogError($"🆘 Fallback сработал: {outcome.Exception?.Message}");
                            await Task.CompletedTask;
                        });

                _logger.LogInformation("Начинаю запрос данных");

                var result = await fallbackPolicy.ExecuteAsync(async () =>
                {
                    var reuslt = await Request(owner, repo, path, cancellation).ConfigureAwait(false);

                    if (reuslt != null && reuslt.Count > 0)
                    {
                        var options = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(TimeSpan.FromMinutes(15))
                            .SetSlidingExpiration(TimeSpan.FromMinutes(10));

                        _memorycache.Set(key_cache, reuslt, options);

                        var staleoptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(25));

                        _memorycache.Set($"stale:{key_cache}", reuslt, staleoptions);

                        _logger.LogInformation($"✅ Данные сохранены в кэш для {key_cache}");
                    }
                    return reuslt ?? new List<FileContent>();
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
                return new List<FileContent>();
            }
            finally
            { 
                _semaphore.Release();
            }
        }

        public async Task<List<FileContent>> Request(string owner, string repo, string path = "", CancellationToken cancellation = default)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("GithubApiClient2");
                string url = string.IsNullOrEmpty(path)
                    ? $"repos/{owner}/{repo}/contents"
                    : $"repos/{owner}/{repo}/contents/{path}";
                _logger.LogInformation("Запрашиваю содержимое: {url}", url);

                var token2 = Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? throw new InvalidOperationException("GITHUB_TOKEN not set");
                client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("token", token2);

                var options = new HttpRequestMessage(HttpMethod.Get, url)
                {
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
                };

                _logger.LogInformation("Начинаю запрос");
                var timer = System.Diagnostics.Stopwatch.StartNew();
                using HttpResponseMessage recpon = await client.SendAsync(options).ConfigureAwait(false);
                timer.Stop();
                _logger.LogInformation($"Запрос завершен за {timer}");
                if (recpon.IsSuccessStatusCode)
                {
                    if (recpon != null)
                    {
                        try
                        {
                            _logger.LogInformation("Читаю ответ");
                            var timer2 = System.Diagnostics.Stopwatch.StartNew();
                            var content = await recpon.Content.ReadAsStreamAsync().ConfigureAwait(false);
                            timer2.Stop();
                            _logger.LogInformation($"Ответ прочитан за {timer2}");

                            _logger.LogInformation("Начинаю парсинг");
                            var result = await _parser.Parsed3(content);
                            _logger.LogInformation("Парсинг завершен");

                            return result != null ? new List<FileContent> { result } : new List<FileContent>();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
                            return new List<FileContent>();
                        }
                    }
                    else
                    {
                        _logger.LogError("Ответ от сервера не найден");
                        return new List<FileContent>();
                    }
                }
                else
                {
                    _logger.LogError("запрос завершился ошибкой. посткод:" + recpon.StatusCode);
                    return new List<FileContent>();
                }
            }
            catch (TaskCanceledException ex) when (!cancellation.IsCancellationRequested)
            {
                _logger.LogError("Операция отменена" + ex.Message + ex.StackTrace);
                return new List<FileContent>();
            }
            catch (TaskCanceledException ex) when (cancellation.IsCancellationRequested)
            {
                _logger.LogError("Операция отменена пользователем" + ex.Message + ex.StackTrace);
                return new List<FileContent>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError("Возниклои исключение при выполнении запроса" + ex.Message + ex.StackTrace);
                return new List<FileContent>();
            }
            catch (Exception ex)
            {
                _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
                return new List<FileContent>();
            }
        }
    }

    public class HttpRequest2
    {
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _memorycache;
        private readonly Microsoft.Extensions.Logging.ILogger<HttpRequest2> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GitParser1 _parser;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        public HttpRequest2(Microsoft.Extensions.Caching.Memory.IMemoryCache memorycache, Microsoft.Extensions.Logging.ILogger<HttpRequest2> logger, IHttpClientFactory httpClientFactory, GitParser1 parser)
        {
            _httpClientFactory = httpClientFactory;
            _memorycache = memorycache;
            _logger = logger;
            _parser = parser;
        }

        public async Task<List<RepositoryContent>> CacheRequest(string owner, string repo, string path = "", CancellationToken cancellation = default)
        {
            string key_cache = $"cached_key{owner}{repo}{path}";

            List<RepositoryContent> oldcache = null;
            if (_memorycache.TryGetValue(key_cache, out List<RepositoryContent> cached) && cached != null)
            { 
                oldcache = cached;
                _logger.LogInformation($"📦 Данные из кэша для {key_cache}");
                return cached;
            }

            await _semaphore.WaitAsync(cancellation);

            try
            {
                if (_memorycache.TryGetValue(key_cache, out object? cachedobject))
                {
                    if (cachedobject is List<RepositoryContent> cached3)
                    { 
                        return cached3;
                    }
                }

                var fallback = Policy<List<RepositoryContent>>
                    .Handle<Exception>()
                    .OrResult(r => r == null || r.Count == 0)
                    .FallbackAsync(
                    fallbackAction: async (outcome, context, ct) =>
                    {
                        _logger.LogWarning("⚠️ Fallback: запрос не удался");

                        var exception = outcome.Exception;
                        var isEmpty = outcome.Result?.Count == 0;

                        if (exception != null)
                        {
                            _logger.LogError($"Ошибка: {exception.Message}");
                        }
                        else if (isEmpty)
                        {
                            _logger.LogWarning("Получен пустой результат");
                        }

                        if (oldcache != null)
                        {
                            _logger.LogInformation("✅ Fallback: возвращаю старые данные из кэша");
                            return oldcache;
                        }

                        string stalekey = $"stalekey:{key_cache}";

                        if (_memorycache.TryGetValue(stalekey, out object? staleobject))
                        {
                            if (staleobject is List<RepositoryContent> cachedstale)
                            {
                                _logger.LogInformation("✅ Fallback: возвращаю stale-копию");
                                return cachedstale;
                            }
                        }

                        _logger.LogWarning("⚠️ Fallback: кэш пуст, возвращаю пустой список");
                        return new List<RepositoryContent>();
                    },
                    onFallbackAsync: async (outcome, ct) =>
                    {
                        _logger.LogError($"🆘 Fallback сработал: {outcome.Exception?.Message}");
                        await Task.CompletedTask;
                    });

                _logger.LogInformation("Начинаю запрос данных");
                var result = await fallback.ExecuteAsync(async () =>
                {
                    var resultat = await Request(owner, repo, path, cancellation).ConfigureAwait(false);

                    if (resultat != null && resultat.Count > 0)
                    {
                        var options = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(15))
                        .SetSlidingExpiration(TimeSpan.FromMinutes(10));

                        _memorycache.Set(key_cache, resultat, options);

                        var optionsstale = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(25));

                        _memorycache.Set($"stale:{key_cache}", resultat, optionsstale);

                        _logger.LogInformation($"✅ Данные сохранены в кэш для {key_cache}");
                    }
                    return resultat ?? new List<RepositoryContent>();
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
                return new List<RepositoryContent>();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<List<RepositoryContent>> Request(string owner, string repo, string path = "", CancellationToken cancellation = default)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("GithubApiClient2");
                string url = string.IsNullOrEmpty(path)
                    ? $"repos/{owner}/{repo}/contents"
                    : $"repos/{owner}/{repo}/contents/{path}";
                _logger.LogInformation("Запрашиваю содержимое: {url}", url);

                var token3 = Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? throw new InvalidOperationException("GITHUB_TOKEN not set");
                client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("token", token3);

                var options = new HttpRequestMessage(HttpMethod.Get, url)
                {
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
                };

                _logger.LogInformation("Начинаю запрос Контента репозиториев");
                var tiner = System.Diagnostics.Stopwatch.StartNew();
                using HttpResponseMessage recpon = await client.SendAsync(options).ConfigureAwait(false);
                tiner.Stop();
                _logger.LogInformation($"Запрос завершен за {tiner}");
                if (recpon.IsSuccessStatusCode)
                {
                    if (recpon != null)
                    {
                        try
                        {
                            _logger.LogInformation("Читаю ответ");
                            var timer2 = System.Diagnostics.Stopwatch.StartNew();
                            var content = await recpon.Content.ReadAsStreamAsync().ConfigureAwait(false);
                            _logger.LogInformation($"Прочитано за {timer2}");

                            _logger.LogInformation("Начинаю парсинг");
                            var result = await _parser.Parsed2(content);
                            _logger.LogInformation("Парсинг завершен");

                            return result?
                                .OrderByDescending(x => x.IsDirectory)
                                .ThenBy(x => x.Name)
                                .ToList() ?? new List<RepositoryContent>();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
                            return new List<RepositoryContent>();
                        }
                    }
                    else
                    {
                        _logger.LogError("Ответ от сервера не найден");
                        return new List<RepositoryContent>();
                    }
                }
                else
                {
                    _logger.LogError("запрос завершился ошибкой. посткод:" + recpon.StatusCode);
                    return new List<RepositoryContent>();
                }
            }
            catch (TaskCanceledException ex) when (!cancellation.IsCancellationRequested)
            {
                _logger.LogError("Операция отменена" + ex.Message + ex.StackTrace);
                return new List<RepositoryContent>();
            }
            catch (TaskCanceledException ex) when (cancellation.IsCancellationRequested)
            {
                _logger.LogError("Операция отменена пользователем" + ex.Message + ex.StackTrace);
                return new List<RepositoryContent>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError("Возниклои исключение при выполнении запроса" + ex.Message + ex.StackTrace);
                return new List<RepositoryContent>();
            }
            catch (Exception ex)
            {
                _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
                return new List<RepositoryContent>();
            }
        }
    }
}

