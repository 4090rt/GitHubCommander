using Microsoft.Extensions.Logging;
using System.Data.SQLite;

namespace GithubComander.src.GitHubCommander.BD
{
    public class DeleteLogs
    {
        private readonly ILogger _logger;
        private readonly PollSQLiteConnect _pollSQLiteConnect;

        public DeleteLogs(ILogger logger, PollSQLiteConnect pollSQLiteConnect)
        {
            _logger = logger;
            _pollSQLiteConnect = pollSQLiteConnect;
        }

        public async Task<bool> DeleteInBD()
        {
            DateTime dateInterval = DateTime.Now.AddDays(-7);
            SQLiteConnection connection = null;
            SQLiteTransaction transaction = null;
            try
            {
                connection = _pollSQLiteConnect.PoolOpen();

                transaction = connection.BeginTransaction();

                string command = "DELETE FROM LogBase WHERE Date < @DateInterval";

                using (var sqlcommand = new SQLiteCommand(command, connection, transaction))
                {
                    sqlcommand.Parameters.AddWithValue("@DateInterval", dateInterval);

                    await sqlcommand.ExecuteNonQueryAsync().ConfigureAwait(false);

                    await transaction.CommitAsync().ConfigureAwait(false);
                    _logger.LogInformation("Плановое удаление прошло успешно");
                    return true;
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
                await (transaction?.RollbackAsync() ?? Task.CompletedTask).ConfigureAwait(false);
                return false;
            }
            finally
            {
                if (connection != null)
                {
                    _pollSQLiteConnect.PullClose(connection);
                }
                transaction?.Dispose();
            }
        }
    }
}
