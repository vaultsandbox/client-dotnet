using System.Text.Json.Serialization;

namespace VaultSandbox.Client.Crypto;

/// <summary>
/// Represents an encrypted payload from the server.
/// </summary>
public sealed record EncryptedPayload
{
    /// <summary>
    /// Payload version (currently 1).
    /// </summary>
    [JsonPropertyName("v")]
    public required int Version { get; init; }

    /// <summary>
    /// Algorithm identifiers.
    /// </summary>
    [JsonPropertyName("algs")]
    public required AlgorithmSuite Algorithms { get; init; }

    /// <summary>
    /// Base64url-encoded KEM ciphertext.
    /// </summary>
    [JsonPropertyName("ct_kem")]
    public required string CtKem { get; init; }

    /// <summary>
    /// Base64url-encoded 12-byte nonce.
    /// </summary>
    [JsonPropertyName("nonce")]
    public required string Nonce { get; init; }

    /// <summary>
    /// Base64url-encoded additional authenticated data.
    /// </summary>
    [JsonPropertyName("aad")]
    public required string Aad { get; init; }

    /// <summary>
    /// Base64url-encoded AES-GCM ciphertext with appended tag.
    /// </summary>
    [JsonPropertyName("ciphertext")]
    public required string Ciphertext { get; init; }

    /// <summary>
    /// Base64url-encoded ML-DSA-65 signature.
    /// </summary>
    [JsonPropertyName("sig")]
    public required string Signature { get; init; }

    /// <summary>
    /// Base64url-encoded server's signing public key.
    /// </summary>
    [JsonPropertyName("server_sig_pk")]
    public required string ServerSigPk { get; init; }
}

/// <summary>
/// Algorithm suite identifiers.
/// </summary>
public sealed record AlgorithmSuite
{
    [JsonPropertyName("kem")]
    public required string Kem { get; init; }

    [JsonPropertyName("sig")]
    public required string Sig { get; init; }

    [JsonPropertyName("aead")]
    public required string Aead { get; init; }

    [JsonPropertyName("kdf")]
    public required string Kdf { get; init; }

    /// <summary>
    /// Returns the ciphersuite string for transcript construction.
    /// </summary>
    public string ToCiphersuiteString() => $"{Kem}:{Sig}:{Aead}:{Kdf}";
}
