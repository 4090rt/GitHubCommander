using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GithubComander.src.GitHubCommander.Data
{

    public class PingResult
    {
        [JsonPropertyName("host")]
        public string Host { get; set; }

        [JsonPropertyName("ping")]
        public long PingMs { get; set; }
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("error")]
        public string Error { get; set; }
    }

    /// <summary>
    /// Полный результат теста сети (провайдер + ping)
    /// </summary>
    public class NetworkSpeedResult
    {
        /// <summary>
        /// Информация о провайдере (из ipinfo.io)
        /// </summary>
        [JsonPropertyName("provider")]
        public ParserEthernet Provider { get; set; }

        /// <summary>
        /// Результаты замера Ping до разных серверов
        /// </summary>
        [JsonPropertyName("pingResults")]
        public List<PingResult> PingResults { get; set; }

        /// <summary>
        /// Средний Ping (мс)
        /// </summary>
        [JsonPropertyName("averagePing")]
        public double? AveragePingMs { get; set; }

        /// <summary>
        /// Минимальный Ping (мс)
        /// </summary>
        [JsonPropertyName("minPing")]
        public long? MinPingMs { get; set; }

        /// <summary>
        /// Максимальный Ping (мс)
        /// </summary>
        [JsonPropertyName("maxPing")]
        public long? MaxPingMs { get; set; }

        /// <summary>
        /// Jitter - разница между макс и мин пингом (мс)
        /// </summary>
        [JsonPropertyName("jitter")]
        public long? JitterMs { get; set; }
    }
}
