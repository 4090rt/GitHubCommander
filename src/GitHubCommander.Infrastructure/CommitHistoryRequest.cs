using GithubComander.src.GitHubCommander.Data;
using GithubComander.src.GitHubCommander.Infrastructure.Delegates;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GithubComander.src.GitHubCommander.Infrastructure
{
    public class CommitHistoryRequest
    {
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _memorycache;
        private readonly Microsoft.Extensions.Logging.ILogger<CommitHistoryRequest> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GitParser1 _parser;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        public CommitHistoryRequest(Microsoft.Extensions.Caching.Memory.IMemoryCache memorycache, Microsoft.Extensions.Logging.ILogger<CommitHistoryRequest> logger, IHttpClientFactory httpClientFactory, GitParser1 parser)
        {
            _httpClientFactory = httpClientFactory;
            _memorycache = memorycache;
            _logger = logger;
            _parser = parser;
        }

        public async Task<GitHubCommit> RequestCache(string owner, string repo, string path = "", CancellationToken cancellation = default)
        {

        }

        public async Task<GitHubCommit> Request(string owner, string repo, string path = "", CancellationToken cancellation = default)
        {
            try
            { 
                var client = _httpClientFactory.CreateClient("GithubApiClient1");

                var options = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{owner}/{repo}/commits")
                {
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
                };

                _logger.LogInformation("Начинаю запрос коммитов");
                var timer = System.Diagnostics.Stopwatch.StartNew();
                using HttpResponseMessage recpon = await client.SendAsync(options, cancellation).ConfigureAwait(false);
                timer.Stop();
                _logger.LogInformation($"Ответ получен за {timer.Elapsed.Seconds}сек.");
                if (recpon.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Начинаю чтение ответа");
                    var timer2 = System.Diagnostics.Stopwatch.StartNew();
                    var readresult = await recpon.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    timer2.Stop();
                    _logger.LogInformation($"Ответ прочитан за {timer2}сек.");

                    _logger.LogInformation("Начинаю парсинг ответа");
                    var timer3 = System.Diagnostics.Stopwatch.StartNew();
                    var parsed = await _parser.ParsedCommit(readresult).ConfigureAwait(false);
                    timer3.Stop();
                    _logger.LogInformation($"Ответ распаршен за {timer3}сек.");

                    if (parsed != null && parsed is GitHubCommit resulttype)
                    {
                        return resulttype;
                    }
                    else
                    {
                        _logger.LogError("Данные о коммитах не найдены или имеют невалидный формат!");
                        return new GitHubCommit();
                    }
                }
                else
                {
                    _logger.LogError("Возникла ошибка запрос" + recpon.StatusCode);
                    return new GitHubCommit();
                }
            }
            catch (TaskCanceledException ex) when (cancellation.IsCancellationRequested)
            {
                _logger.LogError(ex, "Операция отменена ");
                return new GitHubCommit();
            }
            catch (TaskCanceledException ex) when (!cancellation.IsCancellationRequested)
            {
                _logger.LogError(ex, "Операция отменена ");

                return new GitHubCommit();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Возникло исключение  при запросе");

                return new GitHubCommit();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Возникло исключение");

                return new GitHubCommit();
            }

        }
    }
}
