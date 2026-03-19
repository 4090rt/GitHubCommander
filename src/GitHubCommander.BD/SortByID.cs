using GithubComander.src.GitHubCommander.Infrastructure.Delegates;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubComander.src.GitHubCommander.BD
{
    public class SortByID
    {
        private readonly ILogger _logger;
        private readonly IMemoryCache _memoryCache;
        private bool _ischeked = false;
        private readonly PollSQLiteConnect _pollSQLiteConnect;
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly FallBackPolitic _fallbackPolitic;

        public SortByID(ILogger logger, IMemoryCache memoryCache, PollSQLiteConnect pollSQLiteConnect, FallBackPolitic fallbackPolitic)
        {
            _logger = logger;
            _memoryCache = memoryCache;
            _pollSQLiteConnect = pollSQLiteConnect;
            _fallbackPolitic = fallbackPolitic;
            Task.Run(async () => await Inchializate()).ConfigureAwait(false);
        }

        public async Task Inchializate()
        {
            if (_ischeked) return;

            await IndexCrete();
            await IndexProverka();

            _ischeked = true;
        }

        public async Task<List<DataClassLogS>> CacheRequest(int pagecount, int page, CancellationToken cancellation)
        {
            string cache_key = $"Id_cachekey{page}and{pagecount}";
            List<DataClassLogS> oldcache = null;
            if (_memoryCache.TryGetValue(cache_key, out List<DataClassLogS> cached))
            { 
               oldcache = cached;
                _logger.LogInformation($"📦 Данные из кэша для {cache_key}");
                return cached;
            }
            await _semaphoreSlim.WaitAsync(cancellation);
            try
            {
                if (_memoryCache.TryGetValue(cache_key, out List<DataClassLogS> cached2))
                {
                    return cached2;
                }

                var fallback = _fallbackPolitic.FallbackPolitic(_fallbackPolitic.Proverka, oldcache, cache_key, cancellation);

                _logger.LogInformation("Делаю запрос к базе");

                var fallbackres = await fallback.ExecuteAsync(async () =>
                {
                    var result = await Request(pagecount, page, cancellation);

                    if (result != null && result.Count > 0)
                    {
                        var options = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(15))
                        .SetSlidingExpiration(TimeSpan.FromMinutes(10));

                        _memoryCache.Set(cache_key, result, options);

                        string cachestale = $"stalekey{cache_key}";

                        var staleoptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(15));

                        _memoryCache.Set(cachestale, result, staleoptions);

                        _logger.LogInformation($"✅ Cached fresh data for {cache_key}");
                    }
                    return result ?? new List<DataClassLogS>();
                });
                return fallbackres;
            }
            catch (Exception ex)
            {
                _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
                return new List<DataClassLogS>();
            }
            finally
            { 
                _semaphoreSlim.Release();
            }
        }

        public async Task<List<DataClassLogS>> Request(int pagecount, int page, CancellationToken cancellation)
        {
            int offset = (page - 1) * pagecount;
            SQLiteConnection connection = null;
            try
            {
                connection = _pollSQLiteConnect.PoolOpen();
                var item = new List<DataClassLogS>();
                string command = "SELECT Id, LogText, Date  FROM LogBase ORDER BY Id DESC LIMIT @Limit OFFSET @Offset";
                var timer = System.Diagnostics.Stopwatch.StartNew();
                _logger.LogInformation("Начинаю запрос");

                using (var sqlcommand = new SQLiteCommand(command, connection))
                {
                    sqlcommand.Parameters.AddWithValue("@Limit", pagecount);
                    sqlcommand.Parameters.AddWithValue("@Offset", offset);

                    using (var result = await sqlcommand.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        int count = 0;

                            while (await result.ReadAsync())
                            {
                                string id = result.GetString(0);
                                string logs = result.GetString(1);
                                DateTime date = result.GetDateTime(2);

                                var onjects = new DataClassLogS
                                {
                                    ID = id,
                                    LogText = logs,
                                    date = date,
                                };

                                item.Add(onjects);
                                count++;
                            }
                            timer.Stop();
                            _logger.LogInformation($"найдено {count} записей за {timer.ElapsedMilliseconds}мс");
                            return item;
                    }
                }
            }
            catch (SQLiteException ex)
            {
                _logger.LogError("Возникло исключение при работе с бд" + ex.Message + ex.StackTrace);
                return new List<DataClassLogS>();
            }
            catch (Exception ex)
            {
                _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
                return new List<DataClassLogS>();
            }
            finally
            {
                if (connection != null)
                { 
                    _pollSQLiteConnect.PullClose(connection);
                }
            }
        }

        public async Task IndexCrete()
        {
            SQLiteConnection connection = null;
            try
            {
                connection = _pollSQLiteConnect.PoolOpen();
                string command = "CREATE INDEX IF NOT EXISTS IX_LogBase_ID ON LogBase(Id, LogText, Date)";

                using (var sqlcommand = new SQLiteCommand(command, connection))
                {
                    await sqlcommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                    _logger.LogError("Индекс создан");
                }
            }
            catch (SQLiteException ex)
            {
                _logger.LogError("Возникло исключение при работе с бд" + ex.Message + ex.StackTrace);
            }
            catch (Exception ex)
            {
                _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
            }
            finally
            {
                if (connection != null)
                {
                    _pollSQLiteConnect.PullClose(connection);
                }
            }
        }

        public async Task<bool> IndexProverka()
        {
            SQLiteConnection connection = null;
            try
            {
                connection = _pollSQLiteConnect.PoolOpen();
                string command = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = 'IX_LogBase_ID' AND tbl_name = 'LogBase'";

                using (var sqlcommand = new SQLiteCommand(command, connection))
                { 
                   var result = await sqlcommand.ExecuteScalarAsync().ConfigureAwait(false);

                    if (result != null && result != DBNull.Value)
                    {
                        bool exec = Convert.ToInt64(result) == 1;
                        if (exec)
                        {
                            _logger.LogInformation($"✅ Индекс 'IX_LogBase_ID' существует!");
                        }
                        else
                        {
                            _logger.LogError($"❌ Индекс 'IX_LogBase_ID' не найден");
                        }
                        return exec;
                    }
                    return false;
                }
            }
            catch (SQLiteException ex)
            {
                _logger.LogError("Возникло исключение при работе с бд" + ex.Message + ex.StackTrace);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
                return false;
            }
            finally
            {
                if (connection != null)
                {
                    _pollSQLiteConnect.PullClose(connection);
                }
            }
        }
    }
}
