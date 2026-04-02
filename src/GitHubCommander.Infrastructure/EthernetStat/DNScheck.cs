using GithubComander.src.GitHubCommander.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GithubComander.src.GitHubCommander.Infrastructure.EthernetStat
{
    public class DNScheck
    {
        private readonly ILogger<DNScheck> _logger;
        private readonly TimeSpan _timeout = TimeSpan.FromSeconds(3);
        private readonly IMemoryCache _memoryCache;
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        public DNScheck(ILogger<DNScheck> logger, IMemoryCache memoryCache)
        {
            _logger = logger;
            _memoryCache = memoryCache;
        }

        public async Task<List<DnsResult>> CacheRequest(string host, CancellationToken cancellation = default)
        {
            string cache_key = $"cachekey{host}";
            List<DnsResult> oldcache = null;
            string stalekey = $"stale{cache_key}";

            if (_memoryCache.TryGetValue(cache_key, out List<DnsResult> cached))
            { 
                oldcache = cached;
                return cached;
            }

            await _semaphoreSlim.WaitAsync(cancellation);

            try
            {
                if (_memoryCache.TryGetValue(cache_key, out List<DnsResult> cached2))
                {
                    return cached2;
                }

                var fallback = Policy<List<DnsResult>>
                    .Handle<Exception>()
                    .OrResult(r => r == null)
                    .FallbackAsync(
                    fallbackAction: async (outcome, context, ctx) =>
                    {
                        var exception = outcome.Exception;
                        var Isempty = outcome.Result == null;

                        if (exception != null)
                        {
                            _logger.LogWarning($"⚠️ Fallback by exception: {exception.Message}");
                        }
                        else if (Isempty)
                        {
                            _logger.LogWarning($"⚠️ Fallback by empty result");
                        }
                        else if (oldcache != null)
                        {
                            _logger.LogInformation("✅ Fallback: возвращаю старые данные из кэша");
                            return oldcache;
                        }
                        if (_memoryCache.TryGetValue(stalekey, out List<DnsResult> stalecached))
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

                var fallbackresult = await fallback.ExecuteAsync( async () => 
                {
                    var result = await Request(host, cancellation);

                    if (result != null)
                    {
                        var options = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(3))
                        .SetSlidingExpiration(TimeSpan.FromMinutes(1));

                        _memoryCache.Set(cache_key, result, options);

                        var staleoptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(3));

                        _memoryCache.Set(stalekey, result, staleoptions);
                        _logger.LogInformation("✅ Cached fresh data for {CacheCode}", cache_key);
                        return result;
                    }
                    else
                    {
                        _logger.LogInformation("✅ Using cached data for {CacheCode}", cache_key);
                        return default;
                    }
                });
                return fallbackresult;
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Возникло исключение" + ex.Message + ex.StackTrace);
                return new List<DnsResult>();
            }
            finally
            { 
                _semaphoreSlim.Release();
            }
        }

        public async Task<List<DnsResult>> Request(string host, CancellationToken cancellation = default)
        {
            try
            {
                using var ctx = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
                ctx.CancelAfter(_timeout);

                _logger.LogInformation("Начинаю запрос к днс", host);
                var timer = System.Diagnostics.Stopwatch.StartNew();

                var requestdns = await Dns.GetHostAddressesAsync(host, ctx.Token).ConfigureAwait(false);
                timer.Stop();
                if (requestdns != null && requestdns.Length > 0)
                {
                    var ipv4 =  requestdns.Where(a => a.AddressFamily == AddressFamily.InterNetwork).ToArray();
                    var ipv6 = requestdns.Where(a => a.AddressFamily == AddressFamily.InterNetworkV6).ToArray();


                    return new List<DnsResult>
                    {
                        new DnsResult
                        {
                            Host = host,
                            Addresses = requestdns,
                            ResolveTime = timer.ElapsedMilliseconds,
                            Success = true,
                            Error = null
                        }
                    };
                }
                else
                {
                    _logger.LogError("результат null");

                    return new List<DnsResult>
                    {
                        new DnsResult
                        {
                            Host = host,
                            Addresses = null,
                            ResolveTime = 0,
                            Success = false,
                            Error = "null result"
                        }
                    };
                }
            }
            catch (TaskCanceledException ex) when (!cancellation.IsCancellationRequested)
            {
                _logger.LogError("Операция отменена" + ex.Message + ex.StackTrace);

                return new List<DnsResult>
                    {
                        new DnsResult
                        {
                            Host = host,
                            Addresses = null,
                            ResolveTime = 0,
                            Success = false,
                            Error = ex.Message
                        }
                    };
            }
            catch (TaskCanceledException ex) when (cancellation.IsCancellationRequested)
            {
                _logger.LogError("Операция отменена пользователем" + ex.Message + ex.StackTrace);

                return new List<DnsResult>
                    {
                        new DnsResult
                        {
                            Host = host,
                            Addresses = null,
                            ResolveTime = 0,
                            Success = false,
                            Error = ex.Message
                        }
                    };
            }
            catch (SocketException ex)
            {
                _logger.LogError("Возникло исключение сети" + ex.Message + ex.StackTrace);

                return new List<DnsResult>
                    {
                        new DnsResult
                        {
                            Host = host,
                            Addresses = null,
                            ResolveTime = 0,
                            Success = false,
                            Error = ex.Message
                        }
                    };
            }
            catch (Exception ex)
            {
                _logger.LogError("Возникло исключени" + ex.Message + ex.StackTrace);

                return new List<DnsResult>
                    {
                        new DnsResult
                        {
                            Host = host,
                            Addresses = null,
                            ResolveTime = 0,
                            Success = false,
                            Error = ex.Message
                        }
                    };
            }
        }
    }
}
