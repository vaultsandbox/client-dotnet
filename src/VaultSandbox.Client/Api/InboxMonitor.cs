using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace VaultSandbox.Client.Api;

/// <summary>
/// Monitors multiple inboxes for new emails with a unified stream.
/// </summary>
public sealed class InboxMonitor : IAsyncDisposable
{
    private readonly IReadOnlyList<IInbox> _inboxes;
    private readonly Channel<InboxEmailEvent> _channel;
    private readonly List<Task> _watchTasks = new();
    private readonly CancellationTokenSource _cts = new();
    private volatile bool _isStarted;
    private bool _isDisposed;

    internal InboxMonitor(IReadOnlyList<IInbox> inboxes)
    {
        _inboxes = inboxes;
        _channel = Channel.CreateUnbounded<InboxEmailEvent>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false // Multiple inbox watchers write
        });
    }

    /// <summary>
    /// The inboxes being monitored.
    /// </summary>
    public IReadOnlyList<IInbox> Inboxes => _inboxes;

    /// <summary>
    /// Number of inboxes being monitored.
    /// </summary>
    public int InboxCount => _inboxes.Count;

    /// <summary>
    /// Watches for new emails across all monitored inboxes.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Async enumerable of inbox/email pairs.</returns>
    public async IAsyncEnumerable<InboxEmailEvent> WatchAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        EnsureStarted();

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);

        await foreach (var item in _channel.Reader.ReadAllAsync(linkedCts.Token))
        {
            yield return item;
        }
    }

    /// <summary>
    /// Starts monitoring all inboxes.
    /// Called automatically by WatchAsync, but can be called explicitly
    /// to begin buffering emails before consuming.
    /// </summary>
    public void Start()
    {
        EnsureStarted();
    }

    private void EnsureStarted()
    {
        if (_isStarted) return;

        lock (_watchTasks)
        {
            if (_isStarted) return;
            _isStarted = true;

            foreach (var inbox in _inboxes)
            {
                var task = WatchInboxAsync(inbox);
                _watchTasks.Add(task);
            }
        }
    }

    private async Task WatchInboxAsync(IInbox inbox)
    {
        try
        {
            await foreach (var email in inbox.WatchAsync(_cts.Token))
            {
                var evt = new InboxEmailEvent(inbox, email);
                await _channel.Writer.WriteAsync(evt, _cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during disposal
        }
        catch (ObjectDisposedException)
        {
            // Inbox was disposed
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        // Signal cancellation
        await _cts.CancelAsync();

        // Wait for all watch tasks to complete
        try
        {
            await Task.WhenAll(_watchTasks);
        }
        catch
        {
            // Ignore cancellation exceptions
        }

        _channel.Writer.Complete();
        _cts.Dispose();
    }
}

/// <summary>
/// Event containing an inbox and the email that arrived.
/// </summary>
/// <param name="Inbox">The inbox that received the email.</param>
/// <param name="Email">The email that was received.</param>
public sealed record InboxEmailEvent(IInbox Inbox, Email Email)
{
    /// <summary>
    /// Convenience accessor for the inbox email address.
    /// </summary>
    public string InboxAddress => Inbox.EmailAddress;
}
