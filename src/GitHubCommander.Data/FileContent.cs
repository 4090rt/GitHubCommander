using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GithubComander.src.GitHubCommander.Data
{
    public class FileContent
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("encoding")]
        public string Encoding { get; set; }

        public string GetDecodedContent()
        {
            if (string.IsNullOrEmpty(Content) || Encoding != "base64")
                return string.Empty;

            // GitHub добавляет переносы строк в base64, их нужно убрать
            var cleanBase64 = Content.Replace("\n", "").Replace("\r", "");
            var bytes = Convert.FromBase64String(cleanBase64);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
    }
}
