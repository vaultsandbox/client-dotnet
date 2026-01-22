using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using VaultSandbox.Client.Api;

namespace VaultSandbox.Client.Http.Models;

/// <summary>
/// Request to set chaos configuration for an inbox.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record ChaosConfigRequest
{
    /// <summary>
    /// Master switch for chaos on this inbox.
    /// </summary>
    [JsonPropertyName("enabled")]
    public required bool Enabled { get; init; }

    /// <summary>
    /// Optional auto-disable timestamp (ISO 8601).
    /// </summary>
    [JsonPropertyName("expiresAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Latency injection settings.
    /// </summary>
    [JsonPropertyName("latency")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LatencyConfigRequest? Latency { get; init; }

    /// <summary>
    /// Connection drop settings.
    /// </summary>
    [JsonPropertyName("connectionDrop")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ConnectionDropConfigRequest? ConnectionDrop { get; init; }

    /// <summary>
    /// Random error settings.
    /// </summary>
    [JsonPropertyName("randomError")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RandomErrorConfigRequest? RandomError { get; init; }

    /// <summary>
    /// Greylisting settings.
    /// </summary>
    [JsonPropertyName("greylist")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GreylistConfigRequest? Greylist { get; init; }

    /// <summary>
    /// Blackhole mode settings.
    /// </summary>
    [JsonPropertyName("blackhole")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BlackholeConfigRequest? Blackhole { get; init; }
}

/// <summary>
/// Latency injection configuration request.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record LatencyConfigRequest
{
    /// <summary>
    /// Enable latency injection.
    /// </summary>
    [JsonPropertyName("enabled")]
    public required bool Enabled { get; init; }

    /// <summary>
    /// Minimum delay in milliseconds.
    /// </summary>
    [JsonPropertyName("minDelayMs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MinDelayMs { get; init; }

    /// <summary>
    /// Maximum delay in milliseconds.
    /// </summary>
    [JsonPropertyName("maxDelayMs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxDelayMs { get; init; }

    /// <summary>
    /// Randomize delay within range (false = fixed at maxDelayMs).
    /// </summary>
    [JsonPropertyName("jitter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Jitter { get; init; }

    /// <summary>
    /// Probability of applying delay (0.0-1.0).
    /// </summary>
    [JsonPropertyName("probability")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Probability { get; init; }
}

/// <summary>
/// Connection drop configuration request.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record ConnectionDropConfigRequest
{
    /// <summary>
    /// Enable connection dropping.
    /// </summary>
    [JsonPropertyName("enabled")]
    public required bool Enabled { get; init; }

    /// <summary>
    /// Probability of dropping (0.0-1.0).
    /// </summary>
    [JsonPropertyName("probability")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Probability { get; init; }

    /// <summary>
    /// Use graceful close (FIN) vs abrupt (RST).
    /// </summary>
    [JsonPropertyName("graceful")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Graceful { get; init; }
}

/// <summary>
/// Random error configuration request.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record RandomErrorConfigRequest
{
    /// <summary>
    /// Enable random error generation.
    /// </summary>
    [JsonPropertyName("enabled")]
    public required bool Enabled { get; init; }

    /// <summary>
    /// Probability of returning an error (0.0-1.0).
    /// </summary>
    [JsonPropertyName("errorRate")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? ErrorRate { get; init; }

    /// <summary>
    /// Types of errors to return.
    /// </summary>
    [JsonPropertyName("errorTypes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RandomErrorType[]? ErrorTypes { get; init; }
}

/// <summary>
/// Greylisting configuration request.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record GreylistConfigRequest
{
    /// <summary>
    /// Enable greylisting simulation.
    /// </summary>
    [JsonPropertyName("enabled")]
    public required bool Enabled { get; init; }

    /// <summary>
    /// Window for tracking retry attempts (milliseconds).
    /// </summary>
    [JsonPropertyName("retryWindowMs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? RetryWindowMs { get; init; }

    /// <summary>
    /// Number of attempts before accepting.
    /// </summary>
    [JsonPropertyName("maxAttempts")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxAttempts { get; init; }

    /// <summary>
    /// How to identify unique senders.
    /// </summary>
    [JsonPropertyName("trackBy")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GreylistTrackBy? TrackBy { get; init; }
}

/// <summary>
/// Blackhole configuration request.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record BlackholeConfigRequest
{
    /// <summary>
    /// Enable blackhole mode.
    /// </summary>
    [JsonPropertyName("enabled")]
    public required bool Enabled { get; init; }

    /// <summary>
    /// Whether to still trigger webhooks.
    /// </summary>
    [JsonPropertyName("triggerWebhooks")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? TriggerWebhooks { get; init; }
}

/// <summary>
/// Response containing chaos configuration for an inbox.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record ChaosConfigResponse
{
    /// <summary>
    /// Master switch for chaos on this inbox.
    /// </summary>
    [JsonPropertyName("enabled")]
    public required bool Enabled { get; init; }

    /// <summary>
    /// Auto-disable timestamp (ISO 8601).
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Latency injection settings.
    /// </summary>
    [JsonPropertyName("latency")]
    public LatencyConfigResponse? Latency { get; init; }

    /// <summary>
    /// Connection drop settings.
    /// </summary>
    [JsonPropertyName("connectionDrop")]
    public ConnectionDropConfigResponse? ConnectionDrop { get; init; }

    /// <summary>
    /// Random error settings.
    /// </summary>
    [JsonPropertyName("randomError")]
    public RandomErrorConfigResponse? RandomError { get; init; }

    /// <summary>
    /// Greylisting settings.
    /// </summary>
    [JsonPropertyName("greylist")]
    public GreylistConfigResponse? Greylist { get; init; }

    /// <summary>
    /// Blackhole mode settings.
    /// </summary>
    [JsonPropertyName("blackhole")]
    public BlackholeConfigResponse? Blackhole { get; init; }
}

/// <summary>
/// Latency injection configuration response.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record LatencyConfigResponse
{
    /// <summary>
    /// Whether latency injection is enabled.
    /// </summary>
    [JsonPropertyName("enabled")]
    public required bool Enabled { get; init; }

    /// <summary>
    /// Minimum delay in milliseconds.
    /// </summary>
    [JsonPropertyName("minDelayMs")]
    public required int MinDelayMs { get; init; }

    /// <summary>
    /// Maximum delay in milliseconds.
    /// </summary>
    [JsonPropertyName("maxDelayMs")]
    public required int MaxDelayMs { get; init; }

    /// <summary>
    /// Randomize delay within range.
    /// </summary>
    [JsonPropertyName("jitter")]
    public required bool Jitter { get; init; }

    /// <summary>
    /// Probability of applying delay (0.0-1.0).
    /// </summary>
    [JsonPropertyName("probability")]
    public required double Probability { get; init; }
}

/// <summary>
/// Connection drop configuration response.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record ConnectionDropConfigResponse
{
    /// <summary>
    /// Whether connection dropping is enabled.
    /// </summary>
    [JsonPropertyName("enabled")]
    public required bool Enabled { get; init; }

    /// <summary>
    /// Probability of dropping (0.0-1.0).
    /// </summary>
    [JsonPropertyName("probability")]
    public required double Probability { get; init; }

    /// <summary>
    /// Use graceful close (FIN) vs abrupt (RST).
    /// </summary>
    [JsonPropertyName("graceful")]
    public required bool Graceful { get; init; }
}

/// <summary>
/// Random error configuration response.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record RandomErrorConfigResponse
{
    /// <summary>
    /// Whether random error generation is enabled.
    /// </summary>
    [JsonPropertyName("enabled")]
    public required bool Enabled { get; init; }

    /// <summary>
    /// Probability of returning an error (0.0-1.0).
    /// </summary>
    [JsonPropertyName("errorRate")]
    public required double ErrorRate { get; init; }

    /// <summary>
    /// Types of errors to return.
    /// </summary>
    [JsonPropertyName("errorTypes")]
    public required RandomErrorType[] ErrorTypes { get; init; }
}

/// <summary>
/// Greylisting configuration response.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record GreylistConfigResponse
{
    /// <summary>
    /// Whether greylisting is enabled.
    /// </summary>
    [JsonPropertyName("enabled")]
    public required bool Enabled { get; init; }

    /// <summary>
    /// Window for tracking retry attempts (milliseconds).
    /// </summary>
    [JsonPropertyName("retryWindowMs")]
    public required int RetryWindowMs { get; init; }

    /// <summary>
    /// Number of attempts before accepting.
    /// </summary>
    [JsonPropertyName("maxAttempts")]
    public required int MaxAttempts { get; init; }

    /// <summary>
    /// How to identify unique senders.
    /// </summary>
    [JsonPropertyName("trackBy")]
    public required GreylistTrackBy TrackBy { get; init; }
}

/// <summary>
/// Blackhole configuration response.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record BlackholeConfigResponse
{
    /// <summary>
    /// Whether blackhole mode is enabled.
    /// </summary>
    [JsonPropertyName("enabled")]
    public required bool Enabled { get; init; }

    /// <summary>
    /// Whether webhooks are still triggered.
    /// </summary>
    [JsonPropertyName("triggerWebhooks")]
    public required bool TriggerWebhooks { get; init; }
}
