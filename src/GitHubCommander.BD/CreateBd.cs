using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubComander.src.GitHubCommander.BD
{
    public class CreateBd
    {
        private readonly ILogger _logger;
        private bool? _isCheckedCreate;
        private readonly PollSQLiteConnect _poolSQLiteConnect;

        public CreateBd(ILogger logger, PollSQLiteConnect poolSQLiteConnect)
        {
            _logger = logger;
            _poolSQLiteConnect = poolSQLiteConnect;
        }

        public async Task Proverka()
        {
            if (_isCheckedCreate == true) return;

            bool created = await BdNew();

            if (created == false)
            {
                _logger.LogWarning("Не удалось создать БД при проверке");
            }
            
            _isCheckedCreate = true;
        }

        public async Task<bool> BdNew()
        {
            SQLiteConnection connection = null;

            try
            {
                connection = _poolSQLiteConnect.PoolOpen();

                string createTableCommand = @"
                    CREATE TABLE IF NOT EXISTS LogBase (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Log TEXT NOT NULL,
                        Date TEXT NOT NULL
                    )";

                using (var cmd = new SQLiteCommand(createTableCommand, connection))
                {
                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                    _logger.LogInformation("БД создана/проверена успешно");
                }
                return true;
            }
            catch (SQLiteException ex)
            {
                _logger.LogError(ex, "Возникло исключение при работе с БД");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при попытке создания БД");
                return false;
            }
            finally
            {
                if (connection != null)
                {
                    _poolSQLiteConnect.PullClose(connection);
                }
            }
        }
    }
}
