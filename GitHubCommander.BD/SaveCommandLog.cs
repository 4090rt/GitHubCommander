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
            if (logs == null || !logs.Any()) return;

            SQLiteConnection connection = null;
            SQLiteTransaction sQLiteTransaction = null;
            try
            {
                _logger.LogInformation("Сохранение логов...");
                _logger.LogInformation($"Пакетное сохранение {logs.Count()} логов...");

                connection = _pollSQLiteConnect.PoolOpen();
                sQLiteTransaction = connection.BeginTransaction();

                string command = "INSERT INTO [LogBase] (Log, Date) VALUES(@L, @D)";

                using (var commandsql = new SQLiteCommand(command, connection, sQLiteTransaction))
                {
                    commandsql.Parameters.Add("@L", DbType.String);
                    commandsql.Parameters.Add("@D", DbType.DateTime);

                    foreach (var log in logs)
                    {
                        commandsql.Parameters["@L"].Value = log.loginfo;
                        commandsql.Parameters["@D"].Value = log.dateTime;
                        await commandsql.ExecuteNonQueryAsync().ConfigureAwait(false);                  
                    }
                }
                sQLiteTransaction.Commit();
                _logger.LogInformation($"Успешно добавлено {logs.Count()} записей");
            }
            catch (SQLiteException ex)
            {
                sQLiteTransaction?.Rollback();
                _logger.LogError($"Возникло исключение при работе с БД" + ex.Message + ex.StackTrace);
                return;
            }
            catch (Exception ex)
            {
                sQLiteTransaction?.Rollback();
                _logger.LogError("ВОзникло исключение" + ex.Message + ex.StackTrace);
                return;
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
