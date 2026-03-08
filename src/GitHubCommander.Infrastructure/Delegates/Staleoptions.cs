using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GithubComander.src.GitHubCommander.Infrastructure.Delegates
{
    public class Staleoptions
    {
        private readonly ILogger<Staleoptions> _logger;
        private readonly IMemoryCache _memoryCache;

        public Staleoptions(ILogger<Staleoptions> logger, IMemoryCache memoryCache)
        {
            _logger = logger;
            _memoryCache = memoryCache;
        }

        public async Task<T?> StaleRun<T>(Func<T, string, Task<T>> func, T result, string cache_code)
        {
            return await func.Invoke(result, cache_code);
        }

        public T? StaleOpti<T>(T result, string cache_code)
        {
            if (result != null && !IsNullOrEmpty(result))
            {
                var options = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(15))
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));

                _memoryCache.Set(cache_code, result, options);

                var staleOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(25));

                _memoryCache.Set($"stale:{cache_code}", result, staleOptions);
                _logger.LogInformation("✅ Cached fresh data for {CacheCode}", cache_code);
                return result;
            }
            else
            {
                _logger.LogInformation("✅ Using cached data for {CacheCode}", cache_code);
                return default(T);
            }
        }

        public static bool IsNullOrEmpty<T>(T? result)
        {
            if (result == null) return true;

            if (result is IList list)
            {
                return list.Count == 0;
            }

            if (result is ICollection<T> genericCollection)
            {
                return genericCollection.Count == 0;
            }

            return false;
        }
    }
}
