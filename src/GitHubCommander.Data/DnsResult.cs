namespace GithubComander.src.GitHubCommander.Data
{
    public class DnsResult
    {
        public string Host { get; set; } = string.Empty;
        public System.Net.IPAddress[] Addresses { get; set; } = Array.Empty<System.Net.IPAddress>();
        public long ResolveTime { get; set; }
        public bool Success { get; set; }
        public string? Error { get; set; }
    }
}
