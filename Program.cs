// See https://aka.ms/new-console-template for more information
using GithubComander.src.GitHubCommander.Data;
using GithubComander.src.GitHubCommander.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Security;
class program
{
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
        service.AddSingleton<DataModelRepositoryInfo>();
        service.AddSingleton<FileContent>();
        service.AddSingleton<RepositoryContent>();

        service.AddHttpClient("GithubApiClient", client =>
        {
            client.Timeout = TimeSpan.FromMinutes(60);
            client.DefaultRequestHeaders.Add("User-Agent", "GitHubCommander/1.0");
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
            client.BaseAddress = new Uri("https://api.github.com/");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("token", "");

        }).AddTransientHttpErrorPolicy(polly =>
        polly.CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30)))
        .AddTransientHttpErrorPolicy(policy =>
        policy.WaitAndRetryAsync(3, retrycount =>
        TimeSpan.FromSeconds(Math.Pow(2, retrycount))))
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            EnableMultipleHttp2Connections = true,

            PooledConnectionLifetime = TimeSpan.FromMinutes(15),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(10),

            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate | System.Net.DecompressionMethods.Brotli,

            MaxConnectionsPerServer = 10,
            UseCookies = false,
            AllowAutoRedirect = false,
        });
        var serviceProvider = service.BuildServiceProvider();
        var httpRequest = serviceProvider.GetRequiredService<HttpRequest>();
    }
    // показать список репозиториев
    static async Task ShowRepositoryes(HttpRequest gitHubService)
    {
        Console.WriteLine("Начинаю Запрос репозиториев");

        Console.WriteLine("Твои репозитории:");

        var item = await gitHubService.Request();

        for (int i = 0; i < item.Count; i++)
        {
            var items = item[i];
            Console.WriteLine($"[{i + 1}] {items.FullName} ★ {items.StargazersCount}");
            if (!string.IsNullOrEmpty(items.Description))
            {
                Console.WriteLine($"    {items.Description}");
            }
        }
    }
    // показать содержимое репозитория
    static async Task ShowContents(HttpRequest2 gitHubService, string owner, string repo, string path)
    {
        if (string.IsNullOrEmpty(path))
            Console.WriteLine($"📁 {owner}/{repo} (корень)\n");
        else
            Console.WriteLine($"📁 {owner}/{repo}/{path}\n");
        
        var items = await gitHubService.CacheRequest(owner, repo, path);

        if (items == null)
        {
            Console.WriteLine("Файлы не найдены");
            return;
        }

        for (int i = 0; i < items.Count; i++)
        { 
            var item = items[i];
            string icon = item.IsDirectory ? "📁" : "📄";
            string size = item.IsFile ? $" ({FormatSize(item.Size)})" : "";
            Console.WriteLine($"[{i + 1}] {icon} {item.Name}{size}");
        }
    }
    // показать файл
    static async Task ShowFile(HttpRequest3 gitHubService, string owner, string repo, string path)
    {
        Console.Clear();
        Console.WriteLine($"📄 {path}\n");

        var file = await gitHubService.CacheRequest(owner, repo, path);

        if (file != null)
        {
            string content = file.GetDecodedContent();
            Console.WriteLine(content);
        }
        else
        {
            Console.WriteLine("❌ Не удалось прочитать файл");
        }

        Console.WriteLine("\n═══════════════════════════════════════════");
        Console.WriteLine("Нажмите Enter чтобы вернуться...");
        Console.ReadLine();
    }

    // обработка выбора репозитория
    static async Task<(string owner, string repo)> HandleRepositoryChoice(HttpRequest gitHubService, int number)
    {
        var repositories = await gitHubService.CachingRequest();

        if (number < 1 || number > repositories.Count)
        {
            Console.WriteLine("❌ Неверный номер. Нажмите Enter...");
            Console.ReadLine();
            return (null, null);
        }

        var selected = repositories[number - 1];
        var parts = selected.FullName.Split('/');

        return (parts[0], parts[1]);
    }

    // выбор папки или файла
    static async Task<string> HandleContentChoice(HttpRequest2 gitHubService,HttpRequest3 GI, string owner, string repo, string currentPath, int number)
    {
        var items = await gitHubService.CacheRequest(owner, repo, currentPath);

        if (number < 1 || number > items.Count)
        {
            Console.WriteLine("❌ Неверный номер. Нажмите Enter...");
            Console.ReadLine();
            return currentPath;
        }

        var selected = items[number - 1];

        if (selected.IsDirectory)
        {
            return string.IsNullOrEmpty(currentPath)
                ? selected.Name
                : $"{currentPath}/{selected.Name}";
        }
        else
        {
            // Показываем файл
            await ShowFile(GI, owner, repo, selected.Path);
            return currentPath;
        }
    }

    static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}