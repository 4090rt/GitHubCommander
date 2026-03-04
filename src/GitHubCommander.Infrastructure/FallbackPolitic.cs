using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polly;
using Polly.Fallback;

namespace GithubComander.src.GitHubCommander.Infrastructure
{
    public delegate Task<T> FallbackPol<T>(T oldcache, string cache_key, CancellationToken cancellation = default);
    public class FallbackPolitic
    {
        private readonly ILogger<FallbackPolitic> _logger;
        private readonly IMemoryCache _memoryCache;

        public FallbackPolitic(ILogger<FallbackPolitic> logger, IMemoryCache memoryCache)
        { 
            _logger = logger;
            _memoryCache = memoryCache;
        }

        public static bool IsNullOrEmpty<T>(T? result)
        {
            if (result == null) return true;

            if (result is System.Collections.IList list)
            {
                return list.Count == 0;
            }

            if (result is List<T> genericList)
            {
                return genericList.Count == 0;
            }

            return false;
        }

        public async Task<T> FallbackProverka<T>(T oldcache, string cache_key, CancellationToken cancellation = default)
        {
            var stalekey = $"stale:{cache_key}";
            if (_memoryCache.TryGetValue(stalekey, out T? stalecache) && stalecache != null)
            {
                _logger.LogInformation($"✅ Returning stale copy for {stalecache}");
                return stalecache;
            }
            else
            {
                _logger.LogWarning("⚠️ Fallback: кэш пуст, возвращаю default");
                return default!;
            }
        }

        public AsyncFallbackPolicy<T> FallbackPolicy<T>(FallbackPol<T> delegates,T oldcache, string cache_key, CancellationToken cancellation = default)
        {
            var fallback = Policy<T>
                .Handle<Exception>()
                .OrResult(r => IsNullOrEmpty(r))
                .FallbackAsync(
                fallbackAction: async (outcome, context, ct) =>
                {
                    var exception = outcome.Exception;
                    var isEmpty = IsNullOrEmpty(outcome.Result);

                    if (exception != null)
                    {
                        _logger.LogError($"Ошибка: {exception.Message}");
                    }
                    if (isEmpty)
                    {
                        _logger.LogWarning("Получен пустой результат");
                    }
                    if (oldcache != null)
                    {
                        _logger.LogInformation("✅ Fallback: возвращаю старые данные из кэша");
                        return oldcache;
                    }
                    _logger.LogWarning("⚠️ Fallback: кэш пуст, возвращаю пустой список");
                    return await delegates.Invoke(oldcache, cache_key, cancellation);
                },
                onFallbackAsync: async (outcome, ctx) =>
                {
                    _logger.LogError($"🆘 Fallback сработал: {outcome.Exception?.Message}");
                    await Task.CompletedTask;
                });

            return fallback;
        }
    }
}
