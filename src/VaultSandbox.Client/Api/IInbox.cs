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
    /// Whether this inbox uses encryption.
    /// When true, emails are encrypted with ML-KEM-768 and need to be decrypted.
    /// When false, emails are plain and only need Base64 decoding.
    /// </summary>
    bool Encrypted { get; }

    /// <summary>
    /// Whether email authentication checks (SPF, DKIM, DMARC, PTR) are enabled for this inbox.
    /// When false, all authentication results return Skipped status.
    /// </summary>
    bool EmailAuth { get; }

    /// <summary>
    /// Whether this inbox has been disposed.
    /// </summary>
    bool IsDisposed { get; }

    /// <summary>
    /// Gets all emails in the inbox with full content.
    /// </summary>
    Task<IReadOnlyList<Email>> GetEmailsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets all emails in the inbox with metadata only (no body content).
    /// </summary>
    Task<IReadOnlyList<EmailMetadata>> GetEmailsMetadataOnlyAsync(CancellationToken ct = default);

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

    // --- Webhook Operations ---

    /// <summary>
    /// Creates a webhook for this inbox.
    /// </summary>
    /// <param name="options">Webhook configuration options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created webhook with its signing secret.</returns>
    Task<Webhook> CreateWebhookAsync(CreateWebhookOptions options, CancellationToken ct = default);

    /// <summary>
    /// Lists all webhooks for this inbox.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of webhooks (secrets are not included in list responses).</returns>
    Task<IReadOnlyList<Webhook>> ListWebhooksAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a specific webhook by ID.
    /// </summary>
    /// <param name="webhookId">The webhook ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The webhook with its signing secret and delivery statistics.</returns>
    Task<Webhook> GetWebhookAsync(string webhookId, CancellationToken ct = default);

    /// <summary>
    /// Updates a webhook.
    /// </summary>
    /// <param name="webhookId">The webhook ID.</param>
    /// <param name="options">Update options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated webhook.</returns>
    Task<Webhook> UpdateWebhookAsync(string webhookId, UpdateWebhookOptions options, CancellationToken ct = default);

    /// <summary>
    /// Deletes a webhook.
    /// </summary>
    /// <param name="webhookId">The webhook ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteWebhookAsync(string webhookId, CancellationToken ct = default);

    /// <summary>
    /// Tests a webhook by sending a test event.
    /// </summary>
    /// <param name="webhookId">The webhook ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Test result including response from the endpoint.</returns>
    Task<WebhookTestResult> TestWebhookAsync(string webhookId, CancellationToken ct = default);

    /// <summary>
    /// Rotates the signing secret for a webhook.
    /// The old secret remains valid for 1 hour.
    /// </summary>
    /// <param name="webhookId">The webhook ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The new secret and grace period information.</returns>
    Task<WebhookSecretRotation> RotateWebhookSecretAsync(string webhookId, CancellationToken ct = default);

    // --- Chaos Configuration Operations ---

    /// <summary>
    /// Gets the chaos configuration for this inbox.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The current chaos configuration.</returns>
    /// <exception cref="VaultSandbox.Client.Exceptions.ApiException">
    /// Thrown with status code 403 if chaos is disabled globally on the server.
    /// </exception>
    Task<ChaosConfig> GetChaosConfigAsync(CancellationToken ct = default);

    /// <summary>
    /// Sets the chaos configuration for this inbox.
    /// </summary>
    /// <param name="options">The chaos configuration options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated chaos configuration.</returns>
    /// <exception cref="VaultSandbox.Client.Exceptions.ApiException">
    /// Thrown with status code 403 if chaos is disabled globally on the server.
    /// </exception>
    Task<ChaosConfig> SetChaosConfigAsync(SetChaosConfigOptions options, CancellationToken ct = default);

    /// <summary>
    /// Disables all chaos for this inbox.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="VaultSandbox.Client.Exceptions.ApiException">
    /// Thrown with status code 403 if chaos is disabled globally on the server.
    /// </exception>
    Task DisableChaosAsync(CancellationToken ct = default);
}
