// See https://aka.ms/new-console-template for more information
using GithubComander.src.GitHubCommander.Data;
using GithubComander.src.GitHubCommander.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Net.Http.Headers;
using System.Net.Security;

Console.WriteLine("Hello, World!");
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

Console.WriteLine("Запрашиваю список репозиториев...");
var repositories = await httpRequest.CachingRequest("/user/repos");

Console.WriteLine($"\nНайдено репозиториев: {repositories.Count}\n");

for (int i = 0; i < repositories.Count; i++)
{
    var repo = repositories[i];
    Console.WriteLine($"[{i + 1}] {repo.Name} ★ {repo.StargazersCount}");
    if (!string.IsNullOrEmpty(repo.Description))
    {
        Console.WriteLine($"    {repo.Description}");
    }
    Console.WriteLine($"    Обновлен: {repo.UpdatedAt:yyyy-MM-dd}");
    Console.WriteLine();
}