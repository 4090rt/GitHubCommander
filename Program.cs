// See https://aka.ms/new-console-template for more information
using GithubComander.src.GitHubCommander.Data;
using GithubComander.src.GitHubCommander.Infrastructure;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Fallback;
using Polly.Timeout;
using System;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Security;
using System.Text;
using System.Web;
class program
{
    private readonly HttpPutRequest _putRequest;

    public program(HttpPutRequest putRequest)
    { 
        _putRequest = putRequest;
    }
    static async Task Main(string[] args)
    {
        var service = new ServiceCollection();

        service.AddLogging(build =>
        {
            build.AddConsole();
            build.ClearProviders();
            build.SetMinimumLevel(LogLevel.Information);
        });
        service.AddMemoryCache();
        service.AddSingleton<GitParser1>();
        service.AddSingleton<HttpRequest>();
        service.AddSingleton<HttpRequest2>();
        service.AddSingleton<HttpRequest3>();
        service.AddSingleton<DataModelRepositoryInfo>();
        service.AddSingleton<FileContent>();
        service.AddSingleton<RepositoryContent>();
        service.AddSingleton<ViborRepo>();
        service.AddSingleton<ShowReposInreposInfiles>();
        service.AddSingleton<HttpPutRequest>();

        service.AddHttpClient("GithubApiClient1", client1 =>
        {
            client1.DefaultRequestHeaders.Add("User-Agent", "GitHubCommander/1.0");
            client1.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
            client1.BaseAddress = new Uri("https://api.github.com/");
            var token1 = Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? throw new InvalidOperationException("GITHUB_TOKEN not set");
            client1.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("token", token1);

            client1.DefaultRequestVersion = HttpVersion.Version20;
            client1.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;

        }).AddTransientHttpErrorPolicy(polly =>
        polly.CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(60),
            onBreak: (outcome, timespan) =>
            {
                Console.WriteLine($"🔌 Circuit opened for {timespan}");
            },
            onReset: () =>
            {
                Console.WriteLine("✅ Circuit reset");
            },
            onHalfOpen: () =>
            {
                Console.WriteLine("⚠️ Circuit half-open");
            }
            ))
        .AddTransientHttpErrorPolicy(policy =>
        policy.WaitAndRetryAsync(3, retrycount =>
        TimeSpan.FromSeconds(Math.Pow(2, retrycount)) +
        +TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100)),
        onRetry: (outcome, timespan, retryCount, context) =>
        {
            Console.WriteLine($"🔄 Retry {retryCount} after {timespan}");
        }))
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            EnableMultipleHttp2Connections = true,

            PooledConnectionLifetime = TimeSpan.FromMinutes(15),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(10),

            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate | System.Net.DecompressionMethods.Brotli,

            MaxConnectionsPerServer = 10,
            UseCookies = false,
            AllowAutoRedirect = false,
        })
        .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(
            TimeSpan.FromSeconds(10),
            Polly.Timeout.TimeoutStrategy.Pessimistic,
            onTimeoutAsync: (context, timespan, task) =>
            {
                Console.WriteLine($"⏰ Request timed out after {timespan}");
                return Task.CompletedTask;
            }));


        service.AddHttpClient("GithubApiClient2", client2 =>
        {
            client2.Timeout = TimeSpan.FromSeconds(30);
            client2.DefaultRequestHeaders.Add("User-Agent", "GitHubCommander/1.0");
            client2.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
            client2.BaseAddress = new Uri("https://api.github.com/");
            var token2 = Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? throw new InvalidOperationException("GITHUB_TOKEN not set");
            client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", token2);
        }).AddTransientHttpErrorPolicy(policy =>
        policy.CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromMinutes(1),
            onBreak: (outcome, timespan) =>
            {
                Console.WriteLine($"🔌 Circuit opened for {timespan}");
            },
            onHalfOpen: () =>
            {
                Console.WriteLine("⚠️ Circuit half-open");
            },
            onReset: () =>
            {
                Console.WriteLine("✅ Circuit reset");
            }))
        .AddTransientHttpErrorPolicy(polly =>
        polly.WaitAndRetryAsync(3, retrycount =>
        TimeSpan.FromSeconds(Math.Pow(2, retrycount)) +
        TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100)),
        onRetry: (outcome, timespan, retryCount, context) =>
        {
            Console.WriteLine($"🔄 Retry {retryCount} after {timespan}");
        }))
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            EnableMultipleHttp2Connections = true,

            PooledConnectionLifetime = TimeSpan.FromSeconds(15),
            PooledConnectionIdleTimeout = TimeSpan.FromSeconds(10),

            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,

            MaxConnectionsPerServer = 10,
            UseCookies = false,
            AllowAutoRedirect = false,
        }).AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(
            TimeSpan.FromSeconds(10),
            Polly.Timeout.TimeoutStrategy.Pessimistic,
            onTimeoutAsync: (context, timespan, task) =>
            {
                Console.WriteLine($"⏰ Request timed out after {timespan}");
                return Task.CompletedTask;
            }));

        service.AddHttpClient("GithubApiClientPut", client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "GitHubCommander/1.0");
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
            client.BaseAddress = new Uri("https://api.github.com/");
            var tokenPut = Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? throw new InvalidOperationException("GITHUB_TOKEN not set");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("token", tokenPut);


            client.DefaultRequestVersion = HttpVersion.Version20;
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
        }).AddTransientHttpErrorPolicy(polly =>
        polly.CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromMinutes(1),
            onBreak: (outcome, timespan) =>
            {
                Console.WriteLine($"🔌 Circuit opened for {timespan}");
            },
            onHalfOpen: () =>
            {
                Console.WriteLine("⚠️ Circuit half-open");
            },
            onReset: () =>
            {
                Console.WriteLine("✅ Circuit reset");
            }
            )).AddTransientHttpErrorPolicy(policy =>
            policy.WaitAndRetryAsync(3, retrycount =>
            TimeSpan.FromSeconds(Math.Pow(2, retrycount)) +
            TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100)),
            onRetry: (outcome, timespan, retrycount, context) =>
            {
                Console.WriteLine($"🔄 Retry {retrycount} after {timespan}");
            })).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                EnableMultipleHttp2Connections = true,

                PooledConnectionLifetime = TimeSpan.FromMinutes(15),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(10),

                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,

                MaxConnectionsPerServer = 10,
                UseCookies = false,
                AllowAutoRedirect = false,
            }).AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(
                TimeSpan.FromSeconds(60),
                Polly.Timeout.TimeoutStrategy.Pessimistic,
                onTimeoutAsync: (context, timespan, task) =>
                {
                    Console.WriteLine($"⏰ Request timed out after {timespan}");
                    return Task.CompletedTask;
                }));

        var ServicePrivoder = service.BuildServiceProvider();
        var servicec1 = ServicePrivoder.GetRequiredService<HttpRequest>();
        var servicec2 = ServicePrivoder.GetRequiredService<HttpRequest2>();
        var servicec3 = ServicePrivoder.GetRequiredService<HttpRequest3>();
        var services4 = ServicePrivoder.GetRequiredService<HttpPutRequest>();

        await RunNavigator(servicec1, servicec2, servicec3, services4);



        static async Task RunNavigator(HttpRequest request1, HttpRequest2 request2, HttpRequest3 request3, HttpPutRequest request4)
        {
            Console.Clear();

            string currentOwner = "";
            string currentRepo = "";
            string currentPath = "";

            while (true)
            {
                if (string.IsNullOrEmpty(currentRepo))
                {
                    ShowReposInreposInfiles showReposInreposInfiles = new ShowReposInreposInfiles();
                    await showReposInreposInfiles.ShowRepositoryes(request1).ConfigureAwait(false);
                }
                else
                {
                    ShowReposInreposInfiles showReposInreposInfiles = new ShowReposInreposInfiles();
                    await showReposInreposInfiles.ShowContents(request2, currentOwner, currentRepo, currentPath);
                }
                Console.WriteLine("\n═══════════════════════════════════════════");
                Console.WriteLine("0 - Назад | q - Выход");
                Console.Write("Введите номер: ");

                string numb = Console.ReadLine();

                if (numb.ToLower() == "q")
                    break;

                if (numb == "0")
                {
                    if (!string.IsNullOrEmpty(currentPath))
                    {
                        // Поднимаемся на уровень вверх
                        int slash = currentPath.LastIndexOf("/");
                        currentPath = slash > 0 ? currentPath.Substring(0, slash) : "";
                    }
                    else if (!string.IsNullOrEmpty(currentRepo))
                    {
                        // Выходим из репозитория к списку репозиториев
                        currentRepo = "";
                        currentOwner = "";
                        currentPath = "";
                    }
                    continue;
                }

                if (numb.StartsWith("commit "))
                {
                    string param = numb.Substring(7).Trim();
                    var parts = param.Split(' ', 2);
                    string repoPath = parts[0].Trim();
                    string localPath = parts.Length > 1 ? parts[1].Trim() : null;

                    string filePath = string.IsNullOrEmpty(currentPath)
                      ? repoPath
                      : $"{currentPath}/{repoPath}";

                    await HandleCommit(request4, request3, currentOwner, currentRepo, filePath, localPath);
                    continue;
                }

                if (int.TryParse(numb, out int number))
                {
                    if (string.IsNullOrEmpty(currentRepo))
                    {
                        ViborRepo viborRepo = new ViborRepo();
                        var (owner, repo) = await viborRepo.HandleRepositoryChoice(request1, number);
                        if (!string.IsNullOrEmpty(owner) && !string.IsNullOrEmpty(repo))
                        {
                            currentOwner = owner;
                            currentRepo = repo;
                            currentPath = "";
                        }
                    }
                    else
                    {
                        ViborRepo viborRepo = new ViborRepo();
                        currentPath = await viborRepo.HandleContentChoice(request2, request3, currentOwner, currentRepo, currentPath, number);
                    }
                }
            }
        }

        static async Task HandleCommit(HttpPutRequest gitHubService,HttpRequest3 gitHubService2,string owner,string repo,string filePath,string localPath)
        {
            Console.Clear();
            Console.WriteLine($"📄 Обновление: {filePath}");
            Console.WriteLine($"Владелец: {owner}, Репозиторий: {repo}");

            // Получаем SHA файла (может быть null, если файл не существует)
            var files = await gitHubService2.CacheRequest(owner, repo, filePath);
            var file = files?.FirstOrDefault();
            string? sha = file?.Sha;
            
            Console.WriteLine($"SHA: {(sha ?? "null (новый файл)")}");
            Console.WriteLine($"Локальный файл: {localPath}");

            // Проверяем локальный файл
            if (localPath == null || !File.Exists(localPath))
            {
                Console.WriteLine($"❌ Файл не найден: {localPath}");
                Console.ReadKey();
                return;
            }

            // Читаем содержимое
            string newcontent = await File.ReadAllTextAsync(localPath).ConfigureAwait(false);
            Console.WriteLine($"Размер файла: {newcontent.Length} байт");

            // Отправляем на GitHub (создание или обновление)
            var (success, errorMessage) = await gitHubService.UpdateFileAsync(
                owner,
                repo,
                filePath,
                newcontent,
                sha != null ? $"Update {filePath}" : $"Create {filePath}",
                sha
            );

            Console.WriteLine(success ? "✅ Готово!" : $"❌ Ошибка: {errorMessage}");
            Console.WriteLine("Нажмите Enter для продолжения...");
            Console.ReadKey();
        }
    }
}

