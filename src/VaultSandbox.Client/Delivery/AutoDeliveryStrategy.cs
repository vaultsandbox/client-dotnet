using Microsoft.Extensions.Logging;
using VaultSandbox.Client.Http;
using VaultSandbox.Client.Http.Models;

namespace VaultSandbox.Client.Delivery;

/// <summary>
/// Automatic delivery strategy that uses SSE with polling fallback.
/// </summary>
internal sealed class AutoDeliveryStrategy : IDeliveryStrategy
{
    private readonly IVaultSandboxApiClient _apiClient;
    private readonly VaultSandboxClientOptions _options;
    private readonly ILogger<AutoDeliveryStrategy>? _logger;
    private readonly ILoggerFactory? _loggerFactory;

    private IDeliveryStrategy _activeStrategy;
    private bool _usingSse = true;
    private readonly SemaphoreSlim _strategyLock = new(1, 1);

    public bool IsConnected => _activeStrategy.IsConnected;

    public AutoDeliveryStrategy(
        IVaultSandboxApiClient apiClient,
        VaultSandboxClientOptions options,
        ILogger<AutoDeliveryStrategy>? logger = null,
        ILoggerFactory? loggerFactory = null)
    {
        _apiClient = apiClient;
        _options = options;
        _logger = logger;
        _loggerFactory = loggerFactory;

        // Start with SSE strategy
        _activeStrategy = new SseDeliveryStrategy(
            apiClient,
            options,
            loggerFactory?.CreateLogger<SseDeliveryStrategy>());
    }

    public async Task SubscribeAsync(
        string inboxHash,
        string emailAddress,
        Func<SseEmailEvent, Task> onEmail,
        TimeSpan pollInterval,
        CancellationToken ct = default)
    {
        await _strategyLock.WaitAsync(ct);
        try
        {
            try
            {
                await _activeStrategy.SubscribeAsync(inboxHash, emailAddress, onEmail, pollInterval, ct);

                if (_usingSse)
                {
                    _logger?.LogDebug("Successfully subscribed via SSE for {EmailAddress}", emailAddress);
                }
            }
            catch (Exception ex) when (_usingSse)
            {
                _logger?.LogWarning(ex,
                    "SSE subscription failed for {EmailAddress}, falling back to polling",
                    emailAddress);

                await FallbackToPollingAsync();
                await _activeStrategy.SubscribeAsync(inboxHash, emailAddress, onEmail, pollInterval, ct);
            }
        }
        finally
        {
            _strategyLock.Release();
        }
    }

    public async Task UnsubscribeAsync(string inboxHash)
    {
        await _activeStrategy.UnsubscribeAsync(inboxHash);
    }

    private async Task FallbackToPollingAsync()
    {
        if (!_usingSse)
            return;

        _logger?.LogInformation("Switching from SSE to polling strategy");

        await _activeStrategy.DisposeAsync();

        _activeStrategy = new PollingDeliveryStrategy(
            _apiClient,
            _options,
            _loggerFactory?.CreateLogger<PollingDeliveryStrategy>());
        _usingSse = false;
    }

    public async ValueTask DisposeAsync()
    {
        await _activeStrategy.DisposeAsync();
        _strategyLock.Dispose();
    }
}
