using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubComander.src.GitHubCommander.BD
{
    public class MassUpdate
    {
        private readonly ILogger _logger;
        private readonly PollSQLiteConnect _pollSQLiteConnect;

        public MassUpdate(ILogger logger, PollSQLiteConnect pollSQLiteConnect)
        {
            _logger = logger;
            _pollSQLiteConnect = pollSQLiteConnect;
        }

        public async Task Request(string LogText, DateTime Archivedate)
        {
            SQLiteConnection connection = null;
            SQLiteTransaction transaction = null;
            try
            {
                connection = _pollSQLiteConnect.PoolOpen();
                transaction = connection.BeginTransaction();
                string command = "UPDATE LogBase SET LogText = '[ARCHIVED]' || @LogText WHERE Date < @Archivedate";

                using (var sqlcommand = new SQLiteCommand(command, connection, transaction))
                {
                    sqlcommand.Parameters.AddWithValue("@Archivedate", Archivedate);
                    sqlcommand.Parameters.AddWithValue("@LogText", LogText);

                    var result = await sqlcommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                    await transaction.CommitAsync().ConfigureAwait(false);

                    if (result != null)
                    {
                        if (result > 0)
                        {
                            _logger.LogInformation("Успешный Update!");
                        }
                        else
                        {
                            _logger.LogInformation("Не удалось обновить!");
                        }
                    }
                }
            }
            catch (SQLiteException ex)
            {
                _logger.LogInformation("Возникло исключение при работе с бд" + ex.Message + ex.StackTrace);
                if (transaction != null)
                {
                    await (transaction.RollbackAsync() ?? Task.CompletedTask).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Возникло исключение" + ex.Message + ex.StackTrace);
                if (transaction != null)
                {
                    await (transaction.RollbackAsync() ?? Task.CompletedTask).ConfigureAwait(false);
                }
            }
            finally
            {
                transaction?.Dispose();
                if (connection != null)
                {
                    _pollSQLiteConnect.PullClose(connection);
                }           
            }
        }
    }
}
