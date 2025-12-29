namespace VaultSandbox.Client.Api;

/// <summary>
/// Exported inbox data for persistence and sharing.
/// WARNING: Contains private keys - handle securely.
/// </summary>
public sealed record InboxExport
{
    /// <summary>
    /// The email address of the inbox.
    /// </summary>
    public required string EmailAddress { get; init; }

    /// <summary>
    /// When the inbox expires.
    /// </summary>
    public required DateTimeOffset ExpiresAt { get; init; }

    /// <summary>
    /// SHA-256 hash of the client KEM public key.
    /// </summary>
    public required string InboxHash { get; init; }

    /// <summary>
    /// Base64url-encoded server signing public key.
    /// </summary>
    public required string ServerSigPk { get; init; }

    /// <summary>
    /// Base64url-encoded ML-KEM-768 public key.
    /// WARNING: Public key - safe to share.
    /// </summary>
    public required string PublicKeyB64 { get; init; }

    /// <summary>
    /// Base64url-encoded ML-KEM-768 secret key.
    /// WARNING: Private key - keep secure, never share.
    /// </summary>
    public required string SecretKeyB64 { get; init; }

    /// <summary>
    /// When this export was created.
    /// </summary>
    public required DateTimeOffset ExportedAt { get; init; }
}
