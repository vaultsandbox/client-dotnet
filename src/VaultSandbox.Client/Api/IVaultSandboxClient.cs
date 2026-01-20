using System.Text.Json.Serialization;

namespace VaultSandbox.Client.Api;

/// <summary>
/// Main entry point for the VaultSandbox client SDK.
/// </summary>
public interface IVaultSandboxClient : IAsyncDisposable
{
    /// <summary>
    /// Creates a new email inbox with auto-generated ML-KEM-768 keypair.
    /// </summary>
    /// <param name="options">Optional inbox creation settings.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created inbox.</returns>
    Task<IInbox> CreateInboxAsync(CreateInboxOptions? options = null, CancellationToken ct = default);

    /// <summary>
    /// Deletes an inbox by email address.
    /// </summary>
    /// <param name="emailAddress">The inbox email address to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteInboxAsync(string emailAddress, CancellationToken ct = default);

    /// <summary>
    /// Deletes all inboxes associated with the API key.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Number of inboxes deleted.</returns>
    Task<int> DeleteAllInboxesAsync(CancellationToken ct = default);

    /// <summary>
    /// Imports a previously exported inbox.
    /// WARNING: Import data contains private keys - validate source.
    /// </summary>
    /// <param name="export">The exported inbox data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The restored inbox.</returns>
    Task<IInbox> ImportInboxAsync(InboxExport export, CancellationToken ct = default);

    /// <summary>
    /// Validates the API key.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the API key is valid.</returns>
    Task<bool> ValidateApiKeyAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets server information including cryptographic configuration.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task<ServerInfo> GetServerInfoAsync(CancellationToken ct = default);

    /// <summary>
    /// Creates a monitor to watch multiple inboxes for new emails.
    /// </summary>
    /// <param name="inboxes">The inboxes to monitor.</param>
    /// <returns>An inbox monitor that provides a unified email stream.</returns>
    /// <exception cref="ArgumentException">Thrown if no inboxes provided.</exception>
    InboxMonitor MonitorInboxes(IReadOnlyList<IInbox> inboxes);

    /// <summary>
    /// Creates a monitor to watch multiple inboxes for new emails.
    /// </summary>
    /// <param name="inboxes">The inboxes to monitor.</param>
    /// <returns>An inbox monitor that provides a unified email stream.</returns>
    InboxMonitor MonitorInboxes(params IInbox[] inboxes);

    /// <summary>
    /// Exports an inbox to a JSON file.
    /// </summary>
    /// <param name="inbox">The inbox to export.</param>
    /// <param name="filePath">The file path to write to.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="IOException">Thrown if the file cannot be written.</exception>
    Task ExportInboxToFileAsync(
        IInbox inbox,
        string filePath,
        CancellationToken ct = default);

    /// <summary>
    /// Imports an inbox from a JSON file.
    /// </summary>
    /// <param name="filePath">The file path to read from.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The restored inbox.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
    /// <exception cref="VaultSandbox.Client.Exceptions.InvalidImportDataException">Thrown if the file contains invalid data.</exception>
    Task<IInbox> ImportInboxFromFileAsync(
        string filePath,
        CancellationToken ct = default);
}

/// <summary>
/// Server information and configuration.
/// </summary>
public sealed record ServerInfo
{
    public required string ServerSigPk { get; init; }
    public required string Context { get; init; }
    public required int MaxTtl { get; init; }
    public required int DefaultTtl { get; init; }
    public required bool SseConsole { get; init; }
    public required IReadOnlyList<string> AllowedDomains { get; init; }

    /// <summary>
    /// Server's encryption policy for inboxes.
    /// </summary>
    public required EncryptionPolicy EncryptionPolicy { get; init; }

    /// <summary>
    /// Whether spam analysis (Rspamd) is enabled on this server.
    /// </summary>
    public bool SpamAnalysisEnabled { get; init; }

    /// <summary>
    /// Whether per-inbox encryption override is allowed (policy is 'enabled' or 'disabled').
    /// </summary>
    public bool CanOverrideEncryption => EncryptionPolicy is EncryptionPolicy.Enabled or EncryptionPolicy.Disabled;

    /// <summary>
    /// Whether encryption is enabled by default (policy is 'always' or 'enabled').
    /// </summary>
    public bool DefaultEncrypted => EncryptionPolicy is EncryptionPolicy.Always or EncryptionPolicy.Enabled;
}

/// <summary>
/// Server encryption policy for inbox creation.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<EncryptionPolicy>))]
public enum EncryptionPolicy
{
    /// <summary>
    /// All inboxes are encrypted. Per-inbox override is not allowed.
    /// </summary>
    [JsonPropertyName("always")]
    Always,

    /// <summary>
    /// Inboxes are encrypted by default. Can request plain inboxes.
    /// </summary>
    [JsonPropertyName("enabled")]
    Enabled,

    /// <summary>
    /// Inboxes are plain by default. Can request encrypted inboxes.
    /// </summary>
    [JsonPropertyName("disabled")]
    Disabled,

    /// <summary>
    /// All inboxes are plain. Per-inbox override is not allowed.
    /// </summary>
    [JsonPropertyName("never")]
    Never
}
