using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubComander.src.GitHubCommander.BD
{
    public class SelectAll
    {
        private readonly ILogger _logger;
        private readonly PollSQLiteConnect _pollSQLiteConnect;

        public SelectAll(ILogger logger, PollSQLiteConnect pollSQLiteConnect)
        {
            _logger = logger;
            _pollSQLiteConnect = pollSQLiteConnect;
        }

        public async Task<int> Select()
        {
            SQLiteConnection? connection = null;
            SQLiteTransaction? sQLiteTransaction = null;
            try
            {
                _logger.LogInformation("Показываю логи...");

                connection = _pollSQLiteConnect.PoolOpen();

                await using (sQLiteTransaction = connection.BeginTransaction())
                {
                    string command = "SELECT COUNT(*) FROM LogBase";
                    int result = 0;

                    await using (var commandsql = new SQLiteCommand(command, connection, sQLiteTransaction))
                    {
                        var scalarResult = await commandsql.ExecuteScalarAsync().ConfigureAwait(false);
                        result = scalarResult != null ? Convert.ToInt32(scalarResult) : 0;
                    }
                    await sQLiteTransaction.CommitAsync().ConfigureAwait(false);
                    return result;
                }
            }
            catch (SQLiteException ex)
            {
                _logger.LogError(ex, "Возникло исключение при работе с БД");
                await (sQLiteTransaction?.RollbackAsync() ?? Task.CompletedTask).ConfigureAwait(false);
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Возникло исключение");
                await (sQLiteTransaction?.RollbackAsync() ?? Task.CompletedTask).ConfigureAwait(false);
                return 0;
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
