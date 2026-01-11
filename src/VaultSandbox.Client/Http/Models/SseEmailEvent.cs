using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using VaultSandbox.Client.Crypto;

namespace VaultSandbox.Client.Http.Models;

/// <summary>
/// Server-Sent Event for new email notification.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record SseEmailEvent
{
    [JsonPropertyName("inboxId")]
    public required string InboxId { get; init; }

    [JsonPropertyName("emailId")]
    public required string EmailId { get; init; }

    [JsonPropertyName("encryptedMetadata")]
    public required EncryptedPayload EncryptedMetadata { get; init; }
}
