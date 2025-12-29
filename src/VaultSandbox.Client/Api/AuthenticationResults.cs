using System.Text.Json.Serialization;

namespace VaultSandbox.Client.Api;

/// <summary>
/// SPF (Sender Policy Framework) verification result.
/// </summary>
public sealed record SpfResult
{
    [JsonPropertyName("result")]
    [JsonConverter(typeof(JsonStringEnumConverter<SpfStatus>))]
    public SpfStatus Status { get; init; } = SpfStatus.None;

    public string? Domain { get; init; }
    public string? Ip { get; init; }

    [JsonPropertyName("details")]
    public string? Details { get; init; }
}

[JsonConverter(typeof(JsonStringEnumConverter<SpfStatus>))]
public enum SpfStatus
{
    Pass,
    Fail,
    SoftFail,
    Neutral,
    None,
    TempError,
    PermError
}

/// <summary>
/// DKIM (DomainKeys Identified Mail) verification result.
/// </summary>
public sealed record DkimResult
{
    [JsonPropertyName("result")]
    [JsonConverter(typeof(JsonStringEnumConverter<DkimStatus>))]
    public DkimStatus Status { get; init; } = DkimStatus.None;

    public string? Domain { get; init; }
    public string? Selector { get; init; }
    public string? Signature { get; init; }
}

[JsonConverter(typeof(JsonStringEnumConverter<DkimStatus>))]
public enum DkimStatus
{
    Pass,
    Fail,
    None
}

/// <summary>
/// DMARC (Domain-based Message Authentication) result.
/// </summary>
public sealed record DmarcResult
{
    [JsonPropertyName("result")]
    [JsonConverter(typeof(JsonStringEnumConverter<DmarcStatus>))]
    public DmarcStatus Status { get; init; } = DmarcStatus.None;

    [JsonConverter(typeof(JsonStringEnumConverter<DmarcPolicy>))]
    public DmarcPolicy? Policy { get; init; }

    public bool? Aligned { get; init; }
    public string? Domain { get; init; }
}

[JsonConverter(typeof(JsonStringEnumConverter<DmarcStatus>))]
public enum DmarcStatus
{
    Pass,
    Fail,
    None
}

[JsonConverter(typeof(JsonStringEnumConverter<DmarcPolicy>))]
public enum DmarcPolicy
{
    None,
    Quarantine,
    Reject
}

/// <summary>
/// Reverse DNS verification result.
/// </summary>
public sealed record ReverseDnsResult
{
    public bool Verified { get; init; }
    public string? Ip { get; init; }
    public string? Hostname { get; init; }

    /// <summary>
    /// Returns the status based on the Verified property.
    /// </summary>
    [JsonIgnore]
    public ReverseDnsStatus Status => Verified ? ReverseDnsStatus.Pass : ReverseDnsStatus.Fail;
}

public enum ReverseDnsStatus
{
    Pass,
    Fail
}

/// <summary>
/// Complete email authentication results.
/// </summary>
public sealed record AuthenticationResults
{
    public SpfResult? Spf { get; init; }
    public IReadOnlyList<DkimResult>? Dkim { get; init; }
    public DmarcResult? Dmarc { get; init; }
    public ReverseDnsResult? ReverseDns { get; init; }

    /// <summary>
    /// Validates authentication results and returns a summary.
    /// </summary>
    /// <returns>Validation summary with pass/fail status and failure descriptions.</returns>
    public AuthValidation Validate()
    {
        var failures = new List<string>();

        // Check SPF
        var spfPassed = Spf?.Status == SpfStatus.Pass;
        if (Spf is not null && !spfPassed)
        {
            var domain = Spf.Domain is not null ? $" (domain: {Spf.Domain})" : "";
            failures.Add($"SPF check failed: {Spf.Status}{domain}");
        }

        // Check DKIM (at least one signature must pass)
        var dkimPassed = Dkim?.Any(d => d.Status == DkimStatus.Pass) ?? false;
        if (Dkim is { Count: > 0 } && !dkimPassed)
        {
            var failedDomains = string.Join(", ",
                Dkim.Where(d => d.Status != DkimStatus.Pass && d.Domain is not null)
                    .Select(d => d.Domain));
            var domainInfo = string.IsNullOrEmpty(failedDomains) ? "" : $": {failedDomains}";
            failures.Add($"DKIM signature failed{domainInfo}");
        }

        // Check DMARC
        var dmarcPassed = Dmarc?.Status == DmarcStatus.Pass;
        if (Dmarc is not null && !dmarcPassed)
        {
            var policy = Dmarc.Policy is not null ? $" (policy: {Dmarc.Policy})" : "";
            failures.Add($"DMARC policy: {Dmarc.Status}{policy}");
        }

        // Check Reverse DNS
        var reverseDnsPassed = ReverseDns?.Status == ReverseDnsStatus.Pass;
        if (ReverseDns is not null && !reverseDnsPassed)
        {
            var hostname = ReverseDns.Hostname is not null
                ? $" (hostname: {ReverseDns.Hostname})"
                : "";
            failures.Add($"Reverse DNS check failed: {ReverseDns.Status}{hostname}");
        }

        return new AuthValidation
        {
            Passed = spfPassed && dkimPassed && dmarcPassed,
            SpfPassed = spfPassed,
            DkimPassed = dkimPassed,
            DmarcPassed = dmarcPassed,
            ReverseDnsPassed = reverseDnsPassed,
            Failures = failures
        };
    }
}
