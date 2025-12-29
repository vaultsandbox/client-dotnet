using System.Text.Json.Serialization;

namespace VaultSandbox.Client.Http.Models;

/// <summary>
/// Response from deleting all inboxes.
/// </summary>
public sealed record DeleteAllInboxesResponse
{
    [JsonPropertyName("deleted")]
    public required int Deleted { get; init; }
}
