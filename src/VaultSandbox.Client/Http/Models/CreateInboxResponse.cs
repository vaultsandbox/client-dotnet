using System.Text.Json.Serialization;

namespace VaultSandbox.Client.Http.Models;

/// <summary>
/// Response from inbox creation.
/// </summary>
public sealed record CreateInboxResponse
{
    [JsonPropertyName("emailAddress")]
    public required string EmailAddress { get; init; }

    [JsonPropertyName("expiresAt")]
    public required DateTimeOffset ExpiresAt { get; init; }

    [JsonPropertyName("inboxHash")]
    public required string InboxHash { get; init; }

    [JsonPropertyName("serverSigPk")]
    public required string ServerSigPk { get; init; }
}
