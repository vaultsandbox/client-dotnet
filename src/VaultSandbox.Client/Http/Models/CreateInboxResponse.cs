using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace VaultSandbox.Client.Http.Models;

/// <summary>
/// Response from inbox creation.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record CreateInboxResponse
{
    [JsonPropertyName("emailAddress")]
    public required string EmailAddress { get; init; }

    [JsonPropertyName("expiresAt")]
    public required DateTimeOffset ExpiresAt { get; init; }

    [JsonPropertyName("inboxHash")]
    public required string InboxHash { get; init; }

    /// <summary>
    /// Server's ML-DSA-65 signing public key.
    /// Only present when the inbox is encrypted.
    /// </summary>
    [JsonPropertyName("serverSigPk")]
    public string? ServerSigPk { get; init; }

    /// <summary>
    /// Whether email authentication checks are enabled for this inbox.
    /// </summary>
    [JsonPropertyName("emailAuth")]
    public bool EmailAuth { get; init; }

    /// <summary>
    /// Whether the inbox uses encryption.
    /// </summary>
    [JsonPropertyName("encrypted")]
    public bool Encrypted { get; init; } = true;
}
