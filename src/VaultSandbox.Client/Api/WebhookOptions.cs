using System.Diagnostics.CodeAnalysis;
using VaultSandbox.Client.Http.Models;

namespace VaultSandbox.Client.Api;

/// <summary>
/// Options for creating a webhook.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateWebhookOptions
{
    /// <summary>
    /// Target URL for webhook delivery. HTTPS is required in production.
    /// </summary>
    public required string Url { get; set; }

    /// <summary>
    /// Event types to subscribe to. Maximum 10 events.
    /// </summary>
    public required IList<WebhookEventType> Events { get; set; }

    /// <summary>
    /// Optional payload template. Can be a built-in template name
    /// ('slack', 'discord', 'teams', 'simple', 'notification', 'zapier', 'default')
    /// or a custom template configuration.
    /// </summary>
    public WebhookTemplateConfig? Template { get; set; }

    /// <summary>
    /// Optional event filter configuration.
    /// </summary>
    public WebhookFilterConfig? Filter { get; set; }

    /// <summary>
    /// Optional description (max 500 chars).
    /// </summary>
    public string? Description { get; set; }

    internal CreateWebhookRequest ToRequest()
    {
        return new CreateWebhookRequest
        {
            Url = Url,
            Events = Events.ToArray(),
            Template = Template?.ToRequestValue(),
            Filter = Filter?.ToRequest(),
            Description = Description
        };
    }
}

/// <summary>
/// Options for updating a webhook.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateWebhookOptions
{
    /// <summary>
    /// Target URL for webhook delivery. HTTPS is required in production.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Event types to subscribe to. Maximum 10 events.
    /// </summary>
    public IList<WebhookEventType>? Events { get; set; }

    /// <summary>
    /// Payload template configuration. Set to null to remove template.
    /// </summary>
    public WebhookTemplateConfig? Template { get; set; }

    /// <summary>
    /// Whether the Template property has been explicitly set (to allow setting to null).
    /// </summary>
    public bool TemplateWasSet { get; set; }

    /// <summary>
    /// Event filter configuration. Set to null to remove filter.
    /// </summary>
    public WebhookFilterConfig? Filter { get; set; }

    /// <summary>
    /// Whether the Filter property has been explicitly set (to allow setting to null).
    /// </summary>
    public bool FilterWasSet { get; set; }

    /// <summary>
    /// Optional description (max 500 chars).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether the webhook is active.
    /// </summary>
    public bool? Enabled { get; set; }

    internal UpdateWebhookRequest ToRequest()
    {
        return new UpdateWebhookRequest
        {
            Url = Url,
            Events = Events?.ToArray(),
            Template = TemplateWasSet ? Template?.ToRequestValue() : null,
            TemplateWasSet = TemplateWasSet,
            Filter = FilterWasSet ? Filter?.ToRequest() : null,
            FilterWasSet = FilterWasSet,
            Description = Description,
            Enabled = Enabled
        };
    }

    /// <summary>
    /// Removes the template from the webhook.
    /// </summary>
    public void RemoveTemplate()
    {
        Template = null;
        TemplateWasSet = true;
    }

    /// <summary>
    /// Removes the filter from the webhook.
    /// </summary>
    public void RemoveFilter()
    {
        Filter = null;
        FilterWasSet = true;
    }
}

/// <summary>
/// Configuration for webhook templates.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class WebhookTemplateConfig
{
    internal abstract object ToRequestValue();

    /// <summary>
    /// Creates a built-in template configuration.
    /// </summary>
    /// <param name="name">Template name ('slack', 'discord', 'teams', 'simple', 'notification', 'zapier', 'default').</param>
    public static WebhookTemplateConfig BuiltIn(string name) => new BuiltInTemplate(name);

    /// <summary>
    /// Creates a custom template configuration.
    /// </summary>
    /// <param name="body">JSON template with {{variable}} placeholders (max 10000 chars).</param>
    /// <param name="contentType">Optional Content-Type header override.</param>
    public static WebhookTemplateConfig Custom(string body, string? contentType = null) =>
        new CustomTemplateConfig(body, contentType);
}

[ExcludeFromCodeCoverage]
internal sealed class BuiltInTemplate : WebhookTemplateConfig
{
    private readonly string _name;

    public BuiltInTemplate(string name)
    {
        _name = name;
    }

    internal override object ToRequestValue() => _name;
}

[ExcludeFromCodeCoverage]
internal sealed class CustomTemplateConfig : WebhookTemplateConfig
{
    private readonly string _body;
    private readonly string? _contentType;

    public CustomTemplateConfig(string body, string? contentType)
    {
        _body = body;
        _contentType = contentType;
    }

    internal override object ToRequestValue() => new CustomTemplateRequest
    {
        Body = _body,
        ContentType = _contentType
    };
}

/// <summary>
/// Configuration for webhook filters.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class WebhookFilterConfig
{
    /// <summary>
    /// Filter rules (max 10).
    /// </summary>
    public IList<WebhookFilterRuleConfig> Rules { get; set; } = new List<WebhookFilterRuleConfig>();

    /// <summary>
    /// Filter mode ('all' = AND logic, 'any' = OR logic).
    /// </summary>
    public FilterMode Mode { get; set; } = FilterMode.All;

    /// <summary>
    /// Require email to pass SPF/DKIM/DMARC checks.
    /// </summary>
    public bool? RequireAuth { get; set; }

    internal FilterConfigRequest ToRequest()
    {
        return new FilterConfigRequest
        {
            Rules = Rules.Select(r => r.ToRequest()).ToArray(),
            Mode = Mode,
            RequireAuth = RequireAuth
        };
    }
}

/// <summary>
/// Configuration for a single filter rule.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class WebhookFilterRuleConfig
{
    /// <summary>
    /// Field to filter on.
    /// </summary>
    public FilterableField Field { get; set; }

    /// <summary>
    /// Comparison operator.
    /// </summary>
    public FilterOperator Operator { get; set; }

    /// <summary>
    /// Value to match (max 1000 chars).
    /// </summary>
    public required string Value { get; set; }

    /// <summary>
    /// Case-sensitive matching (default: false).
    /// </summary>
    public bool? CaseSensitive { get; set; }

    internal FilterRuleRequest ToRequest()
    {
        return new FilterRuleRequest
        {
            Field = Field,
            Operator = Operator,
            Value = Value,
            CaseSensitive = CaseSensitive
        };
    }
}
