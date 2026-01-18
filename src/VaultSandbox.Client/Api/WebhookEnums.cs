using System.Text.Json.Serialization;

namespace VaultSandbox.Client.Api;

/// <summary>
/// Event types that webhooks can subscribe to.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<WebhookEventType>))]
public enum WebhookEventType
{
    /// <summary>
    /// Fired when an email arrives at an inbox.
    /// </summary>
    [JsonStringEnumMemberName("email.received")]
    EmailReceived,

    /// <summary>
    /// Fired when an email is persisted to storage.
    /// </summary>
    [JsonStringEnumMemberName("email.stored")]
    EmailStored,

    /// <summary>
    /// Fired when an email is deleted.
    /// </summary>
    [JsonStringEnumMemberName("email.deleted")]
    EmailDeleted
}

/// <summary>
/// Fields that can be used in webhook filters.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<FilterableField>))]
public enum FilterableField
{
    /// <summary>
    /// Email subject line.
    /// </summary>
    [JsonStringEnumMemberName("subject")]
    Subject,

    /// <summary>
    /// Sender email address.
    /// </summary>
    [JsonStringEnumMemberName("from.address")]
    FromAddress,

    /// <summary>
    /// Sender display name.
    /// </summary>
    [JsonStringEnumMemberName("from.name")]
    FromName,

    /// <summary>
    /// First recipient email address.
    /// </summary>
    [JsonStringEnumMemberName("to.address")]
    ToAddress,

    /// <summary>
    /// First recipient display name.
    /// </summary>
    [JsonStringEnumMemberName("to.name")]
    ToName,

    /// <summary>
    /// Plain text body (first 5KB).
    /// </summary>
    [JsonStringEnumMemberName("body.text")]
    BodyText,

    /// <summary>
    /// HTML body (first 5KB).
    /// </summary>
    [JsonStringEnumMemberName("body.html")]
    BodyHtml
}

/// <summary>
/// Operators for webhook filter rules.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<FilterOperator>))]
public enum FilterOperator
{
    /// <summary>
    /// Exact match.
    /// </summary>
    [JsonStringEnumMemberName("equals")]
    Equals,

    /// <summary>
    /// Substring match.
    /// </summary>
    [JsonStringEnumMemberName("contains")]
    Contains,

    /// <summary>
    /// Prefix match.
    /// </summary>
    [JsonStringEnumMemberName("starts_with")]
    StartsWith,

    /// <summary>
    /// Suffix match.
    /// </summary>
    [JsonStringEnumMemberName("ends_with")]
    EndsWith,

    /// <summary>
    /// Email domain match (supports subdomains).
    /// </summary>
    [JsonStringEnumMemberName("domain")]
    Domain,

    /// <summary>
    /// Regular expression match.
    /// </summary>
    [JsonStringEnumMemberName("regex")]
    Regex,

    /// <summary>
    /// Field presence check.
    /// </summary>
    [JsonStringEnumMemberName("exists")]
    Exists
}

/// <summary>
/// Mode for combining filter rules.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<FilterMode>))]
public enum FilterMode
{
    /// <summary>
    /// All rules must match (AND logic).
    /// </summary>
    [JsonStringEnumMemberName("all")]
    All,

    /// <summary>
    /// At least one rule must match (OR logic).
    /// </summary>
    [JsonStringEnumMemberName("any")]
    Any
}

/// <summary>
/// Scope of a webhook.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<WebhookScope>))]
public enum WebhookScope
{
    /// <summary>
    /// Receives events for all inboxes.
    /// </summary>
    [JsonStringEnumMemberName("global")]
    Global,

    /// <summary>
    /// Receives events for a specific inbox only.
    /// </summary>
    [JsonStringEnumMemberName("inbox")]
    Inbox
}

/// <summary>
/// Status of a webhook delivery.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<WebhookDeliveryStatus>))]
public enum WebhookDeliveryStatus
{
    /// <summary>
    /// Delivery was successful.
    /// </summary>
    [JsonStringEnumMemberName("success")]
    Success,

    /// <summary>
    /// Delivery failed.
    /// </summary>
    [JsonStringEnumMemberName("failed")]
    Failed
}

/// <summary>
/// Reason for email deletion (used in email.deleted events).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<EmailDeleteReason>))]
public enum EmailDeleteReason
{
    /// <summary>
    /// Manually deleted by user.
    /// </summary>
    [JsonStringEnumMemberName("manual")]
    Manual,

    /// <summary>
    /// Deleted due to TTL expiration.
    /// </summary>
    [JsonStringEnumMemberName("ttl")]
    Ttl,

    /// <summary>
    /// Deleted due to storage eviction.
    /// </summary>
    [JsonStringEnumMemberName("eviction")]
    Eviction
}
