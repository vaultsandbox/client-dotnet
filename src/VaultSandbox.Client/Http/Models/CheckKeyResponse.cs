using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace VaultSandbox.Client.Http.Models;

/// <summary>
/// Response from the /api/check-key endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record CheckKeyResponse
{
    [JsonPropertyName("ok")]
    public required bool Ok { get; init; }
}
