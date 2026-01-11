using System.Net.ServerSentEvents;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using VaultSandbox.Client.Exceptions;
using VaultSandbox.Client.Http;
using VaultSandbox.Client.Http.Models;

namespace VaultSandbox.Client.Delivery;

/// <summary>
/// Real-time email delivery via Server-Sent Events.
/// </summary>
internal sealed class SseDeliveryStrategy : DeliveryStrategyBase
{
    private readonly IVaultSandboxApiClient _apiClient;
    private readonly VaultSandboxClientOptions _options;
    private readonly ILogger<SseDeliveryStrategy>? _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    private CancellationTokenSource? _connectionCts;
    private Task? _connectionTask;
    private int _reconnectAttempts;
    private bool _isConnected;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private TaskCompletionSource<bool>? _initialConnectionTcs;

    public override bool IsConnected => _isConnected;

    public SseDeliveryStrategy(
        IVaultSandboxApiClient apiClient,
        VaultSandboxClientOptions options,
        ILogger<SseDeliveryStrategy>? logger = null)
    {
        _apiClient = apiClient;
        _options = options;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = VaultSandboxJsonContext.Default
        };
    }

    protected override async Task OnSubscribedAsync(InboxSubscription subscription)
    {
        await EnsureConnectedAsync(waitForConnection: true);
    }

    protected override async Task OnUnsubscribedAsync(InboxSubscription subscription)
    {
        // If no more subscriptions, disconnect
        if (Subscriptions.IsEmpty)
        {
            await DisconnectAsync();
        }
        else
        {
            // Reconnect with updated subscription list
            await ReconnectAsync();
        }
    }

    private async Task EnsureConnectedAsync(bool waitForConnection = false)
    {
        TaskCompletionSource<bool>? tcs = null;

        await _connectionLock.WaitAsync();
        try
        {
            if (_connectionTask is null || _connectionTask.IsCompleted)
            {
                _connectionCts = new CancellationTokenSource();
                if (waitForConnection)
                {
                    tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _initialConnectionTcs = tcs;
                }
                _connectionTask = RunConnectionLoopAsync(_connectionCts.Token);
            }
        }
        finally
        {
            _connectionLock.Release();
        }

        // Wait for initial connection outside of lock
        if (tcs is not null)
        {
            await tcs.Task;
        }
    }

    private async Task ReconnectAsync()
    {
        await DisconnectAsync();
        await EnsureConnectedAsync(waitForConnection: false);
    }

    private async Task DisconnectAsync()
    {
        await _connectionLock.WaitAsync();
        try
        {
            if (_connectionCts is not null)
            {
                await _connectionCts.CancelAsync();
                _connectionCts.Dispose();
                _connectionCts = null;
            }

            if (_connectionTask is not null)
            {
                try
                {
                    await _connectionTask;
                }
                catch
                {
                    // Expected - connection may have failed or been cancelled
                }
                _connectionTask = null;
            }

            _isConnected = false;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private async Task RunConnectionLoopAsync(CancellationToken ct)
    {
        var isFirstAttempt = true;
        var hadPreviousConnection = false;

        while (!ct.IsCancellationRequested && !Subscriptions.IsEmpty)
        {
            try
            {
                var inboxHashes = Subscriptions.Keys.ToArray();

                _logger?.LogDebug("Connecting to SSE with {Count} inboxes", inboxHashes.Length);

                await using var stream = await _apiClient.GetEventsStreamAsync(inboxHashes, ct);

                _isConnected = true;
                _reconnectAttempts = 0;

                _logger?.LogInformation("SSE connection established");

                // Signal successful initial connection
                if (isFirstAttempt)
                {
                    isFirstAttempt = false;
                    _initialConnectionTcs?.TrySetResult(true);
                    _initialConnectionTcs = null;
                }

                // Trigger sync callbacks on reconnection (not first connect)
                if (hadPreviousConnection)
                {
                    _logger?.LogDebug("SSE reconnected, triggering sync callbacks");
                    await InvokeReconnectCallbacksAsync();
                }

                hadPreviousConnection = true;

                await ProcessSseStreamAsync(stream, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                _logger?.LogDebug("SSE connection cancelled");
                _initialConnectionTcs?.TrySetCanceled(ct);
                break;
            }
            catch (ApiException ex) when (ex.StatusCode == 400 &&
                ex.ResponseBody?.Contains("No matching inbox hashes found") == true)
            {
                // Server returns 400 when inbox hashes don't exist (deleted or invalid)
                var emailAddress = Subscriptions.Values.FirstOrDefault()?.EmailAddress ?? "unknown";
                var notFoundEx = new InboxNotFoundException(emailAddress);

                if (isFirstAttempt)
                {
                    _initialConnectionTcs?.TrySetException(notFoundEx);
                    _initialConnectionTcs = null;
                }
                throw notFoundEx;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                _reconnectAttempts++;

                // On first attempt, immediately signal failure to allow fallback
                if (isFirstAttempt)
                {
                    isFirstAttempt = false;
                    _initialConnectionTcs?.TrySetException(ex);
                    _initialConnectionTcs = null;
                    throw; // Re-throw to exit the loop and let AutoDeliveryStrategy handle fallback
                }

                if (_reconnectAttempts > _options.SseMaxReconnectAttempts)
                {
                    _logger?.LogError(ex, "SSE max reconnect attempts ({Max}) exceeded",
                        _options.SseMaxReconnectAttempts);
                    throw new SseException(
                        $"SSE connection failed after {_reconnectAttempts} attempts", ex);
                }

                var delay = CalculateReconnectDelay();
                _logger?.LogWarning(ex,
                    "SSE connection lost. Reconnecting in {Delay}ms (attempt {Attempt}/{Max})",
                    delay, _reconnectAttempts, _options.SseMaxReconnectAttempts);

                await Task.Delay(delay, ct);
            }
        }

        _isConnected = false;
    }

    private async Task InvokeReconnectCallbacksAsync()
    {
        foreach (var subscription in Subscriptions.Values)
        {
            if (subscription.OnReconnected is not null)
            {
                try
                {
                    await subscription.OnReconnected();
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex,
                        "Reconnect callback failed for inbox {InboxHash}",
                        subscription.InboxHash);
                }
            }
        }
    }

    private async Task ProcessSseStreamAsync(Stream stream, CancellationToken ct)
    {
        var parser = SseParser.Create(stream);

        await foreach (var sseItem in parser.EnumerateAsync(ct))
        {
            if (sseItem.EventType == "message" || string.IsNullOrEmpty(sseItem.EventType))
            {
                await ProcessEventAsync(sseItem.Data, ct);
            }
        }
    }

    private async Task ProcessEventAsync(string data, CancellationToken ct)
    {
        try
        {
            var json = data;
            var emailEvent = JsonSerializer.Deserialize<SseEmailEvent>(json, _jsonOptions);

            if (emailEvent is null)
            {
                _logger?.LogWarning("Failed to parse SSE event: {Data}", json);
                return;
            }

            if (Subscriptions.TryGetValue(emailEvent.InboxId, out var subscription))
            {
                await subscription.OnEmail(emailEvent);
            }
            else
            {
                _logger?.LogDebug("Received event for unknown inbox: {InboxId}", emailEvent.InboxId);
            }
        }
        catch (JsonException ex)
        {
            _logger?.LogWarning(ex, "Failed to deserialize SSE event");
        }
    }

    private int CalculateReconnectDelay()
    {
        return BackoffCalculator.Calculate(
            _options.SseReconnectIntervalMs,
            _reconnectAttempts,
            maxMultiplier: 10);
    }

    public override async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _connectionLock.Dispose();
    }
}
