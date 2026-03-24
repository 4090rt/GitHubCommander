using GithubComander.src.GitHubCommander.BD;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubComander.src.GitHubCommander.Infrastructure.Delegates
{
    public class HttpRequestDelegate
    {
        private readonly ILogger<HttpRequestDelegate> _logger;
        private readonly GitParser1 _parser;
        private readonly SaveLogInBd _saveLogInBd;

        public delegate Task<T> HttpRequests<T>(HttpClient client, HttpRequestMessage message, CancellationToken cancellation = default);

        public HttpRequestDelegate(ILogger<HttpRequestDelegate> logger, GitParser1 parser, SaveLogInBd saveLogInBd)
        {
            _logger = logger;
            _parser = parser;
            _saveLogInBd = saveLogInBd;

        }

        public async Task<T?> RunRequest<T>(Func<Task<T>> DELEGATE, HttpClient client, CancellationToken cancellation = default)
        {
            var result = await DELEGATE.Invoke();
            return result;
        }

        public async Task<T?> HttpRequestGit<T>(HttpClient client, HttpRequestMessage message, CancellationToken cancellation = default)
        {
            try
            {
                string log = "Начинаю выполнение запроса";
                _logger.LogInformation(log);
                await _saveLogInBd.Saved(log, DateTime.Now);

                var timer = System.Diagnostics.Stopwatch.StartNew();
                using HttpResponseMessage recpon = await client.SendAsync(message, cancellation).ConfigureAwait(false);
                timer.Stop();
                string log2 = $"Запрос выполнен за {timer}";
                _logger.LogInformation(log2);
                await _saveLogInBd.Saved(log2, DateTime.Now);
                if (recpon.IsSuccessStatusCode)
                {
                    string log3 = "Начинаю чтение ответа";
                    _logger.LogInformation(log3);
                    await _saveLogInBd.Saved(log3, DateTime.Now);
                    var timer2 = System.Diagnostics.Stopwatch.StartNew();
                    var content = await recpon.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    timer2.Stop();
                    string log4 = $"Ответ прочитан за {timer2}";
                    _logger.LogInformation(log4);
                    await _saveLogInBd.Saved(log4, DateTime.Now);

                    string log5 = "Начинаю парсинг ответа";
                    _logger.LogInformation(log5);
                    await _saveLogInBd.Saved(log5, DateTime.Now);
                    var timer3 = System.Diagnostics.Stopwatch.StartNew();
                    var result = await _parser.Parsed(content);
                    timer3.Stop();
                    string log6 = $"Ответ распаршен за {timer3}";
                    _logger.LogInformation(log6);
                    await _saveLogInBd.Saved(log6, DateTime.Now);
                    if (result is T typedResult)
                    {
                        return typedResult;
                    }
                }
                else
                {
                    _logger.LogError("Возникла ошибка при попытке запроса. Статус код: {StatusCode}", recpon.StatusCode);
                }
            }
            catch (TaskCanceledException ex) when (!cancellation.IsCancellationRequested)
            {
                _logger.LogError(ex, "Операция отменена");
            }
            catch (TaskCanceledException ex) when (cancellation.IsCancellationRequested)
            {
                _logger.LogError(ex, "Операция отменена пользователем");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Возникло исключение при выполнении запроса");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Возникло исключение");
            }
            return default(T);
        }
    }
}
