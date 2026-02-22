using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubComander.src.GitHubCommander.Infrastructure
{
    public class ShowReposInreposInfiles
    {
        // показать список репозиториев
        public async Task ShowRepositoryes(HttpRequest gitHubService)
        {
            Console.Clear();
            Console.WriteLine("═══ СПИСОК РЕПОЗИТОРИЕВ ═══\n");
            Console.WriteLine("Твои репозитории:\n");

            var item = await gitHubService.Request();

            if (item != null && item.Count > 0)
            {
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
            else
            {
                Console.WriteLine("Репозитории не найдены");
            }
        }
        // показать содержимое репозитория
        public async Task ShowContents(HttpRequest2 gitHubService, string owner, string repo, string path)
        {
            Console.Clear();
            
            if (string.IsNullOrEmpty(path))
                Console.WriteLine($"📁 {owner}/{repo} (корень)\n");
            else
                Console.WriteLine($"📁 {owner}/{repo}/{path}\n");

            var items = await gitHubService.CacheRequest(owner, repo, path);

            if (items != null && items.Count > 0)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    string icon = item.IsDirectory ? "📁" : "📄";
                    string size = item.IsFile ? $" ({FormatSize(item.Size)})" : "";
                    Console.WriteLine($"[{i + 1}] {icon} {item.Name}{size}");
                }
            }
            else
            {
                Console.WriteLine("Репозиторий пуст");
            }
        }
        // показать файл
        public async Task ShowFile(HttpRequest3 gitHubService, string owner, string repo, string path)
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
        // конвертация размеров файла 
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
}
