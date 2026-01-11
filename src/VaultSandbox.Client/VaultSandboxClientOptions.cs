using System.ComponentModel.DataAnnotations;

namespace VaultSandbox.Client;

/// <summary>
/// Configuration options for VaultSandboxClient.
/// </summary>
public sealed class VaultSandboxClientOptions
{
    /// <summary>
    /// The base URL of the VaultSandbox server.
    /// </summary>
    [Required]
    public required string BaseUrl { get; set; }

    /// <summary>
    /// The API key for authentication.
    /// </summary>
    [Required]
    public required string ApiKey { get; set; }

    /// <summary>
    /// HTTP request timeout in milliseconds.
    /// Default: 30000 (30 seconds)
    /// </summary>
    public int HttpTimeoutMs { get; set; } = 30_000;

    /// <summary>
    /// Default timeout for WaitForEmail operations in milliseconds.
    /// Default: 30000 (30 seconds)
    /// </summary>
    public int WaitTimeoutMs { get; set; } = 30_000;

    /// <summary>
    /// Polling interval in milliseconds.
    /// Default: 2000 (2 seconds)
    /// </summary>
    public int PollIntervalMs { get; set; } = 2_000;

    /// <summary>
    /// Maximum number of HTTP retries.
    /// Default: 3
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Initial retry delay in milliseconds.
    /// Default: 1000 (1 second)
    /// </summary>
    public int RetryDelayMs { get; set; } = 1_000;

    /// <summary>
    /// SSE reconnection interval in milliseconds.
    /// Default: 2000 (2 seconds)
    /// </summary>
    public int SseReconnectIntervalMs { get; set; } = 2_000;

    /// <summary>
    /// Maximum SSE reconnection attempts.
    /// Default: 10
    /// </summary>
    public int SseMaxReconnectAttempts { get; set; } = 10;

    /// <summary>
    /// Default delivery strategy.
    /// Default: Sse
    /// </summary>
    public DeliveryStrategy DefaultDeliveryStrategy { get; set; } = DeliveryStrategy.Sse;

    /// <summary>
    /// Default inbox TTL in seconds.
    /// Default: 3600 (1 hour)
    /// </summary>
    public int DefaultInboxTtlSeconds { get; set; } = 3600;

    /// <summary>
    /// Validates the options and throws if invalid.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when validation fails.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
            throw new InvalidOperationException("BaseUrl is required");

        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new InvalidOperationException("ApiKey is required");

        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri))
            throw new InvalidOperationException($"BaseUrl is not a valid absolute URI: {BaseUrl}");

        if (uri.Scheme != "http" && uri.Scheme != "https")
            throw new InvalidOperationException($"BaseUrl must use http or https scheme: {BaseUrl}");

        if (HttpTimeoutMs <= 0)
            throw new InvalidOperationException("HttpTimeoutMs must be positive");

        if (WaitTimeoutMs <= 0)
            throw new InvalidOperationException("WaitTimeoutMs must be positive");

        if (PollIntervalMs <= 0)
            throw new InvalidOperationException("PollIntervalMs must be positive");

        if (MaxRetries < 0)
            throw new InvalidOperationException("MaxRetries cannot be negative");

        if (RetryDelayMs <= 0)
            throw new InvalidOperationException("RetryDelayMs must be positive");

        if (SseReconnectIntervalMs <= 0)
            throw new InvalidOperationException("SseReconnectIntervalMs must be positive");

        if (SseMaxReconnectAttempts < 0)
            throw new InvalidOperationException("SseMaxReconnectAttempts cannot be negative");

        if (DefaultInboxTtlSeconds < 60)
            throw new InvalidOperationException("DefaultInboxTtlSeconds must be at least 60 seconds");
    }
}

/// <summary>
/// Email delivery strategy options.
/// </summary>
public enum DeliveryStrategy
{
    /// <summary>
    /// Use Server-Sent Events for real-time updates (default).
    /// </summary>
    Sse,

    /// <summary>
    /// Use polling for updates.
    /// </summary>
    Polling
}
