using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GithubComander.src.GitHubCommander.Infrastructure
{
    public class HttpPutRequest
    {
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _memorycache;
        private readonly Microsoft.Extensions.Logging.ILogger<HttpPutRequest> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GitParser1 _parser;
        public HttpPutRequest(Microsoft.Extensions.Caching.Memory.IMemoryCache memorycache, Microsoft.Extensions.Logging.ILogger<HttpPutRequest> logger, IHttpClientFactory httpClientFactory, GitParser1 parser)
        {
            _httpClientFactory = httpClientFactory;
            _memorycache = memorycache;
            _logger = logger;
            _parser = parser;
        }
        public async Task<bool> UpdateFileAsync(string owner, string repo, string path, string newContent, string commitMessage, string? sha = null)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("GithubApiClientPut");

                var options = new HttpRequestMessage(HttpMethod.Put, $"/repos/{owner}/{repo}/contents/{path}")
                {
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
                };

                byte[] bytecontent = Encoding.UTF8.GetBytes(newContent);
                string base64content = Convert.ToBase64String(bytecontent);

                // Если sha есть — обновляем, если нет — создаём
                object requestbody = sha != null
                    ? new { message = commitMessage, content = base64content, sha }
                    : new { message = commitMessage, content = base64content };

                var json = JsonSerializer.Serialize(requestbody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var httpcontent = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation($"📝 Сообщение: {commitMessage}, SHA: {(sha ?? "null (создание)")}");

                options.Content = httpcontent;
                using var recpon = await client.SendAsync(options);

                if (recpon.IsSuccessStatusCode)
                {
                    string action = sha != null ? "обновлен" : "создан";
                    _logger.LogInformation($"✅ Файл {path} {action}");

                    // Инвалидируем кэш
                    string cacheKey = $"cached_key{owner}{repo}{path}";
                    _memorycache.Remove(cacheKey);
                    _memorycache.Remove($"stale:{cacheKey}");

                    return true;
                }
                else
                {
                    string errorBody = await recpon.Content.ReadAsStringAsync();
                    _logger.LogError($"❌ Ошибка {recpon.StatusCode}: {errorBody}");

                    // Специфичные ошибки
                    if (recpon.StatusCode == System.Net.HttpStatusCode.Conflict)
                    {
                        _logger.LogError("Conflict: файл был изменен на GitHub");
                    }
                    else if (recpon.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _logger.LogError("NotFound: путь не существует");
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Исключение при обновлении {path}");
                return false;
            }
        }
    }
}
