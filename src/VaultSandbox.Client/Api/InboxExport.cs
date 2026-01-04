using System.Text.Json.Serialization;

namespace VaultSandbox.Client.Api;

/// <summary>
/// Exported inbox data for persistence and sharing.
/// WARNING: Contains private keys - handle securely.
/// Conforms to VaultSandbox spec Section 9 (Inbox Export Format).
/// </summary>
public sealed record InboxExport
{
    /// <summary>
    /// Export format version. MUST be 1.
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; init; } = 1;

    /// <summary>
    /// The email address of the inbox.
    /// </summary>
    [JsonPropertyName("emailAddress")]
    public required string EmailAddress { get; init; }

    /// <summary>
    /// When the inbox expires (ISO 8601 format).
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public required DateTimeOffset ExpiresAt { get; init; }

    /// <summary>
    /// Unique inbox identifier (SHA-256 hash of the client KEM public key).
    /// </summary>
    [JsonPropertyName("inboxHash")]
    public required string InboxHash { get; init; }

    /// <summary>
    /// Base64url-encoded server's ML-DSA-65 signing public key (1952 bytes decoded).
    /// </summary>
    [JsonPropertyName("serverSigPk")]
    public required string ServerSigPk { get; init; }

    /// <summary>
    /// Base64url-encoded ML-KEM-768 secret key (2400 bytes decoded).
    /// WARNING: Private key - keep secure, never share.
    /// The public key can be derived from this (bytes 1152-2400).
    /// </summary>
    [JsonPropertyName("secretKey")]
    public required string SecretKey { get; init; }

    /// <summary>
    /// When this export was created (ISO 8601 format).
    /// </summary>
    [JsonPropertyName("exportedAt")]
    public required DateTimeOffset ExportedAt { get; init; }
}
