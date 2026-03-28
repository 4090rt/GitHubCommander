using Microsoft.Extensions.DependencyInjection;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace GithubComander.src.GitHubCommander.Infrastructure.Delegates
{
    public class lientOptions
    {
        public delegate void ServiceProviderOptions(IServiceCollection services);

        public void RunDelegate(ServiceProviderOptions serviceProviderOptions, IServiceCollection services)
        { 
            serviceProviderOptions?.Invoke(services);
        }

        public void Servis(IServiceCollection services)
        {
            services.AddHttpClient("GithubApiClient1", client =>
            {
                client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.v3+json");
                client.DefaultRequestHeaders.UserAgent.ParseAdd("GitHubCommander/1.0");
                client.BaseAddress = new Uri("https://api.github.com/");

                var token1 = Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? throw new InvalidOperationException("GITHUB_TOKEN not set");
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("token", token1);

                client.DefaultRequestVersion = HttpVersion.Version20;
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
            })
            .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(
                TimeSpan.FromSeconds(30),
                Polly.Timeout.TimeoutStrategy.Pessimistic,
                onTimeoutAsync: (context, timespan, task) =>
                {
                    Console.WriteLine($"⏰ Request timed out after {timespan}");
                    return Task.CompletedTask;
                }))
            .AddTransientHttpErrorPolicy(polly =>
            polly.CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromMinutes(1),
                onBreak: (outcome, timespan) =>
                {
                    Console.WriteLine($"🔌 Circuit opened for {timespan}");
                },
                onHalfOpen: () =>
                {
                    Console.WriteLine("✅ Circuit reset");
                },
                onReset: () =>
                {
                    Console.WriteLine("⚠️ Circuit half-open");
                }))
            .AddTransientHttpErrorPolicy(policy =>
                policy.WaitAndRetryAsync(3, retryCount =>
                TimeSpan.FromSeconds(Math.Pow(2, retryCount)) +
                TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100)),
                onRetry: (outcome, timespan, retrycount, context) =>
                {
                    Console.WriteLine($"🔄 Retry {retrycount} after {timespan}");
                }))
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler()
                {
                    EnableMultipleHttp2Connections = true,

                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(15),
                    PooledConnectionLifetime = TimeSpan.FromMinutes(10),

                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,

                    MaxConnectionsPerServer = 10,
                    UseCookies = false,
                    AllowAutoRedirect = false
                });
        }

        public void ServisEthernet(IServiceCollection services)
        {
            services.AddHttpClient("EthernetApiClient", client =>
            {
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br");
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US");
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                client.DefaultRequestVersion = HttpVersion.Version20;
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
            })
            .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(
                TimeSpan.FromSeconds(30),
                Polly.Timeout.TimeoutStrategy.Pessimistic,
                onTimeoutAsync: (context, timespan, task) =>
                {
                    Console.WriteLine($"⏰ Request timed out after {timespan}");
                    return Task.CompletedTask;
                }))
            .AddTransientHttpErrorPolicy(policy =>
            policy.CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromMinutes(1),
                onBreak: (outcome, timespan) =>
                {
                    Console.WriteLine($"🔌 Circuit opened for {timespan}");
                },
                onHalfOpen: () =>
                {
                    Console.WriteLine("⚠️ Circuit half-open");
                },
                onReset: () =>
                {
                    Console.WriteLine("✅ Circuit reset");
                }))
            .AddTransientHttpErrorPolicy(polly =>
                polly.WaitAndRetryAsync(3, retrycount =>
                TimeSpan.FromSeconds(Math.Pow(2, retrycount)) +
                TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100)),
                onRetry: (outcome, timespan, retrycount, context) =>
                {
                    Console.WriteLine($"🔄 Retry {retrycount} after {timespan}");
                }))
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler()
                {
                    EnableMultipleHttp2Connections = true,

                    PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(15),

                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,

                    MaxConnectionsPerServer = 10,
                    UseCookies = false,
                    AllowAutoRedirect = false,
                });
        }
    }
}
