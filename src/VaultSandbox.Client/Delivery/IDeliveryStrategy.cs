using VaultSandbox.Client.Http.Models;

namespace VaultSandbox.Client.Delivery;

/// <summary>
/// Interface for email delivery strategies.
/// </summary>
internal interface IDeliveryStrategy : IAsyncDisposable
{
    /// <summary>
    /// Subscribes to email notifications for an inbox.
    /// </summary>
    /// <param name="inboxHash">The inbox hash identifier.</param>
    /// <param name="emailAddress">The inbox email address.</param>
    /// <param name="onEmail">Callback invoked when a new email arrives.</param>
    /// <param name="pollInterval">Polling interval for polling-based strategies.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SubscribeAsync(
        string inboxHash,
        string emailAddress,
        Func<SseEmailEvent, Task> onEmail,
        TimeSpan pollInterval,
        CancellationToken ct = default);

    /// <summary>
    /// Unsubscribes from email notifications for an inbox.
    /// </summary>
    /// <param name="inboxHash">The inbox hash identifier.</param>
    Task UnsubscribeAsync(string inboxHash);

    /// <summary>
    /// Checks if the strategy is currently connected/active.
    /// </summary>
    bool IsConnected { get; }
}
