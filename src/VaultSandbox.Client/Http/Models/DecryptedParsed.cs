using System.Text.Json;
using System.Text.Json.Serialization;
using VaultSandbox.Client.Api;

namespace VaultSandbox.Client.Http.Models;

/// <summary>
/// Decrypted parsed email content.
/// </summary>
public sealed record DecryptedParsed
{
    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("html")]
    public string? Html { get; init; }

    /// <summary>
    /// Email headers - values can be strings or complex objects.
    /// </summary>
    [JsonPropertyName("headers")]
    public Dictionary<string, JsonElement>? Headers { get; init; }

    [JsonPropertyName("attachments")]
    public AttachmentData[]? Attachments { get; init; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; init; }

    [JsonPropertyName("links")]
    public string[]? Links { get; init; }

    [JsonPropertyName("authResults")]
    public AuthenticationResults? AuthResults { get; init; }
}

/// <summary>
/// Email attachment data.
/// </summary>
public sealed record AttachmentData
{
    [JsonPropertyName("filename")]
    public required string Filename { get; init; }

    [JsonPropertyName("contentType")]
    public required string ContentType { get; init; }

    [JsonPropertyName("size")]
    public required int Size { get; init; }

    [JsonPropertyName("contentId")]
    public string? ContentId { get; init; }

    [JsonPropertyName("contentDisposition")]
    public string? ContentDisposition { get; init; }

    /// <summary>
    /// Base64 encoded content.
    /// </summary>
    [JsonPropertyName("content")]
    public required string Content { get; init; }

    [JsonPropertyName("checksum")]
    public string? Checksum { get; init; }
}
