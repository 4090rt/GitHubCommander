using GithubComander.src.GitHubCommander.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GithubComander.src.GitHubCommander.Infrastructure
{
    public class GitParser1
    {
        public readonly Microsoft.Extensions.Logging.ILogger<GitParser1> _logger;

        public GitParser1(Microsoft.Extensions.Logging.ILogger<GitParser1> logger)
        { 
            _logger = logger;
        }
        public async Task<List<DataModelRepositoryInfo>> Parsed(Stream stream)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var result = await JsonSerializer.DeserializeAsync<List<DataModelRepositoryInfo>>(stream, options);

                if (result != null)
                {
                    _logger.LogInformation("Успешно распаршено");
                    return result;
                }
                else
                {
                    _logger.LogInformation("объект парсинга не найден");
                    return new List<DataModelRepositoryInfo>();
                }
            }
            catch (JsonException ex) 
            {
                _logger.LogError(ex, "Ошибка формата JSON");
                return new List<DataModelRepositoryInfo>();
            }
            catch (Exception ex) 
            {
                _logger.LogError("Возникло исключение" + ex.Message + ex.StackTrace);
                return new List<DataModelRepositoryInfo>();
            }
        }
    }
}
