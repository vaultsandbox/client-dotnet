using System.Text.Json.Serialization;

namespace VaultSandbox.Client.Http.Models;

/// <summary>
/// Response from inbox sync status endpoint.
/// </summary>
public sealed record InboxSyncResponse
{
    [JsonPropertyName("emailCount")]
    public required int EmailCount { get; init; }

    [JsonPropertyName("emailsHash")]
    public required string EmailsHash { get; init; }
}
