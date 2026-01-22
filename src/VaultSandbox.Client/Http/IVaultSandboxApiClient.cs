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

    // --- Inbox Webhooks ---

    /// <summary>
    /// Creates a webhook for a specific inbox.
    /// </summary>
    Task<WebhookResponse> CreateInboxWebhookAsync(string emailAddress, CreateWebhookRequest request, CancellationToken ct = default);

    /// <summary>
    /// Lists all webhooks for a specific inbox.
    /// </summary>
    Task<WebhookListResponse> ListInboxWebhooksAsync(string emailAddress, CancellationToken ct = default);

    /// <summary>
    /// Gets a specific webhook by ID for an inbox.
    /// </summary>
    Task<WebhookResponse> GetInboxWebhookAsync(string emailAddress, string webhookId, CancellationToken ct = default);

    /// <summary>
    /// Updates a webhook for a specific inbox.
    /// </summary>
    Task<WebhookResponse> UpdateInboxWebhookAsync(string emailAddress, string webhookId, UpdateWebhookRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes a webhook for a specific inbox.
    /// </summary>
    Task DeleteInboxWebhookAsync(string emailAddress, string webhookId, CancellationToken ct = default);

    /// <summary>
    /// Tests a webhook by sending a test event.
    /// </summary>
    Task<TestWebhookResponse> TestInboxWebhookAsync(string emailAddress, string webhookId, CancellationToken ct = default);

    /// <summary>
    /// Rotates the signing secret for a webhook.
    /// </summary>
    Task<RotateSecretResponse> RotateInboxWebhookSecretAsync(string emailAddress, string webhookId, CancellationToken ct = default);

    // --- Inbox Chaos Configuration ---

    /// <summary>
    /// Gets the chaos configuration for a specific inbox.
    /// </summary>
    Task<ChaosConfigResponse> GetInboxChaosConfigAsync(string emailAddress, CancellationToken ct = default);

    /// <summary>
    /// Sets the chaos configuration for a specific inbox.
    /// </summary>
    Task<ChaosConfigResponse> SetInboxChaosConfigAsync(string emailAddress, ChaosConfigRequest request, CancellationToken ct = default);

    /// <summary>
    /// Disables all chaos for a specific inbox.
    /// </summary>
    Task DeleteInboxChaosConfigAsync(string emailAddress, CancellationToken ct = default);
}
