using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace VaultSandbox.Client.Http.Models;

/// <summary>
/// Request to create a new inbox.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record CreateInboxRequest
{
    /// <summary>
    /// Client's ML-KEM-768 public key (base64url-encoded).
    /// Required when encryption is enabled for the inbox.
    /// </summary>
    [JsonPropertyName("clientKemPk")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ClientKemPk { get; init; }

    [JsonPropertyName("ttl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Ttl { get; init; }

    [JsonPropertyName("emailAddress")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EmailAddress { get; init; }

    /// <summary>
    /// Whether to enable email authentication checks (SPF, DKIM, DMARC, PTR) for this inbox.
    /// If not specified, the server default is used.
    /// </summary>
    [JsonPropertyName("emailAuth")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? EmailAuth { get; init; }

    /// <summary>
    /// Requested encryption mode for the inbox. If omitted, the server uses its default based on policy.
    /// Only applicable when the server policy allows overrides ('enabled' or 'disabled').
    /// </summary>
    [JsonPropertyName("encryption")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Encryption { get; init; }

    /// <summary>
    /// Spam analysis preference for this inbox. If omitted, the server default is used.
    /// </summary>
    [JsonPropertyName("spamAnalysis")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? SpamAnalysis { get; init; }
}
