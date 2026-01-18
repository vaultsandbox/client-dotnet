using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using VaultSandbox.Client.Api;

namespace VaultSandbox.Client.Http.Models;

/// <summary>
/// Request to create a new webhook.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record CreateWebhookRequest
{
    /// <summary>
    /// Target URL (HTTPS required in production).
    /// </summary>
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    /// <summary>
    /// Event types to subscribe to (max 10).
    /// </summary>
    [JsonPropertyName("events")]
    public required WebhookEventType[] Events { get; init; }

    /// <summary>
    /// Optional payload template name or custom template.
    /// </summary>
    [JsonPropertyName("template")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Template { get; init; }

    /// <summary>
    /// Optional event filter configuration.
    /// </summary>
    [JsonPropertyName("filter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FilterConfigRequest? Filter { get; init; }

    /// <summary>
    /// Optional description (max 500 chars).
    /// </summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; init; }
}

/// <summary>
/// Request to update a webhook.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record UpdateWebhookRequest
{
    /// <summary>
    /// Target URL (HTTPS required in production).
    /// </summary>
    [JsonPropertyName("url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Url { get; init; }

    /// <summary>
    /// Event types to subscribe to (max 10).
    /// </summary>
    [JsonPropertyName("events")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WebhookEventType[]? Events { get; init; }

    /// <summary>
    /// Template configuration. Set to null to remove.
    /// </summary>
    [JsonPropertyName("template")]
    public object? Template { get; init; }

    /// <summary>
    /// Has the Template property been explicitly set?
    /// </summary>
    [JsonIgnore]
    public bool TemplateWasSet { get; init; }

    /// <summary>
    /// Filter configuration. Set to null to remove.
    /// </summary>
    [JsonPropertyName("filter")]
    public FilterConfigRequest? Filter { get; init; }

    /// <summary>
    /// Has the Filter property been explicitly set?
    /// </summary>
    [JsonIgnore]
    public bool FilterWasSet { get; init; }

    /// <summary>
    /// Optional description (max 500 chars).
    /// </summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; init; }

    /// <summary>
    /// Whether the webhook is active.
    /// </summary>
    [JsonPropertyName("enabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Enabled { get; init; }
}

/// <summary>
/// Filter configuration for webhooks.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record FilterConfigRequest
{
    /// <summary>
    /// Filter rules (max 10).
    /// </summary>
    [JsonPropertyName("rules")]
    public required FilterRuleRequest[] Rules { get; init; }

    /// <summary>
    /// 'all' = AND logic, 'any' = OR logic.
    /// </summary>
    [JsonPropertyName("mode")]
    public required FilterMode Mode { get; init; }

    /// <summary>
    /// Require email to pass SPF/DKIM/DMARC checks.
    /// </summary>
    [JsonPropertyName("requireAuth")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? RequireAuth { get; init; }
}

/// <summary>
/// A single filter rule.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record FilterRuleRequest
{
    /// <summary>
    /// Field to filter on.
    /// </summary>
    [JsonPropertyName("field")]
    public required FilterableField Field { get; init; }

    /// <summary>
    /// Comparison operator.
    /// </summary>
    [JsonPropertyName("operator")]
    public required FilterOperator Operator { get; init; }

    /// <summary>
    /// Value to match (max 1000 chars).
    /// </summary>
    [JsonPropertyName("value")]
    public required string Value { get; init; }

    /// <summary>
    /// Case-sensitive matching (default: false).
    /// </summary>
    [JsonPropertyName("caseSensitive")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? CaseSensitive { get; init; }
}

/// <summary>
/// Custom template configuration.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record CustomTemplateRequest
{
    /// <summary>
    /// Must be 'custom'.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type => "custom";

    /// <summary>
    /// JSON template with {{variable}} placeholders (max 10000 chars).
    /// </summary>
    [JsonPropertyName("body")]
    public required string Body { get; init; }

    /// <summary>
    /// Optional Content-Type header override.
    /// </summary>
    [JsonPropertyName("contentType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ContentType { get; init; }
}

/// <summary>
/// Response containing webhook details.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record WebhookResponse
{
    /// <summary>
    /// Webhook ID (whk_ prefix).
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Target URL.
    /// </summary>
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    /// <summary>
    /// Subscribed event types.
    /// </summary>
    [JsonPropertyName("events")]
    public required WebhookEventType[] Events { get; init; }

    /// <summary>
    /// 'global' or 'inbox'.
    /// </summary>
    [JsonPropertyName("scope")]
    public required WebhookScope Scope { get; init; }

    /// <summary>
    /// Inbox email (inbox webhooks only).
    /// </summary>
    [JsonPropertyName("inboxEmail")]
    public string? InboxEmail { get; init; }

    /// <summary>
    /// Inbox hash (inbox webhooks only).
    /// </summary>
    [JsonPropertyName("inboxHash")]
    public string? InboxHash { get; init; }

    /// <summary>
    /// Whether webhook is active.
    /// </summary>
    [JsonPropertyName("enabled")]
    public required bool Enabled { get; init; }

    /// <summary>
    /// Signing secret (whsec_ prefix) - only on create/get, not list.
    /// </summary>
    [JsonPropertyName("secret")]
    public string? Secret { get; init; }

    /// <summary>
    /// Template configuration.
    /// </summary>
    [JsonPropertyName("template")]
    public object? Template { get; init; }

    /// <summary>
    /// Filter configuration.
    /// </summary>
    [JsonPropertyName("filter")]
    public FilterConfigResponse? Filter { get; init; }

    /// <summary>
    /// Human-readable description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>
    /// ISO timestamp.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// ISO timestamp of last update.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>
    /// ISO timestamp of last delivery attempt.
    /// </summary>
    [JsonPropertyName("lastDeliveryAt")]
    public DateTimeOffset? LastDeliveryAt { get; init; }

    /// <summary>
    /// Status of last delivery.
    /// </summary>
    [JsonPropertyName("lastDeliveryStatus")]
    public WebhookDeliveryStatus? LastDeliveryStatus { get; init; }

    /// <summary>
    /// Delivery statistics (only on get, not list).
    /// </summary>
    [JsonPropertyName("stats")]
    public WebhookStatsResponse? Stats { get; init; }
}

/// <summary>
/// Filter configuration in response.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record FilterConfigResponse
{
    /// <summary>
    /// Filter rules.
    /// </summary>
    [JsonPropertyName("rules")]
    public required FilterRuleResponse[] Rules { get; init; }

    /// <summary>
    /// 'all' = AND logic, 'any' = OR logic.
    /// </summary>
    [JsonPropertyName("mode")]
    public required FilterMode Mode { get; init; }

    /// <summary>
    /// Require email to pass SPF/DKIM/DMARC checks.
    /// </summary>
    [JsonPropertyName("requireAuth")]
    public bool? RequireAuth { get; init; }
}

/// <summary>
/// A single filter rule in response.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record FilterRuleResponse
{
    /// <summary>
    /// Field to filter on.
    /// </summary>
    [JsonPropertyName("field")]
    public required FilterableField Field { get; init; }

    /// <summary>
    /// Comparison operator.
    /// </summary>
    [JsonPropertyName("operator")]
    public required FilterOperator Operator { get; init; }

    /// <summary>
    /// Value to match.
    /// </summary>
    [JsonPropertyName("value")]
    public required string Value { get; init; }

    /// <summary>
    /// Case-sensitive matching.
    /// </summary>
    [JsonPropertyName("caseSensitive")]
    public bool? CaseSensitive { get; init; }
}

/// <summary>
/// Response containing a list of webhooks.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record WebhookListResponse
{
    /// <summary>
    /// List of webhooks.
    /// </summary>
    [JsonPropertyName("webhooks")]
    public required WebhookResponse[] Webhooks { get; init; }

    /// <summary>
    /// Total number of webhooks.
    /// </summary>
    [JsonPropertyName("total")]
    public required int Total { get; init; }
}

/// <summary>
/// Response from testing a webhook.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record TestWebhookResponse
{
    /// <summary>
    /// Whether test succeeded.
    /// </summary>
    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    /// <summary>
    /// HTTP status code from endpoint.
    /// </summary>
    [JsonPropertyName("statusCode")]
    public int? StatusCode { get; init; }

    /// <summary>
    /// Response time in milliseconds.
    /// </summary>
    [JsonPropertyName("responseTime")]
    public int? ResponseTime { get; init; }

    /// <summary>
    /// Response body (truncated to 1KB).
    /// </summary>
    [JsonPropertyName("responseBody")]
    public string? ResponseBody { get; init; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; init; }

    /// <summary>
    /// The test payload that was sent.
    /// </summary>
    [JsonPropertyName("payloadSent")]
    public object? PayloadSent { get; init; }
}

/// <summary>
/// Response from rotating a webhook secret.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record RotateSecretResponse
{
    /// <summary>
    /// Webhook ID.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// New signing secret.
    /// </summary>
    [JsonPropertyName("secret")]
    public required string Secret { get; init; }

    /// <summary>
    /// ISO timestamp - old secret valid until this time (1 hour grace period).
    /// </summary>
    [JsonPropertyName("previousSecretValidUntil")]
    public required DateTimeOffset PreviousSecretValidUntil { get; init; }
}

/// <summary>
/// Webhook delivery statistics.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record WebhookStatsResponse
{
    /// <summary>
    /// Total delivery attempts.
    /// </summary>
    [JsonPropertyName("totalDeliveries")]
    public required int TotalDeliveries { get; init; }

    /// <summary>
    /// Successful deliveries.
    /// </summary>
    [JsonPropertyName("successfulDeliveries")]
    public required int SuccessfulDeliveries { get; init; }

    /// <summary>
    /// Failed deliveries.
    /// </summary>
    [JsonPropertyName("failedDeliveries")]
    public required int FailedDeliveries { get; init; }
}
