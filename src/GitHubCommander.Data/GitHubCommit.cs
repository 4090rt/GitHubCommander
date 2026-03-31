using System;
using System.Text.Json.Serialization;

namespace GithubComander.src.GitHubCommander.Data
{
    /// <summary>
    /// Упрощённая модель коммита GitHub API
    /// </summary>
    public class GitHubCommit
    {
        [JsonPropertyName("sha")]
        public string Sha { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("author_name")]
        public string AuthorName { get; set; }

        [JsonPropertyName("author_date")]
        public DateTime AuthorDate { get; set; }

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; }
    }
}
