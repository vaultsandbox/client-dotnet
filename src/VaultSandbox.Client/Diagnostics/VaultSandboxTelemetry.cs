using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;

namespace VaultSandbox.Client.Diagnostics;

/// <summary>
/// OpenTelemetry instrumentation for VaultSandbox client.
/// Use <see cref="ActivitySource"/> for distributed tracing and <see cref="Meter"/> for metrics.
/// </summary>
[ExcludeFromCodeCoverage]
public static class VaultSandboxTelemetry
{
    /// <summary>
    /// The service name used for telemetry.
    /// </summary>
    public const string ServiceName = "VaultSandbox.Client";

    /// <summary>
    /// The service version used for telemetry.
    /// </summary>
    public static readonly string ServiceVersion =
        typeof(VaultSandboxTelemetry).Assembly.GetName().Version?.ToString() ?? "0.0.0";

    /// <summary>
    /// Activity source for distributed tracing.
    /// Add this source to your OpenTelemetry configuration:
    /// <code>
    /// .AddSource(VaultSandboxTelemetry.ServiceName)
    /// </code>
    /// </summary>
    public static readonly ActivitySource ActivitySource = new(ServiceName, ServiceVersion);

    /// <summary>
    /// Meter for metrics.
    /// Add this meter to your OpenTelemetry configuration:
    /// <code>
    /// .AddMeter(VaultSandboxTelemetry.ServiceName)
    /// </code>
    /// </summary>
    public static readonly Meter Meter = new(ServiceName, ServiceVersion);

    // Counters
    internal static readonly Counter<long> InboxesCreated =
        Meter.CreateCounter<long>(
            "vaultsandbox.inboxes.created",
            description: "Number of inboxes created");

    internal static readonly Counter<long> InboxesDeleted =
        Meter.CreateCounter<long>(
            "vaultsandbox.inboxes.deleted",
            description: "Number of inboxes deleted");

    internal static readonly Counter<long> EmailsReceived =
        Meter.CreateCounter<long>(
            "vaultsandbox.emails.received",
            description: "Number of emails received");

    internal static readonly Counter<long> EmailsDeleted =
        Meter.CreateCounter<long>(
            "vaultsandbox.emails.deleted",
            description: "Number of emails deleted");

    internal static readonly Counter<long> ApiCalls =
        Meter.CreateCounter<long>(
            "vaultsandbox.api.calls",
            description: "Number of API calls made");

    internal static readonly Counter<long> ApiErrors =
        Meter.CreateCounter<long>(
            "vaultsandbox.api.errors",
            description: "Number of API errors");

    // Histograms
    internal static readonly Histogram<double> EmailWaitDuration =
        Meter.CreateHistogram<double>(
            "vaultsandbox.email.wait.duration",
            unit: "ms",
            description: "Time spent waiting for emails");

    internal static readonly Histogram<double> DecryptionDuration =
        Meter.CreateHistogram<double>(
            "vaultsandbox.decryption.duration",
            unit: "ms",
            description: "Time spent decrypting emails");

    internal static readonly Histogram<double> ApiCallDuration =
        Meter.CreateHistogram<double>(
            "vaultsandbox.api.call.duration",
            unit: "ms",
            description: "Duration of API calls");

    /// <summary>
    /// Starts a new activity for an operation.
    /// </summary>
    internal static Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Client)
    {
        return ActivitySource.StartActivity(name, kind);
    }
}
