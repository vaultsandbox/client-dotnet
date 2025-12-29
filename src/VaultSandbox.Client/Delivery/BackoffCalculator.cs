namespace VaultSandbox.Client.Delivery;

/// <summary>
/// Provides exponential backoff calculation with jitter for retry scenarios.
/// </summary>
internal static class BackoffCalculator
{
    private const double DefaultJitterFactor = 0.3;

    /// <summary>
    /// Calculates delay with exponential backoff and jitter.
    /// </summary>
    /// <param name="baseDelayMs">Base delay in milliseconds.</param>
    /// <param name="attempt">Current attempt number (1-based).</param>
    /// <param name="maxMultiplier">Maximum multiplier for the base delay (default: 10).</param>
    /// <param name="jitterFactor">Jitter factor as a fraction of delay (default: 0.3 = 30%).</param>
    /// <returns>Calculated delay in milliseconds with jitter applied.</returns>
    public static int Calculate(
        int baseDelayMs,
        int attempt,
        int maxMultiplier = 10,
        double jitterFactor = DefaultJitterFactor)
    {
        var exponentialDelay = baseDelayMs * (int)Math.Pow(2, Math.Max(0, attempt - 1));
        var cappedDelay = Math.Min(exponentialDelay, baseDelayMs * maxMultiplier);
        var jitter = Random.Shared.NextDouble() * jitterFactor * cappedDelay;
        return (int)(cappedDelay + jitter);
    }

    /// <summary>
    /// Calculates delay with linear backoff and jitter.
    /// </summary>
    /// <param name="currentDelayMs">Current delay in milliseconds.</param>
    /// <param name="multiplier">Multiplier to apply (default: 1.5).</param>
    /// <param name="maxDelayMs">Maximum delay in milliseconds.</param>
    /// <param name="jitterFactor">Jitter factor as a fraction of delay (default: 0.3 = 30%).</param>
    /// <returns>Calculated delay in milliseconds with jitter applied.</returns>
    public static int CalculateLinear(
        int currentDelayMs,
        double multiplier,
        int maxDelayMs,
        double jitterFactor = DefaultJitterFactor)
    {
        var newDelay = Math.Min((int)(currentDelayMs * multiplier), maxDelayMs);
        var jitter = Random.Shared.NextDouble() * jitterFactor * newDelay;
        return (int)(newDelay + jitter);
    }

    /// <summary>
    /// Adds jitter to a delay value.
    /// </summary>
    /// <param name="delayMs">Base delay in milliseconds.</param>
    /// <param name="jitterFactor">Jitter factor as a fraction of delay (default: 0.3 = 30%).</param>
    /// <returns>Delay with jitter applied.</returns>
    public static int AddJitter(int delayMs, double jitterFactor = DefaultJitterFactor)
    {
        var jitter = Random.Shared.NextDouble() * jitterFactor * delayMs;
        return (int)(delayMs + jitter);
    }
}
