using System.Text.Json.Serialization;
using VaultSandbox.Client.Crypto;

namespace VaultSandbox.Client.Http.Models;

/// <summary>
/// Response containing an encrypted email.
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

    [JsonPropertyName("encryptedMetadata")]
    public required EncryptedPayload EncryptedMetadata { get; init; }

    /// <summary>
    /// Encrypted parsed content. May be null when listing emails (only metadata is returned).
    /// </summary>
    [JsonPropertyName("encryptedParsed")]
    public EncryptedPayload? EncryptedParsed { get; init; }
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
