using System.Diagnostics.CodeAnalysis;

namespace VaultSandbox.Client.Api;

/// <summary>
/// Options for creating a new inbox.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateInboxOptions
{
    /// <summary>
    /// Desired email address or domain.
    /// If not specified, a random address is generated.
    /// </summary>
    public string? EmailAddress { get; set; }

    /// <summary>
    /// Time-to-live for the inbox.
    /// Default: 1 hour. Min: 60 seconds. Max: 7 days.
    /// </summary>
    public TimeSpan? Ttl { get; set; }

    /// <summary>
    /// Whether to enable email authentication checks (SPF, DKIM, DMARC, PTR) for this inbox.
    /// If not specified, the server default is used.
    /// </summary>
    public bool? EmailAuth { get; set; }

    /// <summary>
    /// Requested encryption mode for the inbox.
    /// If not specified, the server uses its default based on the encryption policy.
    /// Only applicable when the server policy allows overrides ('enabled' or 'disabled').
    /// </summary>
    public InboxEncryption? Encryption { get; set; }
}

/// <summary>
/// Encryption mode options for inbox creation.
/// </summary>
public enum InboxEncryption
{
    /// <summary>
    /// Request an encrypted inbox (emails are encrypted with ML-KEM-768).
    /// </summary>
    Encrypted,

    /// <summary>
    /// Request a plain inbox (emails are stored unencrypted).
    /// </summary>
    Plain
}
