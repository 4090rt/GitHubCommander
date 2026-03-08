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

        public delegate Task<T> HttpRequests<T>(HttpClient client, HttpRequestMessage message, CancellationToken cancellation = default);

        public HttpRequestDelegate(ILogger<HttpRequestDelegate> logger, GitParser1 parser)
        {
            _logger = logger;
            _parser = parser;
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
                _logger.LogInformation("Начинаю выполнение запроса");
                var timer = System.Diagnostics.Stopwatch.StartNew();
                using HttpResponseMessage recpon = await client.SendAsync(message, cancellation).ConfigureAwait(false);
                timer.Stop();
                _logger.LogInformation($"Запрос выполнен за {timer}");
                if (recpon.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Начинаю чтение ответа");
                    var timer2 = System.Diagnostics.Stopwatch.StartNew();
                    var content = await recpon.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    timer2.Stop();
                    _logger.LogInformation($"Ответ прочитан за {timer2}");

                    _logger.LogInformation("Начинаю парсинг ответа");
                    var timer3 = System.Diagnostics.Stopwatch.StartNew();
                    var result = await _parser.Parsed(content);
                    timer3.Stop();
                    _logger.LogInformation($"Ответ распаршен за {timer3}");

                    if (result is T typedResult)
                    {
                        return typedResult;
                    }
                }
                else
                {
                    _logger.LogError("Возникла ошибка при попытке запроса. статус код:" + recpon.StatusCode);
                }
            }
            catch (TaskCanceledException ex) when (!cancellation.IsCancellationRequested)
            {
                _logger.LogError("Операция отменена" + ex.Message + ex.StackTrace);
            }
            catch (TaskCanceledException ex) when (cancellation.IsCancellationRequested)
            {
                _logger.LogError("Операция отменена пользователем" + ex.Message + ex.StackTrace);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError("Возникло исключение при выполнении запроса" + ex.Message + ex.StackTrace);
            }
            catch (Exception ex)
            {
                _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
            }
            return default(T);
        }
    }
}
