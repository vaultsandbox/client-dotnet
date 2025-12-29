namespace VaultSandbox.Client.Api;

/// <summary>
/// Options for waiting for a specific email count.
/// </summary>
public sealed class WaitForEmailCountOptions
{
    /// <summary>
    /// Maximum time to wait for the target count.
    /// Default: 30 seconds.
    /// </summary>
    public TimeSpan? Timeout { get; set; }
}
