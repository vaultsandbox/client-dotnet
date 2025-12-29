using System.Text.Json.Serialization;

namespace VaultSandbox.Client.Http.Models;

/// <summary>
/// Response from the /api/server-info endpoint.
/// </summary>
public sealed record ServerInfoResponse
{
    [JsonPropertyName("serverSigPk")]
    public required string ServerSigPk { get; init; }

    [JsonPropertyName("algs")]
    public required AlgorithmInfo Algorithms { get; init; }

    [JsonPropertyName("context")]
    public required string Context { get; init; }

    [JsonPropertyName("maxTtl")]
    public required int MaxTtl { get; init; }

    [JsonPropertyName("defaultTtl")]
    public required int DefaultTtl { get; init; }

    [JsonPropertyName("sseConsole")]
    public required bool SseConsole { get; init; }

    [JsonPropertyName("allowedDomains")]
    public required string[] AllowedDomains { get; init; }
}

/// <summary>
/// Algorithm configuration from server.
/// </summary>
public sealed record AlgorithmInfo
{
    [JsonPropertyName("kem")]
    public required string Kem { get; init; }

    [JsonPropertyName("sig")]
    public required string Sig { get; init; }

    [JsonPropertyName("aead")]
    public required string Aead { get; init; }

    [JsonPropertyName("kdf")]
    public required string Kdf { get; init; }
}
