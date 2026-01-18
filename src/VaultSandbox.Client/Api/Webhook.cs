using System.Diagnostics.CodeAnalysis;
using VaultSandbox.Client.Http.Models;

namespace VaultSandbox.Client.Api;

/// <summary>
/// Represents a webhook subscription for an inbox.
/// </summary>
public sealed class Webhook
{
    private readonly IInbox? _inbox;

    /// <summary>
    /// Webhook ID (whk_ prefix).
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Target URL for webhook delivery.
    /// </summary>
    public string Url { get; }

    /// <summary>
    /// Subscribed event types.
    /// </summary>
    public IReadOnlyList<WebhookEventType> Events { get; }

    /// <summary>
    /// Scope of the webhook (global or inbox).
    /// </summary>
    public WebhookScope Scope { get; }

    /// <summary>
    /// Inbox email address (for inbox-scoped webhooks).
    /// </summary>
    public string? InboxEmail { get; }

    /// <summary>
    /// Inbox hash (for inbox-scoped webhooks).
    /// </summary>
    public string? InboxHash { get; }

    /// <summary>
    /// Whether the webhook is active.
    /// </summary>
    public bool Enabled { get; }

    /// <summary>
    /// Signing secret for verifying webhook deliveries (whsec_ prefix).
    /// Only available when retrieving a single webhook, not when listing.
    /// </summary>
    public string? Secret { get; }

    /// <summary>
    /// Template configuration.
    /// </summary>
    public WebhookTemplate? Template { get; }

    /// <summary>
    /// Filter configuration.
    /// </summary>
    public WebhookFilter? Filter { get; }

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// When the webhook was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// When the webhook was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; }

    /// <summary>
    /// When the last delivery attempt occurred.
    /// </summary>
    public DateTimeOffset? LastDeliveryAt { get; }

    /// <summary>
    /// Status of the last delivery.
    /// </summary>
    public WebhookDeliveryStatus? LastDeliveryStatus { get; }

    /// <summary>
    /// Delivery statistics (only available when retrieving a single webhook).
    /// </summary>
    public WebhookStats? Stats { get; }

    internal Webhook(WebhookResponse response, IInbox? inbox = null)
    {
        _inbox = inbox;
        Id = response.Id;
        Url = response.Url;
        Events = response.Events;
        Scope = response.Scope;
        InboxEmail = response.InboxEmail;
        InboxHash = response.InboxHash;
        Enabled = response.Enabled;
        Secret = response.Secret;
        Description = response.Description;
        CreatedAt = response.CreatedAt;
        UpdatedAt = response.UpdatedAt;
        LastDeliveryAt = response.LastDeliveryAt;
        LastDeliveryStatus = response.LastDeliveryStatus;

        if (response.Template is not null)
        {
            Template = WebhookTemplate.FromResponse(response.Template);
        }

        if (response.Filter is not null)
        {
            Filter = new WebhookFilter(response.Filter);
        }

        if (response.Stats is not null)
        {
            Stats = new WebhookStats(response.Stats);
        }
    }

    private void EnsureInboxAvailable()
    {
        if (_inbox is null)
        {
            throw new InvalidOperationException(
                "This webhook was not retrieved through an inbox and cannot perform inbox operations.");
        }
    }

    /// <summary>
    /// Tests this webhook by sending a test event.
    /// </summary>
    public async Task<WebhookTestResult> TestAsync(CancellationToken ct = default)
    {
        EnsureInboxAvailable();
        return await _inbox!.TestWebhookAsync(Id, ct);
    }

    /// <summary>
    /// Rotates the signing secret for this webhook.
    /// </summary>
    public async Task<WebhookSecretRotation> RotateSecretAsync(CancellationToken ct = default)
    {
        EnsureInboxAvailable();
        return await _inbox!.RotateWebhookSecretAsync(Id, ct);
    }

    /// <summary>
    /// Deletes this webhook.
    /// </summary>
    public async Task DeleteAsync(CancellationToken ct = default)
    {
        EnsureInboxAvailable();
        await _inbox!.DeleteWebhookAsync(Id, ct);
    }
}

/// <summary>
/// Webhook template configuration.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class WebhookTemplate
{
    /// <summary>
    /// Template name (e.g., 'slack', 'discord', 'teams', 'simple', 'notification', 'zapier', 'default').
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Custom template configuration (if using a custom template).
    /// </summary>
    public CustomTemplate? Custom { get; }

    private WebhookTemplate(string? name, CustomTemplate? custom)
    {
        Name = name;
        Custom = custom;
    }

    internal static WebhookTemplate? FromResponse(object? template)
    {
        if (template is null)
            return null;

        if (template is string name)
            return new WebhookTemplate(name, null);

        // Handle custom template object
        if (template is System.Text.Json.JsonElement element)
        {
            if (element.ValueKind == System.Text.Json.JsonValueKind.String)
                return new WebhookTemplate(element.GetString(), null);

            if (element.TryGetProperty("type", out var typeElement) &&
                typeElement.GetString() == "custom")
            {
                var body = element.TryGetProperty("body", out var bodyElement)
                    ? bodyElement.GetString()
                    : null;
                var contentType = element.TryGetProperty("contentType", out var ctElement)
                    ? ctElement.GetString()
                    : null;

                if (body is not null)
                {
                    return new WebhookTemplate(null, new CustomTemplate(body, contentType));
                }
            }
        }

        return null;
    }
}

/// <summary>
/// Custom webhook template configuration.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CustomTemplate
{
    /// <summary>
    /// JSON template with {{variable}} placeholders.
    /// </summary>
    public string Body { get; }

    /// <summary>
    /// Optional Content-Type header override.
    /// </summary>
    public string? ContentType { get; }

    public CustomTemplate(string body, string? contentType = null)
    {
        Body = body;
        ContentType = contentType;
    }
}

/// <summary>
/// Webhook filter configuration.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class WebhookFilter
{
    /// <summary>
    /// Filter rules.
    /// </summary>
    public IReadOnlyList<WebhookFilterRule> Rules { get; }

    /// <summary>
    /// Filter mode ('all' = AND logic, 'any' = OR logic).
    /// </summary>
    public FilterMode Mode { get; }

    /// <summary>
    /// Whether to require email authentication (SPF/DKIM/DMARC).
    /// </summary>
    public bool? RequireAuth { get; }

    internal WebhookFilter(FilterConfigResponse response)
    {
        Rules = response.Rules.Select(r => new WebhookFilterRule(r)).ToList();
        Mode = response.Mode;
        RequireAuth = response.RequireAuth;
    }
}

/// <summary>
/// A single webhook filter rule.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class WebhookFilterRule
{
    /// <summary>
    /// Field to filter on.
    /// </summary>
    public FilterableField Field { get; }

    /// <summary>
    /// Comparison operator.
    /// </summary>
    public FilterOperator Operator { get; }

    /// <summary>
    /// Value to match.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Whether matching is case-sensitive.
    /// </summary>
    public bool? CaseSensitive { get; }

    internal WebhookFilterRule(FilterRuleResponse response)
    {
        Field = response.Field;
        Operator = response.Operator;
        Value = response.Value;
        CaseSensitive = response.CaseSensitive;
    }
}

/// <summary>
/// Webhook delivery statistics.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class WebhookStats
{
    /// <summary>
    /// Total delivery attempts.
    /// </summary>
    public int TotalDeliveries { get; }

    /// <summary>
    /// Successful deliveries.
    /// </summary>
    public int SuccessfulDeliveries { get; }

    /// <summary>
    /// Failed deliveries.
    /// </summary>
    public int FailedDeliveries { get; }

    /// <summary>
    /// Success rate as a percentage (0-100).
    /// </summary>
    public double SuccessRate =>
        TotalDeliveries > 0 ? (double)SuccessfulDeliveries / TotalDeliveries * 100 : 0;

    internal WebhookStats(WebhookStatsResponse response)
    {
        TotalDeliveries = response.TotalDeliveries;
        SuccessfulDeliveries = response.SuccessfulDeliveries;
        FailedDeliveries = response.FailedDeliveries;
    }
}

/// <summary>
/// Result of testing a webhook.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class WebhookTestResult
{
    /// <summary>
    /// Whether the test succeeded.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// HTTP status code from the endpoint.
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// Response time in milliseconds.
    /// </summary>
    public int? ResponseTime { get; }

    /// <summary>
    /// Response body (truncated to 1KB).
    /// </summary>
    public string? ResponseBody { get; }

    /// <summary>
    /// Error message if the test failed.
    /// </summary>
    public string? Error { get; }

    internal WebhookTestResult(TestWebhookResponse response)
    {
        Success = response.Success;
        StatusCode = response.StatusCode;
        ResponseTime = response.ResponseTime;
        ResponseBody = response.ResponseBody;
        Error = response.Error;
    }
}

/// <summary>
/// Result of rotating a webhook secret.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class WebhookSecretRotation
{
    /// <summary>
    /// Webhook ID.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// New signing secret.
    /// </summary>
    public string Secret { get; }

    /// <summary>
    /// Time until which the previous secret remains valid (1 hour grace period).
    /// </summary>
    public DateTimeOffset PreviousSecretValidUntil { get; }

    internal WebhookSecretRotation(RotateSecretResponse response)
    {
        Id = response.Id;
        Secret = response.Secret;
        PreviousSecretValidUntil = response.PreviousSecretValidUntil;
    }
}
