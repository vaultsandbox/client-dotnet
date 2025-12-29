using Microsoft.Extensions.Logging;
using VaultSandbox.Client.Api;
using VaultSandbox.Client.Crypto;
using VaultSandbox.Client.Http;

namespace VaultSandbox.Client;

/// <summary>
/// Fluent builder for creating VaultSandboxClient instances.
/// </summary>
public sealed class VaultSandboxClientBuilder
{
    private string? _baseUrl;
    private string? _apiKey;
    private int? _httpTimeoutMs;
    private int? _waitTimeoutMs;
    private int? _pollIntervalMs;
    private int? _maxRetries;
    private int? _retryDelayMs;
    private int? _sseReconnectIntervalMs;
    private int? _sseMaxReconnectAttempts;
    private DeliveryStrategy? _deliveryStrategy;
    private int? _defaultInboxTtlSeconds;
    private ILoggerFactory? _loggerFactory;
    private HttpClient? _httpClient;
    private bool _disposeHttpClient = true;

    /// <summary>
    /// Creates a new builder instance.
    /// </summary>
    public static VaultSandboxClientBuilder Create() => new();

    /// <summary>
    /// Sets the base URL of the VaultSandbox API.
    /// </summary>
    public VaultSandboxClientBuilder WithBaseUrl(string baseUrl)
    {
        _baseUrl = baseUrl;
        return this;
    }

    /// <summary>
    /// Sets the API key for authentication.
    /// </summary>
    public VaultSandboxClientBuilder WithApiKey(string apiKey)
    {
        _apiKey = apiKey;
        return this;
    }

    /// <summary>
    /// Sets the HTTP timeout.
    /// </summary>
    public VaultSandboxClientBuilder WithHttpTimeout(TimeSpan timeout)
    {
        _httpTimeoutMs = (int)timeout.TotalMilliseconds;
        return this;
    }

    /// <summary>
    /// Sets the default wait timeout for WaitForEmailAsync.
    /// </summary>
    public VaultSandboxClientBuilder WithWaitTimeout(TimeSpan timeout)
    {
        _waitTimeoutMs = (int)timeout.TotalMilliseconds;
        return this;
    }

    /// <summary>
    /// Sets the polling interval.
    /// </summary>
    public VaultSandboxClientBuilder WithPollInterval(TimeSpan interval)
    {
        _pollIntervalMs = (int)interval.TotalMilliseconds;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of HTTP retry attempts.
    /// </summary>
    public VaultSandboxClientBuilder WithMaxRetries(int maxRetries)
    {
        _maxRetries = maxRetries;
        return this;
    }

    /// <summary>
    /// Sets the initial retry delay.
    /// </summary>
    public VaultSandboxClientBuilder WithRetryDelay(TimeSpan delay)
    {
        _retryDelayMs = (int)delay.TotalMilliseconds;
        return this;
    }

    /// <summary>
    /// Sets the SSE reconnection interval.
    /// </summary>
    public VaultSandboxClientBuilder WithSseReconnectInterval(TimeSpan interval)
    {
        _sseReconnectIntervalMs = (int)interval.TotalMilliseconds;
        return this;
    }

    /// <summary>
    /// Sets the maximum SSE reconnection attempts.
    /// </summary>
    public VaultSandboxClientBuilder WithSseMaxReconnectAttempts(int maxAttempts)
    {
        _sseMaxReconnectAttempts = maxAttempts;
        return this;
    }

    /// <summary>
    /// Sets the delivery strategy.
    /// </summary>
    public VaultSandboxClientBuilder WithDeliveryStrategy(DeliveryStrategy strategy)
    {
        _deliveryStrategy = strategy;
        return this;
    }

    /// <summary>
    /// Uses SSE delivery strategy for real-time updates.
    /// </summary>
    public VaultSandboxClientBuilder UseSseDelivery()
    {
        _deliveryStrategy = DeliveryStrategy.Sse;
        return this;
    }

    /// <summary>
    /// Uses polling delivery strategy.
    /// </summary>
    public VaultSandboxClientBuilder UsePollingDelivery()
    {
        _deliveryStrategy = DeliveryStrategy.Polling;
        return this;
    }

    /// <summary>
    /// Uses auto delivery strategy (SSE with polling fallback).
    /// This is the default.
    /// </summary>
    public VaultSandboxClientBuilder UseAutoDelivery()
    {
        _deliveryStrategy = DeliveryStrategy.Auto;
        return this;
    }

    /// <summary>
    /// Sets the default inbox TTL.
    /// </summary>
    public VaultSandboxClientBuilder WithDefaultInboxTtl(TimeSpan ttl)
    {
        _defaultInboxTtlSeconds = (int)ttl.TotalSeconds;
        return this;
    }

    /// <summary>
    /// Sets the logger factory for structured logging.
    /// </summary>
    public VaultSandboxClientBuilder WithLogging(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        return this;
    }

    /// <summary>
    /// Uses a custom HttpClient instance.
    /// </summary>
    /// <param name="httpClient">The HttpClient to use.</param>
    /// <param name="disposeClient">Whether to dispose the HttpClient when the client is disposed.</param>
    public VaultSandboxClientBuilder WithHttpClient(HttpClient httpClient, bool disposeClient = false)
    {
        _httpClient = httpClient;
        _disposeHttpClient = disposeClient;
        return this;
    }

    /// <summary>
    /// Builds the VaultSandboxClient instance.
    /// </summary>
    public IVaultSandboxClient Build()
    {
        var options = BuildOptions();
        options.Validate();

        // Determine if we need to dispose the HttpClient:
        // - If user provided their own HttpClient, use their disposeClient setting
        // - If we create a default HttpClient, always dispose it
        var httpClient = _httpClient ?? CreateDefaultHttpClient(options);
        var shouldDisposeHttpClient = _httpClient is null || _disposeHttpClient;

        var apiClient = new VaultSandboxApiClient(
            httpClient,
            disposeHttpClient: shouldDisposeHttpClient,
            _loggerFactory?.CreateLogger<VaultSandboxApiClient>());
        var cryptoProvider = new CryptoProvider();

        // Always dispose the api client since the builder owns it
        return new VaultSandboxClient(apiClient, cryptoProvider, options, disposeApiClient: true, _loggerFactory);
    }

    /// <summary>
    /// Builds the VaultSandboxClient and validates the API key.
    /// </summary>
    public async Task<IVaultSandboxClient> BuildAndValidateAsync(CancellationToken ct = default)
    {
        var client = Build();

        if (!await client.ValidateApiKeyAsync(ct))
        {
            await client.DisposeAsync();
            throw new Exceptions.ApiException(401, "Invalid API key");
        }

        return client;
    }

    private VaultSandboxClientOptions BuildOptions()
    {
        var options = new VaultSandboxClientOptions
        {
            BaseUrl = _baseUrl ?? throw new InvalidOperationException("BaseUrl is required"),
            ApiKey = _apiKey ?? throw new InvalidOperationException("ApiKey is required")
        };

        if (_httpTimeoutMs.HasValue)
            options.HttpTimeoutMs = _httpTimeoutMs.Value;
        if (_waitTimeoutMs.HasValue)
            options.WaitTimeoutMs = _waitTimeoutMs.Value;
        if (_pollIntervalMs.HasValue)
            options.PollIntervalMs = _pollIntervalMs.Value;
        if (_maxRetries.HasValue)
            options.MaxRetries = _maxRetries.Value;
        if (_retryDelayMs.HasValue)
            options.RetryDelayMs = _retryDelayMs.Value;
        if (_sseReconnectIntervalMs.HasValue)
            options.SseReconnectIntervalMs = _sseReconnectIntervalMs.Value;
        if (_sseMaxReconnectAttempts.HasValue)
            options.SseMaxReconnectAttempts = _sseMaxReconnectAttempts.Value;
        if (_deliveryStrategy.HasValue)
            options.DefaultDeliveryStrategy = _deliveryStrategy.Value;
        if (_defaultInboxTtlSeconds.HasValue)
            options.DefaultInboxTtlSeconds = _defaultInboxTtlSeconds.Value;

        return options;
    }

    private static HttpClient CreateDefaultHttpClient(VaultSandboxClientOptions options)
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(options.BaseUrl),
            Timeout = TimeSpan.FromMilliseconds(options.HttpTimeoutMs)
        };

        client.DefaultRequestHeaders.Add("X-API-Key", options.ApiKey);

        return client;
    }
}
