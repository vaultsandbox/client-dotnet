using System.Text.Json;
using System.Text.Json.Serialization;

namespace VaultSandbox.Client.Api;

/// <summary>
/// Individual spam rule/symbol that was triggered during analysis.
/// </summary>
public sealed record SpamSymbol
{
    /// <summary>
    /// Rule identifier (e.g., 'MISSING_HEADERS', 'DKIM_SIGNED', 'SPF_ALLOW').
    /// Rspamd symbol names follow conventions like:
    /// - Positive scores: spam indicators (e.g., 'FORGED_SENDER')
    /// - Negative scores: ham indicators (e.g., 'DKIM_SIGNED')
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Score contribution from this rule.
    /// Positive = increases spam score.
    /// Negative = decreases spam score (indicates legitimacy).
    /// </summary>
    [JsonPropertyName("score")]
    public required double Score { get; init; }

    /// <summary>
    /// Human-readable description of what this rule detects.
    /// May be null for some symbols.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>
    /// Additional context or matched values.
    /// For example, URL rules may contain the matched URLs.
    /// </summary>
    [JsonPropertyName("options")]
    public IReadOnlyList<string>? Options { get; init; }
}

/// <summary>
/// Analysis status indicating the outcome of spam analysis.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<SpamAnalysisStatus>))]
public enum SpamAnalysisStatus
{
    /// <summary>
    /// Successfully analyzed by Rspamd.
    /// </summary>
    [JsonPropertyName("analyzed")]
    Analyzed,

    /// <summary>
    /// Analysis was skipped (disabled globally or per-inbox).
    /// </summary>
    [JsonPropertyName("skipped")]
    Skipped,

    /// <summary>
    /// Analysis failed (Rspamd unavailable, timeout, etc.).
    /// </summary>
    [JsonPropertyName("error")]
    Error
}

/// <summary>
/// Recommended action from Rspamd based on score thresholds.
/// </summary>
[JsonConverter(typeof(SpamActionConverter))]
public enum SpamAction
{
    /// <summary>
    /// Email is clean, deliver normally.
    /// </summary>
    NoAction,

    /// <summary>
    /// Temporarily reject, retry later (anti-spam technique).
    /// </summary>
    Greylist,

    /// <summary>
    /// Add spam headers but deliver.
    /// </summary>
    AddHeader,

    /// <summary>
    /// Modify subject to indicate spam.
    /// </summary>
    RewriteSubject,

    /// <summary>
    /// Temporary rejection (4xx SMTP code).
    /// </summary>
    SoftReject,

    /// <summary>
    /// Permanent rejection (5xx SMTP code).
    /// </summary>
    Reject
}

/// <summary>
/// Custom JSON converter for SpamAction to handle values with spaces.
/// </summary>
internal sealed class SpamActionConverter : JsonConverter<SpamAction>
{
    public override SpamAction Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value switch
        {
            "no action" => SpamAction.NoAction,
            "greylist" => SpamAction.Greylist,
            "add header" => SpamAction.AddHeader,
            "rewrite subject" => SpamAction.RewriteSubject,
            "soft reject" => SpamAction.SoftReject,
            "reject" => SpamAction.Reject,
            _ => throw new JsonException($"Unknown SpamAction value: {value}")
        };
    }

    public override void Write(Utf8JsonWriter writer, SpamAction value, JsonSerializerOptions options)
    {
        var stringValue = value switch
        {
            SpamAction.NoAction => "no action",
            SpamAction.Greylist => "greylist",
            SpamAction.AddHeader => "add header",
            SpamAction.RewriteSubject => "rewrite subject",
            SpamAction.SoftReject => "soft reject",
            SpamAction.Reject => "reject",
            _ => throw new JsonException($"Unknown SpamAction value: {value}")
        };
        writer.WriteStringValue(stringValue);
    }
}

/// <summary>
/// Result of spam analysis for an email.
/// </summary>
public sealed record SpamAnalysisResult
{
    /// <summary>
    /// Analysis status.
    /// </summary>
    [JsonPropertyName("status")]
    public required SpamAnalysisStatus Status { get; init; }

    /// <summary>
    /// Overall spam score (positive = more spammy).
    /// Only present when Status is Analyzed.
    /// Typical range: -10 to +15, but can vary.
    /// </summary>
    [JsonPropertyName("score")]
    public double? Score { get; init; }

    /// <summary>
    /// Required score threshold for spam classification.
    /// Emails with score >= requiredScore are considered spam.
    /// Default Rspamd threshold is typically 6.0.
    /// </summary>
    [JsonPropertyName("requiredScore")]
    public double? RequiredScore { get; init; }

    /// <summary>
    /// Recommended action from Rspamd based on score thresholds.
    /// </summary>
    [JsonPropertyName("action")]
    public SpamAction? Action { get; init; }

    /// <summary>
    /// Whether the email is classified as spam.
    /// True when score >= requiredScore.
    /// </summary>
    [JsonPropertyName("isSpam")]
    public bool? IsSpam { get; init; }

    /// <summary>
    /// List of triggered spam rules/symbols with their scores.
    /// Each symbol represents a specific spam indicator detected.
    /// </summary>
    [JsonPropertyName("symbols")]
    public IReadOnlyList<SpamSymbol>? Symbols { get; init; }

    /// <summary>
    /// Time taken for spam analysis in milliseconds.
    /// Useful for performance monitoring.
    /// </summary>
    [JsonPropertyName("processingTimeMs")]
    public int? ProcessingTimeMs { get; init; }

    /// <summary>
    /// Additional information about the analysis.
    /// Contains error messages when Status is Error.
    /// Contains skip reason when Status is Skipped.
    /// </summary>
    [JsonPropertyName("info")]
    public string? Info { get; init; }
}
