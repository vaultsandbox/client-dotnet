using System.Collections.Concurrent;
using VaultSandbox.Client.Http.Models;

namespace VaultSandbox.Client.Delivery;

/// <summary>
/// Base class for delivery strategies with common subscription management.
/// </summary>
internal abstract class DeliveryStrategyBase : IDeliveryStrategy
{
    protected readonly ConcurrentDictionary<string, InboxSubscription> Subscriptions = new();

    public abstract bool IsConnected { get; }

    public virtual async Task SubscribeAsync(
        string inboxHash,
        string emailAddress,
        Func<SseEmailEvent, Task> onEmail,
        TimeSpan pollInterval,
        CancellationToken ct = default)
    {
        var subscription = new InboxSubscription(inboxHash, emailAddress, onEmail, pollInterval, ct);

        if (!Subscriptions.TryAdd(inboxHash, subscription))
        {
            throw new InvalidOperationException($"Already subscribed to inbox: {inboxHash}");
        }

        await OnSubscribedAsync(subscription);
    }

    public virtual async Task UnsubscribeAsync(string inboxHash)
    {
        if (Subscriptions.TryRemove(inboxHash, out var subscription))
        {
            await OnUnsubscribedAsync(subscription);
        }
    }

    protected virtual Task OnSubscribedAsync(InboxSubscription subscription) => Task.CompletedTask;
    protected virtual Task OnUnsubscribedAsync(InboxSubscription subscription) => Task.CompletedTask;

    public abstract ValueTask DisposeAsync();

    protected sealed record InboxSubscription(
        string InboxHash,
        string EmailAddress,
        Func<SseEmailEvent, Task> OnEmail,
        TimeSpan PollInterval,
        CancellationToken CancellationToken);
}
