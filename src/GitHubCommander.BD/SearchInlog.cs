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
    public class DataClassLogS
    {
        public string ID { get; set; }
        public string LogText { get; set; }
        public DateTime date { get; set; }
    }

    public class SearchInlog
    {
        private readonly ILogger _logger;
        private readonly IMemoryCache _memoryCache;
        private bool _ischeked = false;
        private readonly PollSQLiteConnect _pollSQLiteConnect;
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly FallBackPolitic _fallbackPolitic;

        public SearchInlog(ILogger logger, IMemoryCache memoryCache, PollSQLiteConnect pollSQLiteConnect, FallBackPolitic fallbackPolitic)
        {
            _logger = logger;
            _memoryCache = memoryCache;
            _pollSQLiteConnect = pollSQLiteConnect;
            _fallbackPolitic = fallbackPolitic;
        }

        public async Task Inithializate()
        {
            if (_ischeked) return;

            await IndexCreate();
            await ProverkaIndex();

            _ischeked = true;
        }

        public async Task<List<DataClassLogS>> RequestCache(string log, int pagecount, int page, CancellationToken cancellation)
        {
            string keycache = $"key_cache_{log}_{pagecount}_{page}";
            List<DataClassLogS>? oldcache = null;
            if (_memoryCache.TryGetValue(keycache, out List<DataClassLogS> cached))
            {
                oldcache = cached;
                _logger.LogInformation($"📦 Данные из кэша для {keycache}");
                return cached;
            }

            await _semaphoreSlim.WaitAsync(cancellation);
            try
            {
                if (_memoryCache.TryGetValue(keycache, out List<DataClassLogS> cachedd))
                {
                    return cachedd;
                }

                var fallback = _fallbackPolitic.FallbackPolitic(_fallbackPolitic.Proverka, oldcache, keycache, cancellation);

                _logger.LogInformation("Делаю запрос к базе");

                var fallbackresult = await fallback.ExecuteAsync(async () =>
                {
                    var result = await Request(log, pagecount, page, cancellation);

                    if (result != null && result.Count > 0)
                    {
                        var options = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(TimeSpan.FromMinutes(15))
                            .SetSlidingExpiration(TimeSpan.FromMinutes(3));

                        _memoryCache.Set(keycache, result, options);

                        var stalekey = $"stalekey:{keycache}";

                        var staleoptions = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(TimeSpan.FromMinutes(15));

                        _memoryCache.Set(stalekey, result, staleoptions);
                        _logger.LogInformation($"✅ Cached fresh data for {keycache}");
                    }
                    return result ?? new List<DataClassLogS>();
                });
                return fallbackresult;
            }
            catch (Exception ex)
            {
                _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
                throw;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task<List<DataClassLogS>> Request(string log, int pagecount, int page, CancellationToken cancellation)
        {
            int offset = (page - 1) * pagecount;
            SQLiteConnection connection = null;
            try
            {
                connection = _pollSQLiteConnect.PoolOpen();
                string command = "SELECT Id, LogText, Date FROM LogBase WHERE LogText LIKE @SearchLog ORDER BY Date DESC LIMIT @Limit OFFSET @Offset";
                var items = new List<DataClassLogS>();
                var timer = System.Diagnostics.Stopwatch.StartNew();
                _logger.LogInformation("Начинаю запрос");

                using (var sqlcommand = new SQLiteCommand(command, connection))
                {
                    sqlcommand.Parameters.AddWithValue("@SearchLog", $"%{log}%");
                    sqlcommand.Parameters.AddWithValue("@Limit", pagecount);
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
                                DateTime date = result.GetDateTime(2);

                                var objects = new DataClassLogS()
                                {
                                    ID = id,
                                    LogText = logs,
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
                            return new List<DataClassLogS>();
                        }
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

        public async Task IndexCreate()
        {
            SQLiteConnection connection = null;
            try
            {
                connection = _pollSQLiteConnect.PoolOpen();
                string command = "CREATE INDEX IF NOT EXISTS IX_LogBase_LogText ON LogBase(LogText)";

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

        public async Task<bool> ProverkaIndex()
        {
            SQLiteConnection connection = null;
            try
            {
                connection = _pollSQLiteConnect.PoolOpen();
                string command = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = 'IX_LogBase_LogText' AND tbl_name = 'LogBase'";

                using (var selcommand = new SQLiteCommand(command, connection))
                {
                    var result = await selcommand.ExecuteScalarAsync().ConfigureAwait(false);

                    if (result != null && result != DBNull.Value)
                    {
                        bool exec = Convert.ToInt32(result) == 1;

                        if (exec)
                        {
                            _logger.LogError($"✅ Индекс 'IX_LogBase_LogText' существует!");
                        }
                        else
                        {
                            _logger.LogError($"❌ Индекс 'IX_LogBase_LogText' не найден");
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
