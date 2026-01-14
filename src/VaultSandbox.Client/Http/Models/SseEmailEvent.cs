using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using VaultSandbox.Client.Crypto;

namespace VaultSandbox.Client.Http.Models;

/// <summary>
/// Server-Sent Event for new email notification. Can be either encrypted or plain based on inbox configuration.
/// Use <see cref="IsEncrypted"/> to determine the format.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record SseEmailEvent
{
    [JsonPropertyName("inboxId")]
    public required string InboxId { get; init; }

    [JsonPropertyName("emailId")]
    public required string EmailId { get; init; }

    /// <summary>
    /// Encrypted metadata. Present when inbox uses encryption.
    /// </summary>
    [JsonPropertyName("encryptedMetadata")]
    public EncryptedPayload? EncryptedMetadata { get; init; }

    /// <summary>
    /// Base64-encoded JSON metadata. Present when inbox is plain (not encrypted).
    /// </summary>
    [JsonPropertyName("metadata")]
    public string? Metadata { get; init; }

    /// <summary>
    /// Whether this event is encrypted. Use field presence to discriminate:
    /// encrypted events have <see cref="EncryptedMetadata"/>, plain events have <see cref="Metadata"/>.
    /// </summary>
    public bool IsEncrypted => EncryptedMetadata is not null;
}
