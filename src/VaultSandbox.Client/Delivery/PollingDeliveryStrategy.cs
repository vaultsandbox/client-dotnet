using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using VaultSandbox.Client.Http;
using VaultSandbox.Client.Http.Models;

namespace VaultSandbox.Client.Delivery;

/// <summary>
/// Email delivery via periodic polling with exponential backoff.
/// </summary>
internal sealed class PollingDeliveryStrategy : DeliveryStrategyBase
{
    private readonly IVaultSandboxApiClient _apiClient;
    private readonly VaultSandboxClientOptions _options;
    private readonly ILogger<PollingDeliveryStrategy>? _logger;

    private readonly ConcurrentDictionary<string, PollingState> _pollingStates = new();
    private readonly ConcurrentDictionary<string, Task> _pollingTasks = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _pollingCts = new();

    private const double BackoffMultiplier = 1.5;
    private const int MaxBackoffMultiplier = 15;

    public override bool IsConnected => !Subscriptions.IsEmpty;

    public PollingDeliveryStrategy(
        IVaultSandboxApiClient apiClient,
        VaultSandboxClientOptions options,
        ILogger<PollingDeliveryStrategy>? logger = null)
    {
        _apiClient = apiClient;
        _options = options;
        _logger = logger;
    }

    protected override Task OnSubscribedAsync(InboxSubscription subscription)
    {
        var cts = new CancellationTokenSource();
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cts.Token, subscription.CancellationToken);

        _pollingCts[subscription.InboxHash] = cts;
        _pollingStates[subscription.InboxHash] = new PollingState();
        _pollingTasks[subscription.InboxHash] = PollInboxAsync(subscription, linkedCts.Token);

        return Task.CompletedTask;
    }

    protected override async Task OnUnsubscribedAsync(InboxSubscription subscription)
    {
        if (_pollingCts.TryRemove(subscription.InboxHash, out var cts))
        {
            await cts.CancelAsync();
            cts.Dispose();
        }

        if (_pollingTasks.TryRemove(subscription.InboxHash, out var task))
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        _pollingStates.TryRemove(subscription.InboxHash, out _);
    }

    private async Task PollInboxAsync(InboxSubscription subscription, CancellationToken ct)
    {
        var state = _pollingStates[subscription.InboxHash];
        var initialIntervalMs = (int)subscription.PollInterval.TotalMilliseconds;
        var currentBackoff = initialIntervalMs;
        var maxBackoff = initialIntervalMs * MaxBackoffMultiplier;

        _logger?.LogDebug("Starting polling for inbox {EmailAddress} with interval {Interval}ms",
            subscription.EmailAddress, initialIntervalMs);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var syncStatus = await _apiClient.GetInboxSyncAsync(subscription.EmailAddress, ct);

                if (state.LastEmailsHash != syncStatus.EmailsHash)
                {
                    _logger?.LogDebug(
                        "Inbox {EmailAddress} has changes (hash: {Hash})",
                        subscription.EmailAddress, syncStatus.EmailsHash);

                    state.LastEmailsHash = syncStatus.EmailsHash;
                    currentBackoff = initialIntervalMs; // Reset backoff on change

                    // Fetch new emails (metadata only since we just need IDs)
                    var emails = await _apiClient.GetEmailsAsync(subscription.EmailAddress, includeContent: false, ct);

                    foreach (var email in emails)
                    {
                        // Skip already processed emails
                        if (state.ProcessedEmailIds.Contains(email.Id))
                            continue;

                        state.ProcessedEmailIds.Add(email.Id);

                        var emailEvent = new SseEmailEvent
                        {
                            InboxId = email.InboxId ?? email.Id,
                            EmailId = email.Id,
                            EncryptedMetadata = email.EncryptedMetadata
                        };

                        await subscription.OnEmail(emailEvent);
                    }
                }
                else
                {
                    // No changes - increase backoff
                    currentBackoff = BackoffCalculator.CalculateLinear(
                        currentBackoff, BackoffMultiplier, maxBackoff, jitterFactor: 0);
                }

                var delay = BackoffCalculator.AddJitter(currentBackoff);

                _logger?.LogTrace("Polling {EmailAddress} again in {Delay}ms",
                    subscription.EmailAddress, delay);

                await Task.Delay(delay, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Polling error for inbox {EmailAddress}",
                    subscription.EmailAddress);

                // Backoff on error
                currentBackoff = BackoffCalculator.CalculateLinear(
                    currentBackoff, BackoffMultiplier, maxBackoff, jitterFactor: 0);

                await Task.Delay(BackoffCalculator.AddJitter(currentBackoff), ct);
            }
        }

        _logger?.LogDebug("Stopped polling for inbox {EmailAddress}", subscription.EmailAddress);
    }

    public override async ValueTask DisposeAsync()
    {
        // Cancel all polling tasks
        foreach (var cts in _pollingCts.Values)
        {
            await cts.CancelAsync();
            cts.Dispose();
        }

        // Wait for all tasks to complete (ignoring cancellation exceptions)
        try
        {
            await Task.WhenAll(_pollingTasks.Values);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelling polling tasks
        }

        _pollingCts.Clear();
        _pollingTasks.Clear();
        _pollingStates.Clear();
        Subscriptions.Clear();
    }

    private sealed class PollingState
    {
        public string? LastEmailsHash { get; set; }
        public HashSet<string> ProcessedEmailIds { get; } = [];
    }
}
