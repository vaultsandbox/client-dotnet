using System.Text.Json.Serialization;
using VaultSandbox.Client.Crypto;

namespace VaultSandbox.Client.Http.Models;

/// <summary>
/// Response containing raw encrypted email data.
/// </summary>
public sealed record RawEmailResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("encryptedRaw")]
    public required EncryptedPayload EncryptedRaw { get; init; }
}
