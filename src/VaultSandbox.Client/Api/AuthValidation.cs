namespace VaultSandbox.Client.Api;

/// <summary>
/// Summary of email authentication validation results.
/// </summary>
public sealed record AuthValidation
{
    /// <summary>
    /// True if all core authentication checks (SPF, DKIM, DMARC) passed.
    /// </summary>
    public required bool Passed { get; init; }

    /// <summary>
    /// True if SPF check passed.
    /// </summary>
    public required bool SpfPassed { get; init; }

    /// <summary>
    /// True if at least one DKIM signature passed verification.
    /// </summary>
    public required bool DkimPassed { get; init; }

    /// <summary>
    /// True if DMARC check passed.
    /// </summary>
    public required bool DmarcPassed { get; init; }

    /// <summary>
    /// True if reverse DNS check passed.
    /// </summary>
    public required bool ReverseDnsPassed { get; init; }

    /// <summary>
    /// Human-readable descriptions of any failures.
    /// Empty if all checks passed.
    /// </summary>
    public required IReadOnlyList<string> Failures { get; init; }
}
