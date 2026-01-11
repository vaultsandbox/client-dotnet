using System.Diagnostics.CodeAnalysis;

namespace VaultSandbox.Client.Api;

/// <summary>
/// Options for creating a new inbox.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateInboxOptions
{
    /// <summary>
    /// Desired email address or domain.
    /// If not specified, a random address is generated.
    /// </summary>
    public string? EmailAddress { get; set; }

    /// <summary>
    /// Time-to-live for the inbox.
    /// Default: 1 hour. Min: 60 seconds. Max: 7 days.
    /// </summary>
    public TimeSpan? Ttl { get; set; }
}
