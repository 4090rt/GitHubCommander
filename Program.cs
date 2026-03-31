// See https://aka.ms/new-console-template for more information
using GithubComander.src.GitHubCommander.BD;
using GithubComander.src.GitHubCommander.Data;
using GithubComander.src.GitHubCommander.Infrastructure;
using GithubComander.src.GitHubCommander.Infrastructure.Delegates;
using GithubComander.src.GitHubCommander.Infrastructure.EthernetStat;
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
using System.Threading.Tasks;
using System.Web;

class Program
{
    private readonly HttpPutRequest _putRequest;


    public Program(HttpPutRequest putRequest)
    { 
        _putRequest = putRequest;
    }
    static async Task Main(string[] args)
    {
        var service = new ServiceCollection();

        service.AddLogging(build =>
        {
            build.AddConsole();
            build.SetMinimumLevel(LogLevel.Information);
        });
        service.AddMemoryCache();
        service.AddHttpClient();

        lientOptions options = new lientOptions();
        options.RunDelegate(options.Servis, service);
        options.RunDelegate(options.ServisEthernet, service);

        service.AddSingleton<Microsoft.Extensions.Logging.ILogger>(sp =>
            sp.GetRequiredService<ILoggerFactory>().CreateLogger("GithubComander"));

        service.AddSingleton<FallbackPolitic>();
        service.AddSingleton<GitParser1>();
        service.AddSingleton<Staleoptions>();
        service.AddSingleton<HttpRequestDelegate>();
        service.AddSingleton<Http2Options>();
        service.AddSingleton<HttpRequest>();
        service.AddSingleton<HttpRequest2>();
        service.AddSingleton<HttpRequest3>();
        service.AddSingleton<DataModelRepositoryInfo>();
        service.AddSingleton<FileContent>();
        service.AddSingleton<RepositoryContent>();
        service.AddSingleton<ViborRepo>();
        service.AddSingleton<ShowReposInreposInfiles>();
        service.AddSingleton<HttpPutRequest>();
        service.AddSingleton<HttpDeleteRequest>();
        service.AddSingleton<HttpRequestEthernet>();
        service.AddSingleton<PingRequest>();
        service.AddSingleton<JitterClass>();
        service.AddSingleton<PingRequestUS>();
        service.AddSingleton<PingRequestEu>();
        service.AddSingleton<PingRequestAsia>();
        // Регистрация сервисов БД
        service.AddSingleton<PollSQLiteConnect>();
        service.AddSingleton<CreateBd>();
        service.AddSingleton<SaveCommandLog>();
        service.AddSingleton<SaveLogInBd>();
        service.AddSingleton<SelectAll>();

    var ServicePrivoder = service.BuildServiceProvider();
        var servicec1 = ServicePrivoder.GetRequiredService<HttpRequest>();
        var servicec2 = ServicePrivoder.GetRequiredService<HttpRequest2>();
        var servicec3 = ServicePrivoder.GetRequiredService<HttpRequest3>();
        var services4 = ServicePrivoder.GetRequiredService<HttpPutRequest>();
        var services5 = ServicePrivoder.GetRequiredService<HttpDeleteRequest>();
        var services6 = ServicePrivoder.GetRequiredService<SaveLogInBd>();
        var services7 = ServicePrivoder.GetRequiredService<SelectAll>();
        var services8 = ServicePrivoder.GetRequiredService<HttpRequestEthernet>();
        var services9 = ServicePrivoder.GetRequiredService<PingRequest>();
        var services10 = ServicePrivoder.GetRequiredService<JitterClass>();
        var services11 = ServicePrivoder.GetRequiredService<PingRequestUS>();
        var services12 = ServicePrivoder.GetRequiredService<PingRequestEu>();
        var services13 = ServicePrivoder.GetRequiredService<PingRequestAsia>();
        // Инициализация БД
        var createBd = ServicePrivoder.GetRequiredService<CreateBd>();
        await createBd.Proverka();


        await RunNavigator(servicec1, servicec2, servicec3, services4, services5, services7,
            services8, services9, services10, services11, services12, services13);



        static async Task RunNavigator(HttpRequest request1, HttpRequest2 request2, HttpRequest3 request3,
            HttpPutRequest request4, HttpDeleteRequest request5, SelectAll select, HttpRequestEthernet ethernet, PingRequest pingRequest, JitterClass jitterClass,
            PingRequestUS pingRequestUS, PingRequestEu pingRequestEu, PingRequestAsia pingRequestAsia)
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
                if (numb == "pingserver")
                {
                    try
                    {
                        var result = await pingRequestUS.Request("https://api.github.com");
                        await Task.Delay(100);
                        var result2 = await pingRequestEu.Request("api.github.com");
                        await Task.Delay(100);
                        var result3 = await pingRequestAsia.Request("api.github.com");

                        if (result != null && result2 != null && result3 != null)
                        {
                            Console.WriteLine("-==Пинг до Us сервера github==-");

                            Console.WriteLine($"Host: {result.Host}");
                            Console.WriteLine($"PingMs: {result.PingMs}");
                            Console.WriteLine($"Status: {result.Status}");
                            Console.WriteLine($"Error: {result.Error}");

                            Console.WriteLine("-==Пинг до Eu сервера github==-");

                            Console.WriteLine($"Host: {result2.Host}");
                            Console.WriteLine($"PingMs: {result2.PingMs}");
                            Console.WriteLine($"Status: {result2.Status}");
                            Console.WriteLine($"Error: {result2.Error}");

                            Console.WriteLine("-==Пинг до Asia сервера github==-");

                            Console.WriteLine($"Host: {result3.Host}");
                            Console.WriteLine($"PingMs: {result3.PingMs}");
                            Console.WriteLine($"Status: {result3.Status}");
                            Console.WriteLine($"Error: {result3.Error}");

                            Console.WriteLine("Нажмите Enter для продолжения...");
                            Console.ReadKey();
                            Console.Clear();
                        }
                        else
                        {
                            Console.WriteLine("Не удалос получить информацию о сети");
                            await Task.Delay(10000);
                        }
                        continue;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Возникло исключени при попытке получения информации о соединении");
                        await Task.Delay(10000);
                        continue;
                    }

                }
                if (numb == "ping")
                {
                    try
                    {
                       var result = await ethernet.RequestCache().ConfigureAwait(false);
                       await Task.Delay(100);
                       var reusltping = await pingRequest.Request("speedtest.librespeed.org ").ConfigureAwait(false);
                       await Task.Delay(100);
                       var jitterresult = await jitterClass.JitterSc("google.com/generate_204", 5).ConfigureAwait(false);

                        if (result != null && reusltping != null && jitterresult != null)
                        {
                            foreach (var item in result)
                            {

                                foreach (var item2 in reusltping)
                                {
                                    Console.WriteLine("-==Информация о Провайдере==-");

                                    Console.WriteLine($"IP: {item.IP}");
                                    Console.WriteLine($"Hostname: {item.Hostname}");
                                    Console.WriteLine($"City: {item.City}");
                                    Console.WriteLine($"Region: {item.Region}");
                                    Console.WriteLine($"Country: {item.Country}");
                                    Console.WriteLine($"Location: {item.Loc}");
                                    Console.WriteLine($"Org: {item.Org}");
                                    Console.WriteLine($"Postal: {item.Postal}");
                                    Console.WriteLine($"Timezone: {item.Timezone}");
                                    Console.WriteLine($"Readme: {item.Readme}");
                                    Console.WriteLine("═══════════════════════════════════════════");

                                    Console.WriteLine("-==Информация о пинге==-");

                                    Console.WriteLine($"Host: {item2.Host}");
                                    Console.WriteLine($"PingMs: {item2.PingMs}");
                                    Console.WriteLine($"Status: {item2.Status}");
                                    Console.WriteLine($"Error: {item2.Error}");
                                    Console.WriteLine($"Maxping: {jitterresult.MaxMs}");
                                    Console.WriteLine($"Minping: {jitterresult.MinMS}");
                                    Console.WriteLine($"Averageping: {jitterresult.Average}");
                                    Console.WriteLine($"Jitterping: {jitterresult.JitterMs}");
                                    Console.WriteLine($"TimeJitterRequest: {jitterresult.Timer / 100}sec");

                                }
                            }

                            Console.WriteLine("Нажмите Enter для продолжения...");
                            Console.ReadKey();
                            Console.Clear();
                        }
                        else
                        {
                            Console.WriteLine("Не удалос получить информацию о сети");
                            await Task.Delay(10000);
                        }
                        continue;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Возникло исключени при попытке получения информации о соединении");
                        await Task.Delay(10000);
                        continue;
                    }
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
                if (numb.StartsWith("delete "))
                {
                    string param = numb.Substring(7).Trim();
                    var parts = param.Split(' ', 2);
                    string repoPath = parts[0].Trim();
                    string localPath = parts.Length > 1 ? parts[1].Trim() : null;

                    string filePath = string.IsNullOrEmpty(currentPath)
                   ? repoPath
                   : $"{currentPath}/{repoPath}";

                    await HandleCommit2(request5, request3, currentOwner, currentRepo, filePath, localPath);
                }
                if (numb.StartsWith("AllLogs"))
                {
                    try
                    {
                        Console.WriteLine("Вывожу количество логов в базе");
                        var logs = await select.Select();

                        if (logs == null)
                        {
                            Console.WriteLine("Логи отсутствуют");
                        }
                        Console.WriteLine($"Количество логов - {logs}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Возникло исключени про попытке вывода логов");
                        await Task.Delay(10000);
                        continue;
                    }
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

        static async Task HandleCommit2(HttpDeleteRequest gitHubService, HttpRequest3 gitHubService2, string owner, string repo, string filePath, string localPath)
        {
            Console.Clear();
            Console.WriteLine($"🗑️ Удаление: {filePath}");
            Console.WriteLine($"Владелец: {owner}, Репозиторий: {repo}");

            var files = await gitHubService2.CacheRequest(owner, repo, filePath);
            var file = files?.FirstOrDefault();
            string? sha = file?.Sha;

            if (sha == null)
            {
                Console.WriteLine($"❌ Файл не найден: {filePath}");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"SHA: {sha}");

            var (success, errorMessage) = await gitHubService.DeleteFileAsync(
                owner,
                repo,
                filePath,
                $"Delete {filePath}",
                sha
            );

            Console.WriteLine(success ? "✅ Готово!" : $"❌ Ошибка: {errorMessage}");
            Console.WriteLine("Нажмите Enter для продолжения...");
            Console.ReadKey();
        }
    }
}

