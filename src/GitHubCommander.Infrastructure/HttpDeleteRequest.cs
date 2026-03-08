using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GithubComander.src.GitHubCommander.Infrastructure
{
    public class HttpDeleteRequest
    {
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _memorycache;
        private readonly Microsoft.Extensions.Logging.ILogger<HttpDeleteRequest> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GitParser1 _parser;

        public HttpDeleteRequest(Microsoft.Extensions.Caching.Memory.IMemoryCache memorycache, Microsoft.Extensions.Logging.ILogger<HttpDeleteRequest> logger, IHttpClientFactory httpClientFactory, GitParser1 parser)
        {
            _httpClientFactory = httpClientFactory;
            _memorycache = memorycache;
            _logger = logger;
            _parser = parser;
        }

        public async Task<(bool Success, string ErrorMessage)> DeleteFileAsync(string owner, string repo, string path, string commitMessage, string sha)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("GithubApiClientDelete");

                var options = new HttpRequestMessage(HttpMethod.Delete, $"/repos/{owner}/{repo}/contents/{path}")
                {
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
                };

                var requestbody = new
                {
                    message = commitMessage,
                    sha = sha
                };

                var json = JsonSerializer.Serialize(requestbody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation($"📝 Сообщение: {commitMessage}, SHA: {(sha ?? "null (создание)")}");

                options.Content = content;
                using var request = await client.SendAsync(options).ConfigureAwait(false);

                if (request.IsSuccessStatusCode)
                {
                    string action = "Файл удален!";
                    _logger.LogInformation($"✅ Файл {path} {action}");
                    string cacheKey = $"cached_key{owner}{repo}{path}";
                    _memorycache.Remove(cacheKey);
                    _memorycache.Remove($"stale:{cacheKey}");
                    return (true, "");
                }
                else
                {
                    var error = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
                    string errorMsg = $"❌ Ошибка {request.StatusCode}: {error}";
                    _logger.LogError(errorMsg);

                    // Специфичные ошибки
                    if (request.StatusCode == System.Net.HttpStatusCode.Conflict)
                    {
                        _logger.LogError("Conflict: файл был изменен на GitHub");
                        errorMsg += " | Conflict: файл был изменен на GitHub";
                    }
                    else if (request.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _logger.LogError("NotFound: путь не существует");
                        errorMsg += " | NotFound: путь не существует";
                    }

                    return (false, errorMsg);
                }
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError("Операция отменена" + ex.Message + ex.StackTrace);
                return (false, $"Отменено: {ex.Message}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError("Возниклои исключение при выполнении запроса" + ex.Message + ex.StackTrace);
                return (false, $"Исключение: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Исключение при обновлении {path}");
                return (false, $"Исключение: {ex.Message}");
            }
        }
    }
}
