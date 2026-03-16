using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Fallback;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubComander.src.GitHubCommander.Infrastructure.Delegates
{
    public delegate Task<T> Fallback<T>(T oldcACHE, string memory_key, CancellationToken cancellation = default);
    public class FallBackPolitic
    {
        private readonly ILogger<FallBackPolitic> _logger;
        private readonly IMemoryCache _memoryCache;

        public FallBackPolitic(ILogger<FallBackPolitic> logger, IMemoryCache memoryCache)
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

        public async Task<T> Proverka<T>(T oldcACHE, string memory_key, CancellationToken cancellation = default)
        {
            string stalekey = $"stalekey:{memory_key}";
            if (_memoryCache.TryGetValue(stalekey, out T? cachedstale))
            {
                _logger.LogInformation($"✅ Returning stale copy for {cachedstale}");
                return cachedstale;
            }
            else
            {
                _logger.LogWarning("⚠️ Fallback: кэш пуст, возвращаю default");
                return default!;
            }
        }

        public AsyncFallbackPolicy<T> FallbackPolitic<T>(Fallback<T> fallback,T oldcACHE, string memory_key, CancellationToken cancellation = default)
        {
            var fallbackPolitics = Policy<T>
                .Handle<Exception>()
                .OrResult(r => IsNullOrEmpty(r))
                .FallbackAsync(
                fallbackAction: async (outcome, context, ctx) =>
                {
                    var exception = outcome.Exception;
                    var isempty = IsNullOrEmpty(outcome.Result);

                    if (exception != null)
                    {
                        _logger.LogWarning($"⚠️ Fallback by exception: {exception.Message}");
                    }
                    else if (isempty)
                    {
                        _logger.LogWarning($"⚠️ Fallback by empty result");
                    }
                    else if (oldcACHE != null)
                    {
                        _logger.LogInformation("✅ Fallback: возвращаю старые данные из кэша");
                        return oldcACHE;
                    }

                    return await fallback.Invoke(oldcACHE, memory_key, cancellation);
                },
                onFallbackAsync: async (outcome, ctx) =>
                {
                    _logger.LogError($"🆘 Fallback сработал: {outcome.Exception?.Message}");
                    await Task.CompletedTask;
                });
            return fallbackPolitics;
        }
    }
}
