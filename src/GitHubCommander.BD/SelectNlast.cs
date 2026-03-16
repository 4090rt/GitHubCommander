using GithubComander.src.GitHubCommander.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubComander.src.GitHubCommander.BD
{
    public class NolockOption
    {
        public bool NolockUsing { get; set; }
        public bool Logging { get; set; }
    }
    public class DataClassLog
    { 
        public string ID { get; set; }
        public string LogText { get; set; }
        public DateTime date { get; set; }
    }

    public class SelectNlast
    {
        private readonly ILogger _logger;
        private readonly IMemoryCache _memoryCache;
        private bool _ischeked = false;
        private readonly PollSQLiteConnect _pollSQLiteConnect;
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly FallbackPolitic _fallbackPolitic;

        public SelectNlast(ILogger logger, IMemoryCache memoryCache, 
            PollSQLiteConnect pollSQLiteConnect, FallbackPolitic fallbackPolitic)
        {
            _logger = logger;
            _memoryCache = memoryCache;
            _pollSQLiteConnect = pollSQLiteConnect;
            _fallbackPolitic = fallbackPolitic;
        }

        public async Task Inthializate()
        {
            if (_ischeked) return;

            if (_ischeked == false)
            { 
                await CreateIndex();
                await IndexProverka();
            }

            _ischeked = true;
        }

        public async Task<List<DataClassLog>> CachingReq(int pagecount, int page, NolockOption optionsLock, CancellationToken cancellation = default)
        {
            string keycache = $"key_cache{pagecount}";
            List<DataClassLog>? oldcache = null;
            if (_memoryCache.TryGetValue(keycache, out List<DataClassLog> datacache))
            { 
                oldcache = datacache;
                _logger.LogInformation($"📦 Данные из кэша для {keycache}");
                return datacache;
            }
            await _semaphoreSlim.WaitAsync(cancellation);
            try
            {
                if (_memoryCache.TryGetValue(keycache, out List<DataClassLog> cached))
                {
                    return cached;
                }

                var fallback = _fallbackPolitic.FallbackPolicy(_fallbackPolitic.FallbackProverka, oldcache, keycache, cancellation);

                _logger.LogInformation("Делаю запрос к базе");

                var fallbackresult = await fallback.ExecuteAsync(async () =>
                {
                    var result = await Request(pagecount, page).ConfigureAwait(false);

                    if (result != null && result.Count > 0)
                    {
                        var options = new MemoryCacheEntryOptions()
                               .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
                               .SetSlidingExpiration(TimeSpan.FromMinutes(3));

                        _memoryCache.Set(keycache, result, options);

                        var StaleOptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

                        _memoryCache.Set($"stale: {keycache}", result, StaleOptions);
                        _logger.LogInformation($"✅ Cached fresh data for {keycache}");
                    }
                    return result ?? new List<DataClassLog>();
                });
                return fallbackresult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении Информации");
                throw;
            }
            finally
            { 
                _semaphoreSlim.Release();
            }
        }

        public async Task<List<DataClassLog>>  Request(int pagecount, int page)
        {
            int offset = (page - 1) * pagecount;

            SQLiteConnection connection = null;
            try
            {
                var items = new List<DataClassLog>();
                string command = "SELECT Id, Log, Date FROM LogBase ORDER BY Date DESC LIMIT @PageSize OFFSET @Offset";
                connection = _pollSQLiteConnect.PoolOpen();
                var timer = System.Diagnostics.Stopwatch.StartNew();
                _logger.LogInformation("Начинаю запрос");

                using (var selcommmand = new SQLiteCommand(command, connection))
                {
                    selcommmand.Parameters.AddWithValue("@PageSize", pagecount);
                    selcommmand.Parameters.AddWithValue("@Offset", offset);

                    using (var result = await selcommmand.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        if (result != null)
                        {
                            int count = 0;

                            while (await result.ReadAsync().ConfigureAwait(false))
                            {
                                string id = result.GetString(0);
                                string log = result.GetString(1);
                                DateTime date = result.GetDateTime(2);

                                var objects = new DataClassLog()
                                {
                                    ID = id,
                                    LogText = log,
                                    date = date,
                                };
                                items.Add(objects);
                                count++;
                            }
                            timer.Stop();
                            _logger.LogInformation($"найдено {count} записей за {timer.ElapsedMilliseconds}мс");
                            return items;
                        }
                        else
                        {
                            _logger.LogWarning("Бд ничего не вернула");
                            return new List<DataClassLog>();
                        }
                    }
                }
            }
            catch (SQLiteException ex)
            {
                _logger.LogInformation("Возникло исключение при работе с бд" + ex.Message + ex.StackTrace);
                return new List<DataClassLog>();
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Возникло исключение" + ex.Message + ex.StackTrace);
                return new List<DataClassLog>();
            }
            finally
            {
                if (connection != null)
                {
                    _pollSQLiteConnect.PullClose(connection);
                }
            }
        }
        public async Task CreateIndex()
        {
            SQLiteConnection connection = null;
            try
            {
                connection = _pollSQLiteConnect.PoolOpen();
                string command = "CREATE INDEX IF NOT EXISTS IX_LogBase_Date ON LogBase(Date)";

                using (var commandsql = new SQLiteCommand(command, connection))
                {
                    await commandsql.ExecuteNonQueryAsync().ConfigureAwait(false);
                    _logger.LogInformation("Индекс создан");
                }
            }
            catch (SQLiteException ex)
            {
                _logger.LogInformation("Возникло исключение при работе с бд" + ex.Message + ex.StackTrace);
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Возникло исключение" + ex.Message + ex.StackTrace);
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
                string command = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = 'IX_LogBase_Date' AND tbl_name = 'LogBase'";

                using (var commandsql = new SQLiteCommand(command, connection))
                {
                    var result = await commandsql.ExecuteScalarAsync().ConfigureAwait(false);

                    if (result != null && result != DBNull.Value)
                    {
                        bool exists = Convert.ToInt64(result) > 0;

                        if (exists)
                        {
                            _logger.LogInformation($"✅ Индекс 'IX_LogBase_Date' существует!");
                        }
                        else
                        {
                            _logger.LogInformation($"❌ Индекс 'IX_LogBase_Date' не найден");
                        }
                        return exists;
                    }
                    return false;
                }
            }
            catch (SQLiteException ex)
            {
                _logger.LogInformation("Возникло исключение при работе с бд" + ex.Message + ex.StackTrace);
                return false;

            }
            catch (Exception ex)
            {
                _logger.LogInformation("Возникло исключение" + ex.Message + ex.StackTrace);
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
