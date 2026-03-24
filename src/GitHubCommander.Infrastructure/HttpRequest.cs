using GithubComander.src.GitHubCommander.BD;
using GithubComander.src.GitHubCommander.Data;
using GithubComander.src.GitHubCommander.Infrastructure.Delegates;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
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
        private readonly Http2Options _optionsHttp;
        private readonly HttpRequestDelegate _httpRequestDelegate;
        private readonly Staleoptions _staleoptions;
        private readonly SaveLogInBd _saveLogInBd;

        public HttpRequest(Microsoft.Extensions.Caching.Memory.IMemoryCache memorycache, Microsoft.Extensions.Logging.ILogger<HttpRequest> logger, IHttpClientFactory httpClientFactory, GitParser1 parser,
            FallbackPolitic fallbackPolitic, Http2Options optionsHttp, HttpRequestDelegate httpRequestDelegate, SaveLogInBd saveLogInBd)
        {
            _httpClientFactory = httpClientFactory;
            _memorycache = memorycache;
            _logger = logger;
            _parser = parser;
            _fallbackPolitic = fallbackPolitic;
            _optionsHttp = optionsHttp;
            _httpRequestDelegate = httpRequestDelegate;
            _saveLogInBd = saveLogInBd;
        }

        public async Task<List<DataModelRepositoryInfo>> CachingRequest(CancellationToken cancellation = default)
        {
            string cache_code = $"cachde_code_from_baseadress";

            List<DataModelRepositoryInfo>? oldCached = null;
            if (_memorycache.TryGetValue(cache_code, out object? cacheobject) && 
                cacheobject is List<DataModelRepositoryInfo> cached)
            {
                oldCached = cached;
                string log = $"📦 Данные из кэша для {cache_code}";
                _logger.LogInformation(log);
                await _saveLogInBd.Saved(log, DateTime.Now);
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

                    var rre = await _staleoptions.StaleRun(async (res, code) => _staleoptions.StaleOpti(res, code), reuslt, cache_code);
                    return rre;
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
                var client = _optionsHttp.Delegateclient(_optionsHttp.OptionsClient, "GithubApiClient1");

                var options = _optionsHttp.Delegateoptions(_optionsHttp.OptionshTTP2);

                var result = await _httpRequestDelegate.RunRequest(async () =>
                    await _httpRequestDelegate.HttpRequestGit<List<DataModelRepositoryInfo>>(client, options, cancellation),
                    client, cancellation);

                return result;
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
        private readonly FallbackPolitic _fallbackPolitic;
        private readonly HttpRequestDelegate _httpRequestDelegate;
        private readonly Staleoptions _staleoptions;
        private readonly Http2Options _optionsHttp;
        public HttpRequest3(Microsoft.Extensions.Caching.Memory.IMemoryCache memorycache, Microsoft.Extensions.Logging.ILogger<HttpRequest3> logger, IHttpClientFactory httpClientFactory, GitParser1 parser, FallbackPolitic fallbackPolitic)
        {
            _httpClientFactory = httpClientFactory;
            _memorycache = memorycache;
            _logger = logger;
            _parser = parser;
            _fallbackPolitic = fallbackPolitic;
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

                var fallback = _fallbackPolitic.FallbackPolicy(_fallbackPolitic.FallbackProverka, oldCached, key_cache, cancellation);

                _logger.LogInformation("Начинаю запрос данных");

                var result = await fallback.ExecuteAsync(async () =>
                {
                    var reuslt = await Request(owner, repo, path, cancellation).ConfigureAwait(false);

                    var rre = await _staleoptions.StaleRun(async (res, code) => _staleoptions.StaleOpti(res,code), reuslt, key_cache);
                    return rre;
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
                var client = _optionsHttp.Delegateclient(_optionsHttp.OptionsClient, "GithubApiClient1");

                var options = _optionsHttp.Delegateoptions(_optionsHttp.OptionshTTP2);

                var result = await _httpRequestDelegate.RunRequest(async () =>
                    await _httpRequestDelegate.HttpRequestGit<List<FileContent>>(client, options, cancellation),
                    client, cancellation);

                return result;
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
        private readonly FallbackPolitic _fallbackPolitic;
        private readonly HttpRequestDelegate _httpRequestDelegate;
        private readonly Staleoptions _staleoptions;
        private readonly Http2Options _optionsHttp;
        public HttpRequest2(Microsoft.Extensions.Caching.Memory.IMemoryCache memorycache, Microsoft.Extensions.Logging.ILogger<HttpRequest2> logger, IHttpClientFactory httpClientFactory, GitParser1 parser, FallbackPolitic fallbackPolitic)
        {
            _httpClientFactory = httpClientFactory;
            _memorycache = memorycache;
            _logger = logger;
            _parser = parser;
            _fallbackPolitic = fallbackPolitic;
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

                var fallback = _fallbackPolitic.FallbackPolicy(_fallbackPolitic.FallbackProverka, oldcache, key_cache, cancellation);

                _logger.LogInformation("Начинаю запрос данных");
                var result = await fallback.ExecuteAsync(async () =>
                {
                    var resultat = await Request(owner, repo, path, cancellation).ConfigureAwait(false);

                    var rre = await _staleoptions.StaleRun(async (res, code) => _staleoptions.StaleOpti(res, code), resultat, key_cache);
                    return rre;
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
                var client = _optionsHttp.Delegateclient(_optionsHttp.OptionsClient, "GithubApiClient1");

                var options = _optionsHttp.Delegateoptions(_optionsHttp.OptionshTTP2);

                var result = await _httpRequestDelegate.RunRequest(async () =>
                    await _httpRequestDelegate.HttpRequestGit<List<RepositoryContent>>(client, options, cancellation),
                    client, cancellation);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
                return new List<RepositoryContent>();
            }
        }
    }
}

