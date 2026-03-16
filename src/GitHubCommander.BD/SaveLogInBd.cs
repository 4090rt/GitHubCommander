using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubComander.src.GitHubCommander.BD
{
    public class SaveLogInBd
    {
        private readonly ILogger<SaveLogInBd> _logger;
        private readonly SaveCommandLog _saveLog;

        public SaveLogInBd(ILogger<SaveLogInBd> logger, SaveCommandLog saveLog)
        {
            _logger = logger;
            _saveLog = saveLog;
        }

        public async Task Saved(string logs, DateTime dateTime)
        {
            try
            {
                if (string.IsNullOrEmpty(logs))
                {
                    _logger.LogWarning("Пустое сообщение лога, сохранение отменено");
                    return;
                }

                var objects = new List<(string loginfo, DateTime date)>
                {
                    (logs, dateTime)
                };

                await _saveLog.SaveLogg(objects);
            }
            catch (SQLiteException ex)
            {
                _logger.LogError(ex, "Возникло исключение при работе с БД");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Возникло исключение при сохранении лога");
            }
        }
    }
}
