using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubComander.src.GitHubCommander.Infrastructure
{
    public class ViborRepo
    {

        public async Task<(string owner, string repo)> HandleRepositoryChoice(HttpRequest gitHubService, int number)
        {
            var results = await gitHubService.CachingRequest().ConfigureAwait(false);

            if (number < 1 || number > results.Count)
            {
                Console.WriteLine("❌ Неверный номер. Нажмите Enter...");
                Console.ReadLine();
                return (null, null);
            }

            var ret = results[number - 1];
            var parts = ret.FullName.Split('/');
            return (parts[0], parts[1]);
        }

        public async Task<string> HandleContentChoice(HttpRequest2 gitHubService, HttpRequest3 GI, string owner, string repo, string currentPath, int number)
        {
            var result = await gitHubService.CacheRequest(owner, repo, currentPath).ConfigureAwait(false);

            if (number < 1 || number > result.Count)
            {
                Console.WriteLine("❌ Неверный номер. Нажмите Enter...");
                Console.ReadLine();
                return currentPath;
            }
            var ret = result[number - 1];
            if (ret.IsDirectory)
            {
                return string.IsNullOrEmpty(currentPath)
                    ? ret.Name
                    : $"{currentPath}/{ret.Name}";
            }
            else
            {
                string filePath = string.IsNullOrEmpty(currentPath)
                    ? ret.Name
                    : $"{currentPath}/{ret.Name}";
                ShowReposInreposInfiles show = new ShowReposInreposInfiles();
                await show.ShowFile(GI, owner, repo, filePath).ConfigureAwait(false);
                return currentPath;
            }
        }
    }
}
