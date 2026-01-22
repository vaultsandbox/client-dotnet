using System.Diagnostics.CodeAnalysis;
using VaultSandbox.Client.Http.Models;

namespace VaultSandbox.Client.Api;

/// <summary>
/// Options for setting chaos configuration on an inbox.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SetChaosConfigOptions
{
    /// <summary>
    /// Master switch for chaos on this inbox.
    /// </summary>
    public required bool Enabled { get; set; }

    /// <summary>
    /// Optional auto-disable timestamp. Chaos will be automatically disabled after this time.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// Latency injection configuration.
    /// </summary>
    public LatencyOptions? Latency { get; set; }

    /// <summary>
    /// Connection drop configuration.
    /// </summary>
    public ConnectionDropOptions? ConnectionDrop { get; set; }

    /// <summary>
    /// Random error configuration.
    /// </summary>
    public RandomErrorOptions? RandomError { get; set; }

    /// <summary>
    /// Greylisting configuration.
    /// </summary>
    public GreylistOptions? Greylist { get; set; }

    /// <summary>
    /// Blackhole mode configuration.
    /// </summary>
    public BlackholeOptions? Blackhole { get; set; }

    internal ChaosConfigRequest ToRequest()
    {
        return new ChaosConfigRequest
        {
            Enabled = Enabled,
            ExpiresAt = ExpiresAt,
            Latency = Latency?.ToRequest(),
            ConnectionDrop = ConnectionDrop?.ToRequest(),
            RandomError = RandomError?.ToRequest(),
            Greylist = Greylist?.ToRequest(),
            Blackhole = Blackhole?.ToRequest()
        };
    }
}

/// <summary>
/// Options for latency injection chaos.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class LatencyOptions
{
    /// <summary>
    /// Enable latency injection.
    /// </summary>
    public required bool Enabled { get; set; }

    /// <summary>
    /// Minimum delay in milliseconds. Default: 500.
    /// </summary>
    public int? MinDelayMs { get; set; }

    /// <summary>
    /// Maximum delay in milliseconds. Default: 10000. Max: 60000.
    /// </summary>
    public int? MaxDelayMs { get; set; }

    /// <summary>
    /// Randomize delay within range. When false, uses maxDelayMs as fixed delay. Default: true.
    /// </summary>
    public bool? Jitter { get; set; }

    /// <summary>
    /// Probability of applying delay (0.0-1.0). Default: 1.0.
    /// </summary>
    public double? Probability { get; set; }

    internal LatencyConfigRequest ToRequest()
    {
        return new LatencyConfigRequest
        {
            Enabled = Enabled,
            MinDelayMs = MinDelayMs,
            MaxDelayMs = MaxDelayMs,
            Jitter = Jitter,
            Probability = Probability
        };
    }
}

/// <summary>
/// Options for connection drop chaos.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ConnectionDropOptions
{
    /// <summary>
    /// Enable connection dropping.
    /// </summary>
    public required bool Enabled { get; set; }

    /// <summary>
    /// Probability of dropping the connection (0.0-1.0). Default: 1.0.
    /// </summary>
    public double? Probability { get; set; }

    /// <summary>
    /// Use graceful close (FIN) vs abrupt (RST). Default: true.
    /// </summary>
    public bool? Graceful { get; set; }

    internal ConnectionDropConfigRequest ToRequest()
    {
        return new ConnectionDropConfigRequest
        {
            Enabled = Enabled,
            Probability = Probability,
            Graceful = Graceful
        };
    }
}

/// <summary>
/// Options for random error chaos.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class RandomErrorOptions
{
    /// <summary>
    /// Enable random error generation.
    /// </summary>
    public required bool Enabled { get; set; }

    /// <summary>
    /// Probability of returning an error (0.0-1.0). Default: 0.1.
    /// </summary>
    public double? ErrorRate { get; set; }

    /// <summary>
    /// Types of errors to return. Default: [Temporary].
    /// </summary>
    public IList<RandomErrorType>? ErrorTypes { get; set; }

    internal RandomErrorConfigRequest ToRequest()
    {
        return new RandomErrorConfigRequest
        {
            Enabled = Enabled,
            ErrorRate = ErrorRate,
            ErrorTypes = ErrorTypes?.ToArray()
        };
    }
}

/// <summary>
/// Options for greylisting chaos.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GreylistOptions
{
    /// <summary>
    /// Enable greylisting simulation.
    /// </summary>
    public required bool Enabled { get; set; }

    /// <summary>
    /// Window for tracking retry attempts in milliseconds. Default: 300000 (5 minutes).
    /// </summary>
    public int? RetryWindowMs { get; set; }

    /// <summary>
    /// Number of attempts before accepting. Default: 2.
    /// </summary>
    public int? MaxAttempts { get; set; }

    /// <summary>
    /// How to identify unique senders. Default: IpSender.
    /// </summary>
    public GreylistTrackBy? TrackBy { get; set; }

    internal GreylistConfigRequest ToRequest()
    {
        return new GreylistConfigRequest
        {
            Enabled = Enabled,
            RetryWindowMs = RetryWindowMs,
            MaxAttempts = MaxAttempts,
            TrackBy = TrackBy
        };
    }
}

/// <summary>
/// Options for blackhole chaos.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class BlackholeOptions
{
    /// <summary>
    /// Enable blackhole mode. Accepts emails but silently discards them.
    /// </summary>
    public required bool Enabled { get; set; }

    /// <summary>
    /// Whether to still trigger webhooks when blackholing. Default: false.
    /// </summary>
    public bool? TriggerWebhooks { get; set; }

    internal BlackholeConfigRequest ToRequest()
    {
        return new BlackholeConfigRequest
        {
            Enabled = Enabled,
            TriggerWebhooks = TriggerWebhooks
        };
    }
}
