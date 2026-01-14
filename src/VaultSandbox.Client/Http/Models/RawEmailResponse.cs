using System.Text.Json.Serialization;
using VaultSandbox.Client.Crypto;

namespace VaultSandbox.Client.Http.Models;

/// <summary>
/// Response containing raw email data. Can be either encrypted or plain based on inbox configuration.
/// Use <see cref="IsEncrypted"/> to determine the format.
/// </summary>
public sealed record RawEmailResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Encrypted raw email content. Present when inbox uses encryption.
    /// </summary>
    [JsonPropertyName("encryptedRaw")]
    public EncryptedPayload? EncryptedRaw { get; init; }

    /// <summary>
    /// Base64-encoded raw email content. Present when inbox is plain (not encrypted).
    /// </summary>
    [JsonPropertyName("raw")]
    public string? Raw { get; init; }

    /// <summary>
    /// Whether this raw email response is encrypted. Use field presence to discriminate:
    /// encrypted emails have <see cref="EncryptedRaw"/>, plain emails have <see cref="Raw"/>.
    /// </summary>
    public bool IsEncrypted => EncryptedRaw is not null;
}
