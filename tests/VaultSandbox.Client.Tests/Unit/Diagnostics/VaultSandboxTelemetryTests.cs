using System.Diagnostics;
using System.Diagnostics.Metrics;
using FluentAssertions;
using VaultSandbox.Client.Diagnostics;
using Xunit;

namespace VaultSandbox.Client.Tests.Unit.Diagnostics;

public class VaultSandboxTelemetryTests : IDisposable
{
    private readonly List<Activity> _capturedActivities = new();
    private readonly ActivityListener _activityListener;
    private readonly MeterListener _meterListener;
    private readonly List<(string Name, object Value, KeyValuePair<string, object?>[] Tags)> _capturedMeasurements = new();

    public VaultSandboxTelemetryTests()
    {
        // Set up activity listener
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == VaultSandboxTelemetry.ServiceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => _capturedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(_activityListener);

        // Set up meter listener
        _meterListener = new MeterListener();
        _meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == VaultSandboxTelemetry.ServiceName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _meterListener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            _capturedMeasurements.Add((instrument.Name, measurement, tags.ToArray()));
        });
        _meterListener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
        {
            _capturedMeasurements.Add((instrument.Name, measurement, tags.ToArray()));
        });
        _meterListener.Start();
    }

    public void Dispose()
    {
        _activityListener.Dispose();
        _meterListener.Dispose();
    }

    #region ServiceName and ServiceVersion Tests

    [Fact]
    public void ServiceName_ShouldBeCorrectValue()
    {
        // Assert
        VaultSandboxTelemetry.ServiceName.Should().Be("VaultSandbox.Client");
    }

    [Fact]
    public void ServiceVersion_ShouldNotBeNull()
    {
        // Assert
        VaultSandboxTelemetry.ServiceVersion.Should().NotBeNull();
    }

    [Fact]
    public void ServiceVersion_ShouldBeValidVersionFormat()
    {
        // Assert - Version should either be a valid version or "0.0.0" fallback
        var version = VaultSandboxTelemetry.ServiceVersion;
        version.Should().MatchRegex(@"^\d+\.\d+\.\d+(\.\d+)?$");
    }

    #endregion

    #region ActivitySource Tests

    [Fact]
    public void ActivitySource_ShouldNotBeNull()
    {
        // Assert
        VaultSandboxTelemetry.ActivitySource.Should().NotBeNull();
    }

    [Fact]
    public void ActivitySource_ShouldHaveCorrectName()
    {
        // Assert
        VaultSandboxTelemetry.ActivitySource.Name.Should().Be(VaultSandboxTelemetry.ServiceName);
    }

    [Fact]
    public void ActivitySource_ShouldHaveCorrectVersion()
    {
        // Assert
        VaultSandboxTelemetry.ActivitySource.Version.Should().Be(VaultSandboxTelemetry.ServiceVersion);
    }

    [Fact]
    public void ActivitySource_ShouldBeSameInstanceOnMultipleAccess()
    {
        // Act
        var source1 = VaultSandboxTelemetry.ActivitySource;
        var source2 = VaultSandboxTelemetry.ActivitySource;

        // Assert
        source1.Should().BeSameAs(source2);
    }

    #endregion

    #region Meter Tests

    [Fact]
    public void Meter_ShouldNotBeNull()
    {
        // Assert
        VaultSandboxTelemetry.Meter.Should().NotBeNull();
    }

    [Fact]
    public void Meter_ShouldHaveCorrectName()
    {
        // Assert
        VaultSandboxTelemetry.Meter.Name.Should().Be(VaultSandboxTelemetry.ServiceName);
    }

    [Fact]
    public void Meter_ShouldHaveCorrectVersion()
    {
        // Assert
        VaultSandboxTelemetry.Meter.Version.Should().Be(VaultSandboxTelemetry.ServiceVersion);
    }

    [Fact]
    public void Meter_ShouldBeSameInstanceOnMultipleAccess()
    {
        // Act
        var meter1 = VaultSandboxTelemetry.Meter;
        var meter2 = VaultSandboxTelemetry.Meter;

        // Assert
        meter1.Should().BeSameAs(meter2);
    }

    #endregion

    #region Counter Tests

    [Fact]
    public void InboxesCreated_ShouldNotBeNull()
    {
        // Assert
        VaultSandboxTelemetry.InboxesCreated.Should().NotBeNull();
    }

    [Fact]
    public void InboxesCreated_ShouldHaveCorrectName()
    {
        // Assert
        VaultSandboxTelemetry.InboxesCreated.Name.Should().Be("vaultsandbox.inboxes.created");
    }

    [Fact]
    public void InboxesCreated_ShouldRecordMeasurements()
    {
        // Arrange
        _capturedMeasurements.Clear();

        // Act
        VaultSandboxTelemetry.InboxesCreated.Add(1);

        // Assert
        _capturedMeasurements.Should().ContainSingle(m => m.Name == "vaultsandbox.inboxes.created");
    }

    [Fact]
    public void InboxesDeleted_ShouldNotBeNull()
    {
        // Assert
        VaultSandboxTelemetry.InboxesDeleted.Should().NotBeNull();
    }

    [Fact]
    public void InboxesDeleted_ShouldHaveCorrectName()
    {
        // Assert
        VaultSandboxTelemetry.InboxesDeleted.Name.Should().Be("vaultsandbox.inboxes.deleted");
    }

    [Fact]
    public void InboxesDeleted_ShouldRecordMeasurements()
    {
        // Arrange
        _capturedMeasurements.Clear();

        // Act
        VaultSandboxTelemetry.InboxesDeleted.Add(1);

        // Assert
        _capturedMeasurements.Should().ContainSingle(m => m.Name == "vaultsandbox.inboxes.deleted");
    }

    [Fact]
    public void EmailsReceived_ShouldNotBeNull()
    {
        // Assert
        VaultSandboxTelemetry.EmailsReceived.Should().NotBeNull();
    }

    [Fact]
    public void EmailsReceived_ShouldHaveCorrectName()
    {
        // Assert
        VaultSandboxTelemetry.EmailsReceived.Name.Should().Be("vaultsandbox.emails.received");
    }

    [Fact]
    public void EmailsReceived_ShouldRecordMeasurements()
    {
        // Arrange
        _capturedMeasurements.Clear();

        // Act
        VaultSandboxTelemetry.EmailsReceived.Add(5);

        // Assert
        var measurement = _capturedMeasurements.Should().ContainSingle(m => m.Name == "vaultsandbox.emails.received").Subject;
        measurement.Value.Should().Be(5L);
    }

    [Fact]
    public void EmailsDeleted_ShouldNotBeNull()
    {
        // Assert
        VaultSandboxTelemetry.EmailsDeleted.Should().NotBeNull();
    }

    [Fact]
    public void EmailsDeleted_ShouldHaveCorrectName()
    {
        // Assert
        VaultSandboxTelemetry.EmailsDeleted.Name.Should().Be("vaultsandbox.emails.deleted");
    }

    [Fact]
    public void EmailsDeleted_ShouldRecordMeasurements()
    {
        // Arrange
        _capturedMeasurements.Clear();

        // Act
        VaultSandboxTelemetry.EmailsDeleted.Add(3);

        // Assert
        var measurement = _capturedMeasurements.Should().ContainSingle(m => m.Name == "vaultsandbox.emails.deleted").Subject;
        measurement.Value.Should().Be(3L);
    }

    [Fact]
    public void ApiCalls_ShouldNotBeNull()
    {
        // Assert
        VaultSandboxTelemetry.ApiCalls.Should().NotBeNull();
    }

    [Fact]
    public void ApiCalls_ShouldHaveCorrectName()
    {
        // Assert
        VaultSandboxTelemetry.ApiCalls.Name.Should().Be("vaultsandbox.api.calls");
    }

    [Fact]
    public void ApiCalls_ShouldRecordMeasurements()
    {
        // Arrange
        _capturedMeasurements.Clear();

        // Act
        VaultSandboxTelemetry.ApiCalls.Add(10);

        // Assert
        var measurement = _capturedMeasurements.Should().ContainSingle(m => m.Name == "vaultsandbox.api.calls").Subject;
        measurement.Value.Should().Be(10L);
    }

    [Fact]
    public void ApiErrors_ShouldNotBeNull()
    {
        // Assert
        VaultSandboxTelemetry.ApiErrors.Should().NotBeNull();
    }

    [Fact]
    public void ApiErrors_ShouldHaveCorrectName()
    {
        // Assert
        VaultSandboxTelemetry.ApiErrors.Name.Should().Be("vaultsandbox.api.errors");
    }

    [Fact]
    public void ApiErrors_ShouldRecordMeasurements()
    {
        // Arrange
        _capturedMeasurements.Clear();

        // Act
        VaultSandboxTelemetry.ApiErrors.Add(2);

        // Assert
        var measurement = _capturedMeasurements.Should().ContainSingle(m => m.Name == "vaultsandbox.api.errors").Subject;
        measurement.Value.Should().Be(2L);
    }

    #endregion

    #region Histogram Tests

    [Fact]
    public void EmailWaitDuration_ShouldNotBeNull()
    {
        // Assert
        VaultSandboxTelemetry.EmailWaitDuration.Should().NotBeNull();
    }

    [Fact]
    public void EmailWaitDuration_ShouldHaveCorrectName()
    {
        // Assert
        VaultSandboxTelemetry.EmailWaitDuration.Name.Should().Be("vaultsandbox.email.wait.duration");
    }

    [Fact]
    public void EmailWaitDuration_ShouldHaveCorrectUnit()
    {
        // Assert
        VaultSandboxTelemetry.EmailWaitDuration.Unit.Should().Be("ms");
    }

    [Fact]
    public void EmailWaitDuration_ShouldRecordMeasurements()
    {
        // Arrange
        _capturedMeasurements.Clear();

        // Act
        VaultSandboxTelemetry.EmailWaitDuration.Record(150.5);

        // Assert
        var measurement = _capturedMeasurements.Should().ContainSingle(m => m.Name == "vaultsandbox.email.wait.duration").Subject;
        measurement.Value.Should().Be(150.5);
    }

    [Fact]
    public void DecryptionDuration_ShouldNotBeNull()
    {
        // Assert
        VaultSandboxTelemetry.DecryptionDuration.Should().NotBeNull();
    }

    [Fact]
    public void DecryptionDuration_ShouldHaveCorrectName()
    {
        // Assert
        VaultSandboxTelemetry.DecryptionDuration.Name.Should().Be("vaultsandbox.decryption.duration");
    }

    [Fact]
    public void DecryptionDuration_ShouldHaveCorrectUnit()
    {
        // Assert
        VaultSandboxTelemetry.DecryptionDuration.Unit.Should().Be("ms");
    }

    [Fact]
    public void DecryptionDuration_ShouldRecordMeasurements()
    {
        // Arrange
        _capturedMeasurements.Clear();

        // Act
        VaultSandboxTelemetry.DecryptionDuration.Record(25.3);

        // Assert
        var measurement = _capturedMeasurements.Should().ContainSingle(m => m.Name == "vaultsandbox.decryption.duration").Subject;
        measurement.Value.Should().Be(25.3);
    }

    [Fact]
    public void ApiCallDuration_ShouldNotBeNull()
    {
        // Assert
        VaultSandboxTelemetry.ApiCallDuration.Should().NotBeNull();
    }

    [Fact]
    public void ApiCallDuration_ShouldHaveCorrectName()
    {
        // Assert
        VaultSandboxTelemetry.ApiCallDuration.Name.Should().Be("vaultsandbox.api.call.duration");
    }

    [Fact]
    public void ApiCallDuration_ShouldHaveCorrectUnit()
    {
        // Assert
        VaultSandboxTelemetry.ApiCallDuration.Unit.Should().Be("ms");
    }

    [Fact]
    public void ApiCallDuration_ShouldRecordMeasurements()
    {
        // Arrange
        _capturedMeasurements.Clear();

        // Act
        VaultSandboxTelemetry.ApiCallDuration.Record(100.0);

        // Assert
        var measurement = _capturedMeasurements.Should().ContainSingle(m => m.Name == "vaultsandbox.api.call.duration").Subject;
        measurement.Value.Should().Be(100.0);
    }

    #endregion

    #region StartActivity Tests

    [Fact]
    public void StartActivity_WithValidName_ShouldCreateActivity()
    {
        // Arrange
        _capturedActivities.Clear();

        // Act
        using var activity = VaultSandboxTelemetry.StartActivity("TestOperation");

        // Assert
        activity.Should().NotBeNull();
        activity!.OperationName.Should().Be("TestOperation");
    }

    [Fact]
    public void StartActivity_WithDefaultKind_ShouldBeClient()
    {
        // Act
        using var activity = VaultSandboxTelemetry.StartActivity("TestOperation");

        // Assert
        activity.Should().NotBeNull();
        activity!.Kind.Should().Be(ActivityKind.Client);
    }

    [Fact]
    public void StartActivity_WithSpecificKind_ShouldUseProvidedKind()
    {
        // Act
        using var activity = VaultSandboxTelemetry.StartActivity("TestOperation", ActivityKind.Producer);

        // Assert
        activity.Should().NotBeNull();
        activity!.Kind.Should().Be(ActivityKind.Producer);
    }

    [Theory]
    [InlineData(ActivityKind.Client)]
    [InlineData(ActivityKind.Server)]
    [InlineData(ActivityKind.Producer)]
    [InlineData(ActivityKind.Consumer)]
    [InlineData(ActivityKind.Internal)]
    public void StartActivity_WithVariousKinds_ShouldSupportAllKinds(ActivityKind kind)
    {
        // Act
        using var activity = VaultSandboxTelemetry.StartActivity("TestOperation", kind);

        // Assert
        activity.Should().NotBeNull();
        activity!.Kind.Should().Be(kind);
    }

    [Fact]
    public void StartActivity_ShouldHaveCorrectSource()
    {
        // Act
        using var activity = VaultSandboxTelemetry.StartActivity("TestOperation");

        // Assert
        activity.Should().NotBeNull();
        activity!.Source.Should().BeSameAs(VaultSandboxTelemetry.ActivitySource);
    }

    [Fact]
    public void StartActivity_ShouldBeStoppedOnDispose()
    {
        // Act
        Activity? capturedActivity;
        using (var activity = VaultSandboxTelemetry.StartActivity("TestOperation"))
        {
            capturedActivity = activity;
            activity.Should().NotBeNull();
            activity!.IsStopped.Should().BeFalse();
        }

        // Assert - after dispose
        capturedActivity!.IsStopped.Should().BeTrue();
    }

    [Fact]
    public void StartActivity_MultipleActivities_ShouldBeIndependent()
    {
        // Act
        using var activity1 = VaultSandboxTelemetry.StartActivity("Operation1");
        using var activity2 = VaultSandboxTelemetry.StartActivity("Operation2");

        // Assert
        activity1.Should().NotBeNull();
        activity2.Should().NotBeNull();
        activity1!.Id.Should().NotBe(activity2!.Id);
        activity1.OperationName.Should().Be("Operation1");
        activity2.OperationName.Should().Be("Operation2");
    }

    [Fact]
    public void StartActivity_WithEmptyName_ShouldStillCreateActivity()
    {
        // Act
        using var activity = VaultSandboxTelemetry.StartActivity("");

        // Assert
        activity.Should().NotBeNull();
        activity!.OperationName.Should().BeEmpty();
    }

    #endregion

    #region Counter with Tags Tests

    [Fact]
    public void Counter_WithTags_ShouldRecordWithTags()
    {
        // Arrange
        _capturedMeasurements.Clear();
        var tags = new TagList
        {
            { "inbox_id", "test-inbox-123" }
        };

        // Act
        VaultSandboxTelemetry.InboxesCreated.Add(1, tags);

        // Assert
        var measurement = _capturedMeasurements.Should().ContainSingle(m => m.Name == "vaultsandbox.inboxes.created").Subject;
        measurement.Tags.Should().ContainSingle(t => t.Key == "inbox_id" && t.Value!.ToString() == "test-inbox-123");
    }

    [Fact]
    public void ApiCalls_WithMethodTag_ShouldRecordCorrectly()
    {
        // Arrange
        _capturedMeasurements.Clear();

        // Act
        VaultSandboxTelemetry.ApiCalls.Add(1, new KeyValuePair<string, object?>("method", "GET"));

        // Assert
        var measurement = _capturedMeasurements.Should().ContainSingle(m => m.Name == "vaultsandbox.api.calls").Subject;
        measurement.Tags.Should().ContainSingle(t => t.Key == "method" && t.Value!.ToString() == "GET");
    }

    [Fact]
    public void ApiErrors_WithStatusCodeTag_ShouldRecordCorrectly()
    {
        // Arrange
        _capturedMeasurements.Clear();

        // Act
        VaultSandboxTelemetry.ApiErrors.Add(1, new KeyValuePair<string, object?>("status_code", 500));

        // Assert
        var measurement = _capturedMeasurements.Should().ContainSingle(m => m.Name == "vaultsandbox.api.errors").Subject;
        measurement.Tags.Should().ContainSingle(t => t.Key == "status_code" && (int)t.Value! == 500);
    }

    #endregion

    #region Histogram with Tags Tests

    [Fact]
    public void Histogram_WithTags_ShouldRecordWithTags()
    {
        // Arrange
        _capturedMeasurements.Clear();

        // Act
        VaultSandboxTelemetry.ApiCallDuration.Record(200.5, new KeyValuePair<string, object?>("endpoint", "/api/inboxes"));

        // Assert
        var measurement = _capturedMeasurements.Should().ContainSingle(m => m.Name == "vaultsandbox.api.call.duration").Subject;
        measurement.Value.Should().Be(200.5);
        measurement.Tags.Should().ContainSingle(t => t.Key == "endpoint" && t.Value!.ToString() == "/api/inboxes");
    }

    [Fact]
    public void DecryptionDuration_WithAlgorithmTag_ShouldRecordCorrectly()
    {
        // Arrange
        _capturedMeasurements.Clear();

        // Act
        VaultSandboxTelemetry.DecryptionDuration.Record(15.7, new KeyValuePair<string, object?>("algorithm", "AES-GCM"));

        // Assert
        var measurement = _capturedMeasurements.Should().ContainSingle(m => m.Name == "vaultsandbox.decryption.duration").Subject;
        measurement.Value.Should().Be(15.7);
        measurement.Tags.Should().ContainSingle(t => t.Key == "algorithm" && t.Value!.ToString() == "AES-GCM");
    }

    #endregion

    #region Instrument Description Tests

    [Fact]
    public void InboxesCreated_ShouldHaveDescription()
    {
        // Assert
        VaultSandboxTelemetry.InboxesCreated.Description.Should().Be("Number of inboxes created");
    }

    [Fact]
    public void InboxesDeleted_ShouldHaveDescription()
    {
        // Assert
        VaultSandboxTelemetry.InboxesDeleted.Description.Should().Be("Number of inboxes deleted");
    }

    [Fact]
    public void EmailsReceived_ShouldHaveDescription()
    {
        // Assert
        VaultSandboxTelemetry.EmailsReceived.Description.Should().Be("Number of emails received");
    }

    [Fact]
    public void EmailsDeleted_ShouldHaveDescription()
    {
        // Assert
        VaultSandboxTelemetry.EmailsDeleted.Description.Should().Be("Number of emails deleted");
    }

    [Fact]
    public void ApiCalls_ShouldHaveDescription()
    {
        // Assert
        VaultSandboxTelemetry.ApiCalls.Description.Should().Be("Number of API calls made");
    }

    [Fact]
    public void ApiErrors_ShouldHaveDescription()
    {
        // Assert
        VaultSandboxTelemetry.ApiErrors.Description.Should().Be("Number of API errors");
    }

    [Fact]
    public void EmailWaitDuration_ShouldHaveDescription()
    {
        // Assert
        VaultSandboxTelemetry.EmailWaitDuration.Description.Should().Be("Time spent waiting for emails");
    }

    [Fact]
    public void DecryptionDuration_ShouldHaveDescription()
    {
        // Assert
        VaultSandboxTelemetry.DecryptionDuration.Description.Should().Be("Time spent decrypting emails");
    }

    [Fact]
    public void ApiCallDuration_ShouldHaveDescription()
    {
        // Assert
        VaultSandboxTelemetry.ApiCallDuration.Description.Should().Be("Duration of API calls");
    }

    #endregion

    #region Multiple Measurements Tests

    [Fact]
    public void Counter_MultipleMeasurements_ShouldRecordAll()
    {
        // Arrange
        _capturedMeasurements.Clear();

        // Act
        VaultSandboxTelemetry.ApiCalls.Add(1);
        VaultSandboxTelemetry.ApiCalls.Add(2);
        VaultSandboxTelemetry.ApiCalls.Add(3);

        // Assert
        var apiCallMeasurements = _capturedMeasurements.Where(m => m.Name == "vaultsandbox.api.calls").ToList();
        apiCallMeasurements.Should().HaveCount(3);
        apiCallMeasurements.Select(m => m.Value).Should().BeEquivalentTo(new object[] { 1L, 2L, 3L });
    }

    [Fact]
    public void Histogram_MultipleMeasurements_ShouldRecordAll()
    {
        // Arrange
        _capturedMeasurements.Clear();

        // Act
        VaultSandboxTelemetry.ApiCallDuration.Record(100.0);
        VaultSandboxTelemetry.ApiCallDuration.Record(200.0);
        VaultSandboxTelemetry.ApiCallDuration.Record(300.0);

        // Assert
        var durationMeasurements = _capturedMeasurements.Where(m => m.Name == "vaultsandbox.api.call.duration").ToList();
        durationMeasurements.Should().HaveCount(3);
        durationMeasurements.Select(m => m.Value).Should().BeEquivalentTo(new object[] { 100.0, 200.0, 300.0 });
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void Counter_WithZeroValue_ShouldRecord()
    {
        // Arrange
        _capturedMeasurements.Clear();

        // Act
        VaultSandboxTelemetry.ApiCalls.Add(0);

        // Assert
        var measurement = _capturedMeasurements.Should().ContainSingle(m => m.Name == "vaultsandbox.api.calls").Subject;
        measurement.Value.Should().Be(0L);
    }

    [Fact]
    public void Counter_WithLargeValue_ShouldRecord()
    {
        // Arrange
        _capturedMeasurements.Clear();

        // Act
        VaultSandboxTelemetry.EmailsReceived.Add(long.MaxValue);

        // Assert
        var measurement = _capturedMeasurements.Should().ContainSingle(m => m.Name == "vaultsandbox.emails.received").Subject;
        measurement.Value.Should().Be(long.MaxValue);
    }

    [Fact]
    public void Histogram_WithZeroValue_ShouldRecord()
    {
        // Arrange
        _capturedMeasurements.Clear();

        // Act
        VaultSandboxTelemetry.ApiCallDuration.Record(0.0);

        // Assert
        var measurement = _capturedMeasurements.Should().ContainSingle(m => m.Name == "vaultsandbox.api.call.duration").Subject;
        measurement.Value.Should().Be(0.0);
    }

    [Fact]
    public void Histogram_WithVerySmallValue_ShouldRecord()
    {
        // Arrange
        _capturedMeasurements.Clear();

        // Act
        VaultSandboxTelemetry.DecryptionDuration.Record(0.001);

        // Assert
        var measurement = _capturedMeasurements.Should().ContainSingle(m => m.Name == "vaultsandbox.decryption.duration").Subject;
        measurement.Value.Should().Be(0.001);
    }

    [Fact]
    public void Histogram_WithLargeValue_ShouldRecord()
    {
        // Arrange
        _capturedMeasurements.Clear();

        // Act
        VaultSandboxTelemetry.EmailWaitDuration.Record(double.MaxValue);

        // Assert
        var measurement = _capturedMeasurements.Should().ContainSingle(m => m.Name == "vaultsandbox.email.wait.duration").Subject;
        measurement.Value.Should().Be(double.MaxValue);
    }

    #endregion
}
