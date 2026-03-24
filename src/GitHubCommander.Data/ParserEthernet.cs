using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GithubComander.src.GitHubCommander.Data
{
    public class ParserEthernet
    {
        [JsonPropertyName("ip")]
        public string IP { get; set; }

        [JsonPropertyName("hostname")]
        public string Hostname { get; set; }

        [JsonPropertyName("city")]
        public string City { get; set; }

        [JsonPropertyName("region")]
        public string Region { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }

        [JsonPropertyName("loc")]
        public string Loc { get; set; }

        [JsonPropertyName("org")]
        public string Org { get; set; }

        [JsonPropertyName("postal")]
        public string Postal { get; set; }

        [JsonPropertyName("timezone")]
        public string Timezone { get; set; }

        [JsonPropertyName("readme")]
        public string Readme { get; set; }

        // Дополнительные поля для тестов скорости
        [JsonIgnore]
        public long? PingMs { get; set; }

        [JsonIgnore]
        public double? DownloadSpeedMbps { get; set; }

        [JsonIgnore]
        public double? UploadSpeedMbps { get; set; }

        [JsonIgnore]
        public long? JitterMs { get; set; }
    }
}
