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
    public class DatesLogsInBd
    {
        private readonly ILogger _logger;
        private readonly IMemoryCache _memoryCache;
        private bool _ischeked = false;
        private readonly PollSQLiteConnect _pollSQLiteConnect;
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly FallBackPolitic _fallbackPolitic;

        public DatesLogsInBd(ILogger logger, IMemoryCache memoryCache, PollSQLiteConnect pollSQLiteConnect, FallBackPolitic fallbackPolitic)
        {
            _logger = logger;
            _memoryCache = memoryCache;
            _pollSQLiteConnect = pollSQLiteConnect;
            _fallbackPolitic = fallbackPolitic;
        }

        public async Task Inithialize()
        {
            if (_ischeked) return;

            await CreateIndex();
            await IndexProverka();

            _ischeked = true;
        }

        public async Task<List<DataClassLog>> RequestCache(DateTime date, int pages, int pagescount, CancellationToken cancellation)
        {
            string cache_key = $"cachekey_dates_{date:yyyyMMdd}_{pages}_{pagescount}";
            List<DataClassLog>? oldcache = null;
            if (_memoryCache.TryGetValue(cache_key, out List<DataClassLog> cached))
            {
                _logger.LogInformation($"📦 Данные из кэша для {cache_key}");
                return cached;
            }
            await _semaphoreSlim.WaitAsync(cancellation);
            try
            {
                if (_memoryCache.TryGetValue(cache_key, out List<DataClassLog> cacheds))
                {
                    return cacheds;
                }

                var fallback = _fallbackPolitic.FallbackPolitic(_fallbackPolitic.Proverka, oldcache, cache_key, cancellation);

                _logger.LogInformation("Делаю запрос к базе");

                var fallbackresult = await fallback.ExecuteAsync(async () =>
                {
                    var result = await Request(date, pages, pagescount, cancellation);

                    var options = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(15))
                        .SetSlidingExpiration(TimeSpan.FromMinutes(10));

                    _memoryCache.Set(cache_key, result, options);

                    string stalekey = $"stale{cache_key}";
                    var staleoptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(15));

                    _memoryCache.Set(stalekey, result, staleoptions);
                    _logger.LogInformation($"✅ Cached fresh data for {cache_key}");
                    return result ?? new List<DataClassLog>();
                });
                return fallbackresult;
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Возникло исключение" + ex.Message + ex.StackTrace);
                throw;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
        public async Task<List<DataClassLog>> Request(DateTime date, int pages, int pagescount, CancellationToken cancellation)
        {
            int offset = (pages - 1) * pagescount;
            SQLiteConnection connection = null;
            try
            {
                connection = _pollSQLiteConnect.PoolOpen();
                var items = new List<DataClassLog>();
                string command = "SELECT Id, LogText, Date FROM LogBase WHERE Date >= @StartDate AND Date < @EndDate ORDER BY Id DESC LIMIT @Limit OFFSET @Offset";
                var timer = System.Diagnostics.Stopwatch.StartNew();
                _logger.LogInformation("Начинаю запрос");

                using (var sqlcommand = new SQLiteCommand(command, connection))
                {
                    sqlcommand.Parameters.AddWithValue("@StartDate", date.Date);
                    sqlcommand.Parameters.AddWithValue("@EndDate", date.Date.AddDays(1));
                    sqlcommand.Parameters.AddWithValue("@Limit", pagescount);
                    sqlcommand.Parameters.AddWithValue("@Offset", offset);

                    using (var result = await sqlcommand.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        int count = 0;

                        if (result != null)
                        {
                            while (await result.ReadAsync())
                            {
                                string id = result.GetString(0);
                                string logs = result.GetString(1);
                                DateTime dateTime = result.GetDateTime(2);

                                var objects = new DataClassLog()
                                {
                                    ID = id,
                                    LogText = logs,
                                    date = dateTime,
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
                _logger.LogError("Возникло исключение при работе с Базой" + ex.Message + ex.StackTrace);
                return new List<DataClassLog>();
            }
            catch (Exception ex)
            {
                _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
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
                string command = "CREATE INDEX IF NOT EXISTS IX_LogBase_Dates ON LogBase(Date)";

                using (var sqlcommand = new SQLiteCommand(command, connection))
                { 
                    await sqlcommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                    _logger.LogInformation("Индекс создан");
                }
            }
            catch (SQLiteException ex)
            {
                _logger.LogError("Возникло исключение при работе с Базой" + ex.Message + ex.StackTrace);
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
                string command = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = 'IX_LogBase_Dates' AND tbl_name = 'LogBase'";

                using (var sqlcommand = new SQLiteCommand(command, connection))
                { 
                    var result = await sqlcommand.ExecuteScalarAsync().ConfigureAwait(false);

                    if (result != null && result != DBNull.Value)
                    {
                        bool execc = Convert.ToInt32(result) == 1;

                        if (execc)
                        {
                            _logger.LogInformation($"✅ Индекс 'IX_LogBase_Dates' существует!");
                        }
                        else
                        {
                            _logger.LogInformation($"❌ Индекс 'IX_LogBase_LogDates' не найден");
                        }
                        return execc;
                    }
                    return false;
                }
            }
            catch (SQLiteException ex)
            {
                _logger.LogError("Возникло исключение при работе с Базой" + ex.Message + ex.StackTrace);
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
