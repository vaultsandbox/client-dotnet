using System.Diagnostics.CodeAnalysis;
using VaultSandbox.Client.Http.Models;

namespace VaultSandbox.Client.Api;

/// <summary>
/// Represents the chaos configuration for an inbox.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ChaosConfig
{
    /// <summary>
    /// Master switch for chaos on this inbox.
    /// </summary>
    public bool Enabled { get; }

    /// <summary>
    /// Auto-disable timestamp. Chaos will be automatically disabled after this time.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; }

    /// <summary>
    /// Latency injection configuration.
    /// </summary>
    public LatencyConfig? Latency { get; }

    /// <summary>
    /// Connection drop configuration.
    /// </summary>
    public ConnectionDropConfig? ConnectionDrop { get; }

    /// <summary>
    /// Random error configuration.
    /// </summary>
    public RandomErrorConfig? RandomError { get; }

    /// <summary>
    /// Greylisting configuration.
    /// </summary>
    public GreylistConfig? Greylist { get; }

    /// <summary>
    /// Blackhole mode configuration.
    /// </summary>
    public BlackholeConfig? Blackhole { get; }

    internal ChaosConfig(ChaosConfigResponse response)
    {
        Enabled = response.Enabled;
        ExpiresAt = response.ExpiresAt;

        if (response.Latency is not null)
            Latency = new LatencyConfig(response.Latency);

        if (response.ConnectionDrop is not null)
            ConnectionDrop = new ConnectionDropConfig(response.ConnectionDrop);

        if (response.RandomError is not null)
            RandomError = new RandomErrorConfig(response.RandomError);

        if (response.Greylist is not null)
            Greylist = new GreylistConfig(response.Greylist);

        if (response.Blackhole is not null)
            Blackhole = new BlackholeConfig(response.Blackhole);
    }
}

/// <summary>
/// Latency injection configuration.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class LatencyConfig
{
    /// <summary>
    /// Whether latency injection is enabled.
    /// </summary>
    public bool Enabled { get; }

    /// <summary>
    /// Minimum delay in milliseconds.
    /// </summary>
    public int MinDelayMs { get; }

    /// <summary>
    /// Maximum delay in milliseconds.
    /// </summary>
    public int MaxDelayMs { get; }

    /// <summary>
    /// Whether delay is randomized within range.
    /// </summary>
    public bool Jitter { get; }

    /// <summary>
    /// Probability of applying delay (0.0-1.0).
    /// </summary>
    public double Probability { get; }

    internal LatencyConfig(LatencyConfigResponse response)
    {
        Enabled = response.Enabled;
        MinDelayMs = response.MinDelayMs;
        MaxDelayMs = response.MaxDelayMs;
        Jitter = response.Jitter;
        Probability = response.Probability;
    }
}

/// <summary>
/// Connection drop configuration.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ConnectionDropConfig
{
    /// <summary>
    /// Whether connection dropping is enabled.
    /// </summary>
    public bool Enabled { get; }

    /// <summary>
    /// Probability of dropping the connection (0.0-1.0).
    /// </summary>
    public double Probability { get; }

    /// <summary>
    /// Whether to use graceful close (FIN) vs abrupt (RST).
    /// </summary>
    public bool Graceful { get; }

    internal ConnectionDropConfig(ConnectionDropConfigResponse response)
    {
        Enabled = response.Enabled;
        Probability = response.Probability;
        Graceful = response.Graceful;
    }
}

/// <summary>
/// Random error configuration.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class RandomErrorConfig
{
    /// <summary>
    /// Whether random error generation is enabled.
    /// </summary>
    public bool Enabled { get; }

    /// <summary>
    /// Probability of returning an error (0.0-1.0).
    /// </summary>
    public double ErrorRate { get; }

    /// <summary>
    /// Types of errors that can be returned.
    /// </summary>
    public IReadOnlyList<RandomErrorType> ErrorTypes { get; }

    internal RandomErrorConfig(RandomErrorConfigResponse response)
    {
        Enabled = response.Enabled;
        ErrorRate = response.ErrorRate;
        ErrorTypes = response.ErrorTypes;
    }
}

/// <summary>
/// Greylisting configuration.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GreylistConfig
{
    /// <summary>
    /// Whether greylisting is enabled.
    /// </summary>
    public bool Enabled { get; }

    /// <summary>
    /// Window for tracking retry attempts in milliseconds.
    /// </summary>
    public int RetryWindowMs { get; }

    /// <summary>
    /// Number of attempts before accepting.
    /// </summary>
    public int MaxAttempts { get; }

    /// <summary>
    /// How to identify unique senders.
    /// </summary>
    public GreylistTrackBy TrackBy { get; }

    internal GreylistConfig(GreylistConfigResponse response)
    {
        Enabled = response.Enabled;
        RetryWindowMs = response.RetryWindowMs;
        MaxAttempts = response.MaxAttempts;
        TrackBy = response.TrackBy;
    }
}

/// <summary>
/// Blackhole mode configuration.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class BlackholeConfig
{
    /// <summary>
    /// Whether blackhole mode is enabled.
    /// </summary>
    public bool Enabled { get; }

    /// <summary>
    /// Whether webhooks are still triggered when blackholing.
    /// </summary>
    public bool TriggerWebhooks { get; }

    internal BlackholeConfig(BlackholeConfigResponse response)
    {
        Enabled = response.Enabled;
        TriggerWebhooks = response.TriggerWebhooks;
    }
}
