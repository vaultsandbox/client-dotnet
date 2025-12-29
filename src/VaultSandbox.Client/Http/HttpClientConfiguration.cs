using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace VaultSandbox.Client.Http;

/// <summary>
/// HTTP client configuration with retry and circuit breaker policies.
/// </summary>
internal static class HttpClientConfiguration
{
    /// <summary>
    /// Configures an HttpClient with resilience handlers.
    /// </summary>
    public static IHttpClientBuilder AddVaultSandboxResilience(
        this IHttpClientBuilder builder,
        VaultSandboxClientOptions options)
    {
        builder.AddResilienceHandler("VaultSandbox", configure =>
        {
            // Retry policy with exponential backoff
            configure.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = options.MaxRetries,
                Delay = TimeSpan.FromMilliseconds(options.RetryDelayMs),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = args => ValueTask.FromResult(ShouldRetry(args.Outcome))
            });

            // Circuit breaker
            configure.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                SamplingDuration = TimeSpan.FromSeconds(30),
                FailureRatio = 0.5,
                MinimumThroughput = 10,
                BreakDuration = TimeSpan.FromSeconds(30),
                ShouldHandle = args => ValueTask.FromResult(ShouldRetry(args.Outcome))
            });

            // Overall timeout
            configure.AddTimeout(TimeSpan.FromMilliseconds(options.HttpTimeoutMs));
        });

        return builder;
    }

    private static bool ShouldRetry(Outcome<HttpResponseMessage> outcome)
    {
        // Retry on network errors
        if (outcome.Exception is not null)
            return true;

        // Retry on specific status codes
        if (outcome.Result is { } response)
        {
            return response.StatusCode switch
            {
                HttpStatusCode.RequestTimeout => true,        // 408
                HttpStatusCode.TooManyRequests => true,       // 429
                HttpStatusCode.InternalServerError => true,   // 500
                HttpStatusCode.BadGateway => true,            // 502
                HttpStatusCode.ServiceUnavailable => true,    // 503
                HttpStatusCode.GatewayTimeout => true,        // 504
                _ => false
            };
        }

        return false;
    }
}
