using GithubComander.src.GitHubCommander.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GithubComander.src.GitHubCommander.Infrastructure
{
    public class HttpRequest
    {
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _memorycache;
        private readonly Microsoft.Extensions.Logging.ILogger<HttpRequest> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GitParser1 _parser;
        public HttpRequest(Microsoft.Extensions.Caching.Memory.IMemoryCache memorycache, Microsoft.Extensions.Logging.ILogger<HttpRequest> logger, IHttpClientFactory httpClientFactory, GitParser1 parser)
        {
            _httpClientFactory = httpClientFactory;
            _memorycache = memorycache;
            _logger = logger;
            _parser = parser;
        }

        public async Task<List<DataModelRepositoryInfo>> CachingRequest(string url = "", CancellationToken cancellation = default)
        {
            string cache_code = $"cachde_code_from{url}";

            if (_memorycache.TryGetValue(cache_code, out object? cacheobject))
            {
                if (cacheobject is List<DataModelRepositoryInfo> cached)
                {
                    _logger.LogInformation($"📦 Данные из кэша для {cache_code}");
                    return cached;
                }
            }
            try
            {
                _logger.LogInformation("начинаю процесс получения данных");

                var reuslt = await Request(url, cancellation);

                var options = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(15))
                    .SetSlidingExpiration(TimeSpan.FromMinutes(10));

                _memorycache.Set(cache_code, reuslt, options);
                return reuslt;
            }
            catch (Exception ex)
            {
                _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
                return new List<DataModelRepositoryInfo>();
            }
        }

        public async Task<List<DataModelRepositoryInfo>> Request(string url = "", CancellationToken cancellation = default)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("GithubApiClient");

                client.DefaultRequestHeaders.Authorization =
                              new System.Net.Http.Headers.AuthenticationHeaderValue("token", "");

                var options = new HttpRequestMessage(HttpMethod.Get, url)
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
        public HttpRequest3(Microsoft.Extensions.Caching.Memory.IMemoryCache memorycache, Microsoft.Extensions.Logging.ILogger<HttpRequest3> logger, IHttpClientFactory httpClientFactory, GitParser1 parser)
        {
            _httpClientFactory = httpClientFactory;
            _memorycache = memorycache;
            _logger = logger;
            _parser = parser;
        }

        public async Task<FileContent> CacheRequest(string owner, string repo, string path = "", CancellationToken cancellation = default)
        {
            string key_cache = $"cached_key{owner}{repo}{path}";

            if (_memorycache.TryGetValue(key_cache, out FileContent cached))
            {
                _logger.LogInformation($"📦 Данные из кэша для {key_cache}");
                return cached;
            }
            try
            {
                _logger.LogInformation("Начинаю запрос данных");

                var result = await Request(owner, repo, path, cancellation);

                var options = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(15))
                    .SetSlidingExpiration(TimeSpan.FromMinutes(10));

                _memorycache.Set(key_cache, result, options);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
                return new FileContent();
            }
        }

        public async Task<FileContent> Request(string owner, string repo, string path = "", CancellationToken cancellation = default)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("GithubApiClient");
                string url = $"/repos/{owner}/{repo}/contents/{path}";
                _logger.LogInformation("Запрашиваю содержимое: {url}", url);

                client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("token", "");

                var options = new HttpRequestMessage(HttpMethod.Get, url)
                {
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
                };

                _logger.LogInformation("Начинаю запрос");
                var timer = System.Diagnostics.Stopwatch.StartNew();
                using HttpResponseMessage recpon = await client.SendAsync(options).ConfigureAwait(false);
                _logger.LogInformation($"Запрос завершен за {recpon}");
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

                            return result;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
                            return new FileContent();
                        }
                    }
                    else
                    {
                        _logger.LogError("Ответ от сервера не найден");
                        return new FileContent();
                    }
                }
                else
                {
                    _logger.LogError("запрос завершился ошибкой. посткод:" + recpon.StatusCode);
                    return new FileContent();
                }
            }
            catch (TaskCanceledException ex) when (!cancellation.IsCancellationRequested)
            {
                _logger.LogError("Операция отменена" + ex.Message + ex.StackTrace);
                return new FileContent();
            }
            catch (TaskCanceledException ex) when (cancellation.IsCancellationRequested)
            {
                _logger.LogError("Операция отменена пользователем" + ex.Message + ex.StackTrace);
                return new FileContent();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError("Возниклои исключение при выполнении запроса" + ex.Message + ex.StackTrace);
                return new FileContent();
            }
            catch (Exception ex)
            {
                _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
                return new FileContent();
            }
        }
    }

    public class HttpRequest2
    {
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _memorycache;
        private readonly Microsoft.Extensions.Logging.ILogger<HttpRequest2> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GitParser1 _parser;
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

            if (_memorycache.TryGetValue(key_cache, out List<RepositoryContent> cachded))
            {
                _logger.LogInformation($"📦 Данные из кэша для {key_cache}");
                return cachded;
            }
            try
            {
                _logger.LogInformation("Начинаю запрос данных");

                var result = await Request(owner, repo, path, cancellation);

                var options = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(15))
                    .SetSlidingExpiration(TimeSpan.FromMinutes(10));

                _memorycache.Set(key_cache, result, options);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
                return new List<RepositoryContent>();
            }
        }

        public async Task<List<RepositoryContent>> Request(string owner, string repo, string path = "", CancellationToken cancellation = default)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("GithubApiClient");
                string url = $"/repos/{owner}/{repo}/contents/{path}";
                _logger.LogInformation("Запрашиваю содержимое: {url}", url);

                client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("token", "");

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

