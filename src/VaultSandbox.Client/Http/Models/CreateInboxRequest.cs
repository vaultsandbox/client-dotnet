using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace VaultSandbox.Client.Http.Models;

/// <summary>
/// Request to create a new inbox.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record CreateInboxRequest
{
    [JsonPropertyName("clientKemPk")]
    public required string ClientKemPk { get; init; }

    [JsonPropertyName("ttl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Ttl { get; init; }

    [JsonPropertyName("emailAddress")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EmailAddress { get; init; }
}
