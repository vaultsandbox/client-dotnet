using VaultSandbox.Client.Http.Models;

namespace VaultSandbox.Client.Http;

/// <summary>
/// Low-level HTTP API client interface for VaultSandbox server.
/// </summary>
internal interface IVaultSandboxApiClient : IDisposable
{
    /// <summary>
    /// Validates the API key.
    /// </summary>
    Task<CheckKeyResponse> CheckKeyAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets server configuration and public key.
    /// </summary>
    Task<ServerInfoResponse> GetServerInfoAsync(CancellationToken ct = default);

    /// <summary>
    /// Creates a new inbox.
    /// </summary>
    Task<CreateInboxResponse> CreateInboxAsync(CreateInboxRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes a specific inbox by email address.
    /// </summary>
    Task DeleteInboxAsync(string emailAddress, CancellationToken ct = default);

    /// <summary>
    /// Deletes all inboxes for the current API key.
    /// </summary>
    Task<DeleteAllInboxesResponse> DeleteAllInboxesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets inbox sync status (email count and hash).
    /// </summary>
    Task<InboxSyncResponse> GetInboxSyncAsync(string emailAddress, CancellationToken ct = default);

    /// <summary>
    /// Gets all emails in an inbox.
    /// </summary>
    Task<EmailResponse[]> GetEmailsAsync(string emailAddress, bool includeContent, CancellationToken ct = default);

    /// <summary>
    /// Gets a specific email by ID.
    /// </summary>
    Task<EmailResponse> GetEmailAsync(string emailAddress, string emailId, CancellationToken ct = default);

    /// <summary>
    /// Gets raw email data by ID.
    /// </summary>
    Task<RawEmailResponse> GetRawEmailAsync(string emailAddress, string emailId, CancellationToken ct = default);

    /// <summary>
    /// Marks an email as read.
    /// </summary>
    Task MarkEmailAsReadAsync(string emailAddress, string emailId, CancellationToken ct = default);

    /// <summary>
    /// Deletes an email.
    /// </summary>
    Task DeleteEmailAsync(string emailAddress, string emailId, CancellationToken ct = default);

    /// <summary>
    /// Opens an SSE stream for real-time email notifications.
    /// </summary>
    /// <param name="inboxHashes">Inbox hashes to subscribe to.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The HTTP response stream for SSE parsing.</returns>
    Task<Stream> GetEventsStreamAsync(IEnumerable<string> inboxHashes, CancellationToken ct = default);
}
