using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace GithubComander.src.GitHubCommander.BD
{
    public class SaveCommandLog
    {
        private readonly Microsoft.Extensions.Logging.ILogger _logger;
        private readonly PollSQLiteConnect _pollSQLiteConnect;
        public SaveCommandLog(Microsoft.Extensions.Logging.ILogger logger, PollSQLiteConnect pollSQLiteConnect)
        {
            _logger = logger;
            _pollSQLiteConnect = pollSQLiteConnect;
        }

        public async Task SaveLogg(IEnumerable<(string loginfo, DateTime dateTime)> logs)
        {
            if (logs == null || !logs.Any())
            {
                _logger.LogWarning("Пустой список логов, сохранение отменено");
                return;
            }

            SQLiteConnection? connection = null;
            SQLiteTransaction? sQLiteTransaction = null;
            try
            {
                _logger.LogInformation("Сохранение логов...");
                _logger.LogInformation("Пакетное сохранение {Count} логов...", logs.Count());

                connection = _pollSQLiteConnect.PoolOpen();
                await using (sQLiteTransaction = connection.BeginTransaction())
                {
                    string command = "INSERT INTO [LogBase] (Log, Date) VALUES(@L, @D)";

                    await using (var commandsql = new SQLiteCommand(command, connection, sQLiteTransaction))
                    {
                        commandsql.Parameters.Add("@L", DbType.String);
                        commandsql.Parameters.Add("@D", DbType.String);

                        foreach (var log in logs)
                        {
                            commandsql.Parameters["@L"].Value = log.loginfo;
                            commandsql.Parameters["@D"].Value = log.dateTime.ToString("yyyy-MM-dd HH:mm:ss");
                            await commandsql.ExecuteNonQueryAsync().ConfigureAwait(false);
                        }
                    }

                    await sQLiteTransaction.CommitAsync().ConfigureAwait(false);
                }

                _logger.LogInformation("Успешно добавлено {Count} записей", logs.Count());
            }
            catch (SQLiteException ex)
            {
                _logger.LogError(ex, "Возникло исключение при работе с БД");
                await (sQLiteTransaction?.RollbackAsync() ?? Task.CompletedTask).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Возникло исключение");
                await (sQLiteTransaction?.RollbackAsync() ?? Task.CompletedTask).ConfigureAwait(false);
            }
            finally
            {
                sQLiteTransaction?.Dispose();
                if (connection != null)
                {
                    _pollSQLiteConnect.PullClose(connection);
                }
            }
        }
    }
}
