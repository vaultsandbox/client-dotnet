using System.Text.Json.Serialization;
using VaultSandbox.Client.Crypto;

namespace VaultSandbox.Client.Http.Models;

/// <summary>
/// Response containing an email. Can be either encrypted or plain based on inbox configuration.
/// Use <see cref="IsEncrypted"/> to determine the format.
/// </summary>
public sealed record EmailResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("inboxId")]
    public string? InboxId { get; init; }

    [JsonPropertyName("receivedAt")]
    public DateTimeOffset? ReceivedAt { get; init; }

    [JsonPropertyName("isRead")]
    public bool IsRead { get; init; }

    // --- Encrypted format fields ---

    /// <summary>
    /// Encrypted metadata. Present when inbox uses encryption.
    /// </summary>
    [JsonPropertyName("encryptedMetadata")]
    public EncryptedPayload? EncryptedMetadata { get; init; }

    /// <summary>
    /// Encrypted parsed content. May be null when listing emails (only metadata is returned).
    /// Present when inbox uses encryption.
    /// </summary>
    [JsonPropertyName("encryptedParsed")]
    public EncryptedPayload? EncryptedParsed { get; init; }

    // --- Plain format fields ---

    /// <summary>
    /// Base64-encoded JSON metadata. Present when inbox is plain (not encrypted).
    /// </summary>
    [JsonPropertyName("metadata")]
    public string? Metadata { get; init; }

    /// <summary>
    /// Base64-encoded JSON parsed content. Present when inbox is plain (not encrypted).
    /// May be null when listing emails (only metadata is returned).
    /// </summary>
    [JsonPropertyName("parsed")]
    public string? Parsed { get; init; }

    /// <summary>
    /// Whether this email response is encrypted. Use field presence to discriminate:
    /// encrypted emails have <see cref="EncryptedMetadata"/>, plain emails have <see cref="Metadata"/>.
    /// </summary>
    public bool IsEncrypted => EncryptedMetadata is not null;
}

/// <summary>
/// Decrypted email metadata.
/// </summary>
public sealed record DecryptedMetadata
{
    [JsonPropertyName("from")]
    public required string From { get; init; }

    /// <summary>
    /// Recipients - can be a single string or an array from the server.
    /// Use StringOrArrayConverter to handle both cases.
    /// </summary>
    [JsonPropertyName("to")]
    [JsonConverter(typeof(StringOrArrayConverter))]
    public required string[] To { get; init; }

    [JsonPropertyName("subject")]
    public required string Subject { get; init; }

    /// <summary>
    /// Optional - may come from EmailResponse.ReceivedAt instead.
    /// </summary>
    [JsonPropertyName("receivedAt")]
    public DateTimeOffset? ReceivedAt { get; init; }
}
