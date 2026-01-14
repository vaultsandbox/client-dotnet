using System.Text.Json.Serialization;

namespace VaultSandbox.Client.Api;

/// <summary>
/// SPF (Sender Policy Framework) verification result.
/// </summary>
public sealed record SpfResult
{
    [JsonPropertyName("result")]
    [JsonConverter(typeof(JsonStringEnumConverter<SpfStatus>))]
    public SpfStatus Result { get; init; } = SpfStatus.None;

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
    PermError,
    Skipped
}

/// <summary>
/// DKIM (DomainKeys Identified Mail) verification result.
/// </summary>
public sealed record DkimResult
{
    [JsonPropertyName("result")]
    [JsonConverter(typeof(JsonStringEnumConverter<DkimStatus>))]
    public DkimStatus Result { get; init; } = DkimStatus.None;

    public string? Domain { get; init; }
    public string? Selector { get; init; }
    public string? Signature { get; init; }
}

[JsonConverter(typeof(JsonStringEnumConverter<DkimStatus>))]
public enum DkimStatus
{
    Pass,
    Fail,
    None,
    Skipped
}

/// <summary>
/// DMARC (Domain-based Message Authentication) result.
/// </summary>
public sealed record DmarcResult
{
    [JsonPropertyName("result")]
    [JsonConverter(typeof(JsonStringEnumConverter<DmarcStatus>))]
    public DmarcStatus Result { get; init; } = DmarcStatus.None;

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
    None,
    Skipped
}

[JsonConverter(typeof(JsonStringEnumConverter<DmarcPolicy>))]
public enum DmarcPolicy
{
    None,
    Quarantine,
    Reject
}

[JsonConverter(typeof(JsonStringEnumConverter<ReverseDnsStatus>))]
public enum ReverseDnsStatus
{
    Pass,
    Fail,
    None,
    Skipped
}

/// <summary>
/// Reverse DNS verification result.
/// </summary>
public sealed record ReverseDnsResult
{
    [JsonPropertyName("result")]
    [JsonConverter(typeof(JsonStringEnumConverter<ReverseDnsStatus>))]
    public ReverseDnsStatus Result { get; init; } = ReverseDnsStatus.None;

    public string? Ip { get; init; }
    public string? Hostname { get; init; }
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

        // Check SPF (skipped is treated as informational, not a failure)
        var spfPassed = Spf?.Result == SpfStatus.Pass;
        var spfSkipped = Spf?.Result == SpfStatus.Skipped;
        if (Spf is not null && !spfPassed && !spfSkipped)
        {
            var domain = Spf.Domain is not null ? $" (domain: {Spf.Domain})" : "";
            failures.Add($"SPF check failed: {Spf.Result}{domain}");
        }

        // Check DKIM (at least one signature must pass, skipped is informational)
        var dkimPassed = Dkim?.Any(d => d.Result == DkimStatus.Pass) ?? false;
        var dkimAllSkipped = Dkim?.All(d => d.Result == DkimStatus.Skipped) ?? false;
        if (Dkim is { Count: > 0 } && !dkimPassed && !dkimAllSkipped)
        {
            var failedDomains = string.Join(", ",
                Dkim.Where(d => d.Result != DkimStatus.Pass && d.Result != DkimStatus.Skipped && d.Domain is not null)
                    .Select(d => d.Domain));
            var domainInfo = string.IsNullOrEmpty(failedDomains) ? "" : $": {failedDomains}";
            failures.Add($"DKIM signature failed{domainInfo}");
        }

        // Check DMARC (skipped is informational)
        var dmarcPassed = Dmarc?.Result == DmarcStatus.Pass;
        var dmarcSkipped = Dmarc?.Result == DmarcStatus.Skipped;
        if (Dmarc is not null && !dmarcPassed && !dmarcSkipped)
        {
            var policy = Dmarc.Policy is not null ? $" (policy: {Dmarc.Policy})" : "";
            failures.Add($"DMARC policy: {Dmarc.Result}{policy}");
        }

        // Check Reverse DNS (skipped is informational)
        var reverseDnsPassed = ReverseDns?.Result == ReverseDnsStatus.Pass;
        var reverseDnsSkipped = ReverseDns?.Result == ReverseDnsStatus.Skipped;
        if (ReverseDns is not null && !reverseDnsPassed && !reverseDnsSkipped)
        {
            var hostname = ReverseDns.Hostname is not null
                ? $" (hostname: {ReverseDns.Hostname})"
                : "";
            failures.Add($"Reverse DNS check failed{hostname}");
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
