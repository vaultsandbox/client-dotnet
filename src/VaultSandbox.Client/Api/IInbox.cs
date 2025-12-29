namespace VaultSandbox.Client.Api;

/// <summary>
/// Represents an email inbox with operations for receiving and managing emails.
/// </summary>
public interface IInbox : IAsyncDisposable
{
    /// <summary>
    /// The email address of this inbox.
    /// </summary>
    string EmailAddress { get; }

    /// <summary>
    /// When this inbox expires.
    /// </summary>
    DateTimeOffset ExpiresAt { get; }

    /// <summary>
    /// SHA-256 hash of the client KEM public key (inbox identifier).
    /// </summary>
    string InboxHash { get; }

    /// <summary>
    /// Whether this inbox has been disposed.
    /// </summary>
    bool IsDisposed { get; }

    /// <summary>
    /// Gets all emails in the inbox.
    /// </summary>
    Task<IReadOnlyList<Email>> GetEmailsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a specific email by ID.
    /// </summary>
    Task<Email> GetEmailAsync(string emailId, CancellationToken ct = default);

    /// <summary>
    /// Gets the raw source of an email.
    /// </summary>
    Task<string> GetEmailRawAsync(string emailId, CancellationToken ct = default);

    /// <summary>
    /// Waits for an email matching the specified filters.
    /// </summary>
    Task<Email> WaitForEmailAsync(WaitForEmailOptions? options = null, CancellationToken ct = default);

    /// <summary>
    /// Watches for new emails as an async stream.
    /// </summary>
    IAsyncEnumerable<Email> WatchAsync(CancellationToken ct = default);

    /// <summary>
    /// Marks an email as read.
    /// </summary>
    Task MarkAsReadAsync(string emailId, CancellationToken ct = default);

    /// <summary>
    /// Deletes an email from the inbox.
    /// </summary>
    Task DeleteEmailAsync(string emailId, CancellationToken ct = default);

    /// <summary>
    /// Exports inbox data (including private keys) for persistence.
    /// WARNING: Contains private keys - handle securely.
    /// </summary>
    Task<InboxExport> ExportAsync();

    /// <summary>
    /// Gets the current email count.
    /// </summary>
    Task<int> GetEmailCountAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the synchronization status of the inbox including email count and hash.
    /// </summary>
    /// <remarks>
    /// The hash can be used to efficiently detect changes without fetching all emails.
    /// Compare the hash from two calls - if different, the inbox contents have changed.
    /// </remarks>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The current sync status.</returns>
    Task<InboxSyncStatus> GetSyncStatusAsync(CancellationToken ct = default);

    /// <summary>
    /// Waits until the inbox contains at least the specified number of emails.
    /// </summary>
    /// <param name="count">The minimum number of emails to wait for.</param>
    /// <param name="options">Optional wait configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="VaultSandbox.Client.Exceptions.VaultSandboxTimeoutException">Thrown if timeout is reached.</exception>
    Task WaitForEmailCountAsync(
        int count,
        WaitForEmailCountOptions? options = null,
        CancellationToken ct = default);
}
